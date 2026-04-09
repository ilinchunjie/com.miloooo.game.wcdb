using System.Collections.Generic;
using Miloooo.WCDB;
using UnityEngine;

namespace Miloooo.WCDB.Samples.BatchCrud
{
    public sealed class BatchCrudSample : MonoBehaviour
    {
        private void Start()
        {
            using var database = WcdbDatabase.Open(System.IO.Path.Combine(Application.persistentDataPath, "batch-crud.db"));

            WcdbTable table = database.Table("event_log");
            table.Create(new TableSchema("event_log")
                .Column("id", ColumnType.Integer, isPrimaryKey: true)
                .Column("category", ColumnType.Text, notNull: true)
                .Column("payload", ColumnType.Text, notNull: true));

            database.BeginTransaction();
            try {
                var rows = new List<RowData>();
                for (int index = 0; index < 1000; ++index) {
                    rows.Add(new RowData
                    {
                        { "id", index + 1 },
                        { "category", "combat" },
                        { "payload", $"event-{index + 1}" },
                    });
                }
                table.InsertMany(rows);
                database.Commit();
            } catch {
                database.Rollback();
                throw;
            }

            using WcdbQueryResult result = table.Query(new QueryOptions
            {
                OrderBySql = "\"id\" DESC",
                Limit = 10,
            });

            foreach (RowData row in result) {
                Debug.Log($"Recent row: {row["id"]} => {row["payload"]}");
            }
        }
    }
}
