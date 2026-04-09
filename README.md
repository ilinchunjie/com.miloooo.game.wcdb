# WCDB for Unity

基于 [WCDB](https://github.com/Tencent/wcdb) 的 Unity 跨平台数据库插件，提供 SQLCipher 加密、WAL 模式、版本迁移等能力，通过 C# P/Invoke 封装原生 C 桥接层，开箱即用。

## ✨ 核心功能

- **跨平台支持** — Android (armeabi-v7a / arm64-v8a / x86_64)、iOS、macOS、Windows (x64)
- **SQLCipher 加密** — 支持 AES-256 数据库加密，通过 `EncryptionKey` 一行配置
- **WAL 模式** — 默认启用 Write-Ahead Logging，支持读写并发
- **CRUD 操作** — 基于 `WcdbTable` 的类型安全插入、查询、更新、删除
- **批量操作** — `InsertMany` 支持批量写入，事务自动回滚
- **参数化查询** — 防 SQL 注入，支持原生 SQL 与参数绑定
- **Schema 定义** — 通过 `TableSchema` 流式 API 声明表结构
- **版本迁移** — 基于 `user_version` 的增量迁移系统，支持建表、加列、重命名、删表等操作
- **事务支持** — `BeginTransaction` / `Commit` / `Rollback`，支持嵌套事务
- **Prepared Statement** — 高性能预编译语句，支持游标遍历

## 📦 安装

### 方式一：Unity Package Manager (推荐)

1. 打开 Unity 编辑器，菜单 **Window → Package Manager**
2. 点击左上角 **+** 按钮，选择 **Add package from git URL…**
3. 输入：
   ```
   https://github.com/ilinchunjie/com.miloooo.game.wcdb.git?path=unity/package
   ```
4. 点击 **Add**，等待导入完成

> 如需锁定版本，可在 URL 后追加 `#v1.0.0`（替换为实际 tag）

### 方式二：手动修改 manifest.json

编辑项目 `Packages/manifest.json`，在 `dependencies` 中添加：

```json
{
  "dependencies": {
    "com.miloooo.game.wcdb": "https://github.com/ilinchunjie/com.miloooo.game.wcdb.git?path=unity/package"
  }
}
```

## 🚀 快速上手

### 打开数据库

```csharp
using com.miloooo.game.wcdb;

// 基本用法
using var db = WcdbDatabase.Open(Application.persistentDataPath + "/game.db");

// 启用加密
using var db = WcdbDatabase.Open(path, new WcdbDatabaseOptions
{
    EncryptionKey = System.Text.Encoding.UTF8.GetBytes("your-secret-key")
});
```

### 定义 Schema 并建表

```csharp
var schema = new TableSchema("players")
    .Column("id",    ColumnType.Integer, isPrimaryKey: true, autoIncrement: true)
    .Column("name",  ColumnType.Text,    notNull: true)
    .Column("score", ColumnType.Integer)
    .Column("data",  ColumnType.Blob);

db.Table("players").Create(schema);
```

### 插入数据

```csharp
var table = db.Table("players");

// 单行插入
table.Insert(new RowData
{
    ["name"]  = WcdbValue.FromString("Alice"),
    ["score"] = WcdbValue.FromInt64(100)
});

// 批量插入 (自动事务)
table.InsertMany(new[]
{
    new RowData { ["name"] = WcdbValue.FromString("Bob"),   ["score"] = WcdbValue.FromInt64(200) },
    new RowData { ["name"] = WcdbValue.FromString("Carol"), ["score"] = WcdbValue.FromInt64(300) },
});

// UPSERT
table.Insert(row, orReplace: true);
```

### 查询数据

```csharp
// 查询全部
using var result = table.Query();
while (result.ReadNext(out RowData row))
{
    string name = row["name"].AsString();
    long score  = row["score"].AsInt64();
}

// 条件查询 + 排序 + 分页
using var result = table.Query(new QueryOptions
{
    SelectedColumns = new[] { "name", "score" },
    Condition = SqlCondition.Where("score > ?", 150),
    OrderBySql = "score DESC",
    Limit = 10,
    Offset = 0
});

// 查询单条
RowData? top = table.QueryFirst(new QueryOptions
{
    OrderBySql = "score DESC",
    Limit = 1
});
```

### 更新与删除

```csharp
// 更新
table.Update(
    new RowData { ["score"] = WcdbValue.FromInt64(999) },
    SqlCondition.Where("name = ?", "Alice")
);

// 删除
table.Delete(SqlCondition.Where("score < ?", 100));
```

### 事务

```csharp
db.BeginTransaction();
try
{
    table.Insert(row1);
    table.Insert(row2);
    db.Commit();
}
catch
{
    db.Rollback();
    throw;
}
```

### 版本迁移

```csharp
db.RunMigrations(new[]
{
    MigrationScript.ToVersion(1, m =>
    {
        m.CreateTable(new TableSchema("players")
            .Column("id",   ColumnType.Integer, isPrimaryKey: true, autoIncrement: true)
            .Column("name", ColumnType.Text,    notNull: true));
    }),

    MigrationScript.ToVersion(2, m =>
    {
        m.AddColumn("players", "score", ColumnType.Integer,
                    defaultValueSql: "0");
    }),

    MigrationScript.ToVersion(3, m =>
    {
        m.DropTable("legacy_table");
        m.RenameTable("players", "users");
    }),
});
```

### 原生 SQL

```csharp
// 执行
db.Execute("CREATE INDEX IF NOT EXISTS idx_score ON players(score)");

// 参数化查询
using var result = db.Query("SELECT * FROM players WHERE name LIKE ?", "%Ali%");
```

## 📁 项目结构

```
unity/
├── package/                        # UPM 包
│   ├── package.json
│   ├── Plugins/
│   │   ├── Runtime/                # C# API
│   │   │   ├── WcdbDatabase.cs     # 数据库连接与操作
│   │   │   ├── WcdbTable.cs        # 表级 CRUD
│   │   │   ├── Schema.cs           # 表/列定义
│   │   │   ├── Query.cs            # 查询条件与选项
│   │   │   ├── Migration.cs        # 版本迁移
│   │   │   ├── RowData.cs          # 行数据容器
│   │   │   ├── WcdbValue.cs        # 值类型封装
│   │   │   ├── WcdbQueryResult.cs  # 查询结果游标
│   │   │   └── WcdbPreparedStatement.cs
│   │   ├── Android/                # .so (per ABI)
│   │   ├── iOS/                    # .xcframework
│   │   ├── macOS/                  # .dylib
│   │   └── x86_64/                 # .dll (Windows)
│   └── Samples~/                   # 示例
├── native/                         # C 桥接层
│   ├── include/wcdb_unity.h
│   └── src/wcdb_unity.cpp
├── scripts/                        # 构建脚本
│   ├── build-apple.sh
│   ├── build-android.sh
│   └── build-host.sh
└── CMakeLists.txt                  # Unity 构建配置
```

## 🔧 从源码构建

```bash
# iOS
bash unity/scripts/build-apple.sh ios ./output

# macOS
bash unity/scripts/build-apple.sh macos ./output

# Android (需要 NDK)
bash unity/scripts/build-android.sh arm64-v8a $ANDROID_NDK_ROOT ./output

# Windows
bash unity/scripts/build-host.sh windows x64 .build/win ./output

# Linux
bash unity/scripts/build-host.sh linux x86_64 .build/linux ./output
```

## 📄 License

BSD 3-Clause License. See [LICENSE](LICENSE) for details.