using Miloooo.WCDB;
using UnityEngine;

namespace Miloooo.WCDB.Samples.BasicCrud
{
    public sealed class BasicCrudSample : MonoBehaviour
    {
        private void Start()
        {
            using var database = WcdbDatabase.Open(System.IO.Path.Combine(Application.persistentDataPath, "basic-crud.db"));

            var schema = new TableSchema("player")
                .Column("id", ColumnType.Integer, isPrimaryKey: true)
                .Column("name", ColumnType.Text, notNull: true)
                .Column("level", ColumnType.Integer, notNull: true, defaultValueSql: "1");

            WcdbTable table = database.Table("player");
            table.Create(schema);

            var player = new RowData
            {
                { "id", 1 },
                { "name", "Hero" },
                { "level", 10 },
            };

            table.Insert(player);

            table.Update(new RowData
            {
                { "level", 11 },
            }, SqlCondition.Where("id = ?", 1));

            RowData? fetched = table.QueryFirst(new QueryOptions
            {
                Condition = SqlCondition.Where("id = ?", 1),
            });

            if (fetched != null) {
                Debug.Log($"Fetched player: {fetched["name"]} lv.{fetched["level"]}");
            }

            table.Delete(SqlCondition.Where("id = ?", 1));
        }
    }
}
