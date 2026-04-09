# WCDB Unity Minimal Plugin

This package publishes `com.miloooo.game.wcdb` as a Unity UPM package with a size-first native runtime named `wcdb` and a data-driven C# API in `Plugins/Runtime`.

## What Is Included

- Native binaries named `wcdb` for Windows, Android, iOS, macOS, and Linux.
- `Plugins/Runtime` C# bridge for:
  - `WcdbDatabase.Open(path, options)` / `Close()`
  - transaction APIs
  - raw `Execute`, `Query`, and `Prepare`
  - coarse native CRUD through `WcdbTable`
  - C# `user_version` migrations
- Samples for basic CRUD, batch CRUD, and version migration.

## Minimal Build Defaults

The release workflow builds a smallest-possible flavor by default:

- `WCDB_CPP=OFF`
- `WCDB_BRIDGE=OFF`
- `WCDB_ZSTD=OFF`
- `SQLCIPHER_ENABLE_CIPHER=OFF`
- `SQLCIPHER_ENABLE_JSON1=OFF`
- `SQLCIPHER_ENABLE_FTS3=OFF`
- `SQLCIPHER_ENABLE_FTS5=OFF`
- `SQLCIPHER_ENABLE_RTREE=OFF`
- `SQLCIPHER_ENABLE_SESSION=OFF`
- `SQLCIPHER_ENABLE_PREUPDATE_HOOK=OFF`
- `SQLCIPHER_ENABLE_STAT4=OFF`
- `SQLCIPHER_ENABLE_EXPLAIN_COMMENTS=OFF`
- `SQLCIPHER_ENABLE_DBSTAT_VTAB=OFF`
- `SQLCIPHER_ENABLE_COLUMN_METADATA=OFF`
- `WCDB_ENABLE_REPAIR=OFF`
- `WCDB_ENABLE_NATIVE_MIGRATION=OFF`
- `WCDB_ENABLE_TRACE=OFF`

Recommended default for the main package:

- Keep `SQLCIPHER_ENABLE_CIPHER` off unless you intentionally ship an encrypted flavor.
- Keep `SQLCIPHER_ENABLE_JSON1` off unless SQL-side JSON querying is required.
- Keep `SQLCIPHER_ENABLE_FTS3` and `SQLCIPHER_ENABLE_FTS5` off unless you need full-text search.
- Keep `SQLCIPHER_ENABLE_RTREE` off unless you need spatial indexing.
- Keep `SQLCIPHER_ENABLE_SESSION` and `SQLCIPHER_ENABLE_PREUPDATE_HOOK` off unless you explicitly need change capture.
- Keep `WCDB_ZSTD`, `WCDB_ENABLE_REPAIR`, native migration, and trace off for the Unity minimal build.

## Performance Model

- Public API stays in C# for ergonomics.
- High-frequency CRUD execution stays in native code to avoid excessive fine-grained P/Invoke round-trips.
- Migrations stay in C# because they are not a hot path and `PRAGMA user_version` plus `Execute()` keeps the native surface small.

## iOS Distribution

- iOS ships as `Plugins/iOS/wcdb.xcframework`.
- Only `ios-arm64` is included.
- C# uses `DllImport("__Internal")` on iOS player builds while the plugin name remains `wcdb`.
