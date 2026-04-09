using System.Linq;
using Miloooo.WCDB;
using UnityEngine;

namespace Miloooo.WCDB.Samples.VersionMigration
{
    public sealed class VersionMigrationSample : MonoBehaviour
    {
        private void Start()
        {
            using var database = WcdbDatabase.Open(System.IO.Path.Combine(Application.persistentDataPath, "migration.db"));

            database.RunMigrations(new[]
            {
                MigrationScript.ToVersion(1, builder => builder.CreateTable(
                    new TableSchema("player")
                        .Column("id", ColumnType.Integer, isPrimaryKey: true)
                        .Column("name", ColumnType.Text, notNull: true)
                )),
                MigrationScript.ToVersion(2, builder => builder
                    .AddColumn("player", "last_login", ColumnType.Integer, defaultValueSql: "0")
                    .Custom(db =>
                    {
                        WcdbTable table = db.Table("player");
                        RowData? player = table.QueryFirst(new QueryOptions
                        {
                            Condition = SqlCondition.Where("id = ?", 1),
                        });
                        if (player != null) {
                            table.Update(new RowData
                            {
                                { "last_login", System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                            }, SqlCondition.Where("id = ?", 1));
                        }
                    }))
            }.OrderBy(script => script.TargetVersion));

            Debug.Log("Migrations applied.");
        }
    }
}
