using System;
using System.Collections.Generic;

namespace Miloooo.WCDB
{
    public sealed class MigrationBuilder
    {
        private readonly List<Action<WcdbDatabase>> _steps = new();

        internal IReadOnlyList<Action<WcdbDatabase>> Steps => _steps;

        public MigrationBuilder Execute(string sql, params object?[] parameters)
        {
            _steps.Add(database => database.Execute(sql, parameters));
            return this;
        }

        public MigrationBuilder CreateTable(TableSchema schema)
        {
            _steps.Add(database => database.Table(schema.Name).Create(schema));
            return this;
        }

        public MigrationBuilder AddColumn(string tableName,
                                          string columnName,
                                          ColumnType columnType,
                                          ColumnFlags flags = ColumnFlags.None,
                                          string? defaultValueSql = null)
        {
            var declaredType = new ColumnSchema(columnName, columnType);
            if ((flags & ColumnFlags.NotNull) != 0) {
                declaredType.NotNull();
            }
            if ((flags & ColumnFlags.Unique) != 0) {
                declaredType.Unique();
            }
            if ((flags & ColumnFlags.PrimaryKey) != 0) {
                declaredType.PrimaryKey();
            }
            if ((flags & ColumnFlags.AutoIncrement) != 0) {
                declaredType.AutoIncrement();
            }
            if (!string.IsNullOrWhiteSpace(defaultValueSql)) {
                declaredType.DefaultSql(defaultValueSql);
            }

            _steps.Add(database =>
            {
                string sql = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {declaredType.ToDeclaredType()}";
                if ((declaredType.Flags & ColumnFlags.PrimaryKey) != 0) {
                    sql += " PRIMARY KEY";
                }
                if ((declaredType.Flags & ColumnFlags.AutoIncrement) != 0) {
                    sql += " AUTOINCREMENT";
                }
                if ((declaredType.Flags & ColumnFlags.NotNull) != 0) {
                    sql += " NOT NULL";
                }
                if ((declaredType.Flags & ColumnFlags.Unique) != 0) {
                    sql += " UNIQUE";
                }
                if (!string.IsNullOrWhiteSpace(declaredType.DefaultValueSql)) {
                    sql += $" DEFAULT {declaredType.DefaultValueSql}";
                }
                database.Execute(sql);
            });
            return this;
        }

        public MigrationBuilder RenameTable(string fromTableName, string toTableName)
        {
            _steps.Add(database =>
            {
                database.Execute($"ALTER TABLE \"{fromTableName}\" RENAME TO \"{toTableName}\"");
            });
            return this;
        }

        public MigrationBuilder DropTable(string tableName)
        {
            _steps.Add(database => database.Execute($"DROP TABLE IF EXISTS \"{tableName}\""));
            return this;
        }

        public MigrationBuilder Custom(Action<WcdbDatabase> action)
        {
            _steps.Add(action ?? throw new ArgumentNullException(nameof(action)));
            return this;
        }
    }

    public sealed class MigrationScript
    {
        private MigrationScript(int targetVersion, IReadOnlyList<Action<WcdbDatabase>> steps)
        {
            TargetVersion = targetVersion;
            Steps = steps;
        }

        public int TargetVersion { get; }

        internal IReadOnlyList<Action<WcdbDatabase>> Steps { get; }

        public static MigrationScript ToVersion(int targetVersion, Action<MigrationBuilder>? configure = null)
        {
            if (targetVersion <= 0) {
                throw new ArgumentOutOfRangeException(nameof(targetVersion));
            }

            var builder = new MigrationBuilder();
            configure?.Invoke(builder);
            return new MigrationScript(targetVersion, builder.Steps);
        }
    }
}
