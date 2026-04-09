using System;
using System.Collections.Generic;

namespace com.miloooo.game.wcdb
{
    public sealed class SqlCondition
    {
        public SqlCondition(string sql, IReadOnlyList<WcdbValue>? parameters = null)
        {
            Sql = sql ?? throw new ArgumentNullException(nameof(sql));
            Parameters = parameters ?? Array.Empty<WcdbValue>();
        }

        public string Sql { get; }

        public IReadOnlyList<WcdbValue> Parameters { get; }

        public static SqlCondition Where(string sql, params object?[] parameters)
        {
            var values = new WcdbValue[parameters.Length];
            for (int index = 0; index < parameters.Length; ++index) {
                values[index] = WcdbValue.FromObject(parameters[index]);
            }
            return new SqlCondition(sql, values);
        }
    }

    public sealed class QueryOptions
    {
        public IReadOnlyList<string>? SelectedColumns { get; set; }

        public SqlCondition? Condition { get; set; }

        public string? OrderBySql { get; set; }

        public int? Limit { get; set; }

        public int? Offset { get; set; }
    }
}
