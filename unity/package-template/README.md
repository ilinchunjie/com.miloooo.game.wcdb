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

- `UNITY_WCDB_ENABLE_CIPHER=OFF`
- `UNITY_WCDB_ENABLE_JSON1=OFF`
- `UNITY_WCDB_ENABLE_FTS=OFF`
- `UNITY_WCDB_ENABLE_RTREE=OFF`
- `UNITY_WCDB_ENABLE_SESSION=OFF`
- `UNITY_WCDB_ENABLE_PREUPDATE_HOOK=OFF`
- `UNITY_WCDB_ENABLE_ZSTD=OFF`
- `UNITY_WCDB_ENABLE_REPAIR=OFF`
- `UNITY_WCDB_ENABLE_WCDB_NATIVE_MIGRATION=OFF`
- `UNITY_WCDB_ENABLE_TRACE=OFF`

Recommended default for the main package:

- Keep `CIPHER` off unless you intentionally ship an encrypted flavor.
- Keep `JSON1` off unless SQL-side JSON querying is required.
- Keep `FTS` off unless you need full-text search.
- Keep `RTREE` off unless you need spatial indexing.
- Keep `SESSION` and `PREUPDATE_HOOK` off unless you explicitly need change capture.
- Keep `ZSTD`, `REPAIR`, native migration, and trace off for the Unity minimal build.

## Performance Model

- Public API stays in C# for ergonomics.
- High-frequency CRUD execution stays in native code to avoid excessive fine-grained P/Invoke round-trips.
- Migrations stay in C# because they are not a hot path and `PRAGMA user_version` plus `Execute()` keeps the native surface small.

## iOS Distribution

- iOS ships as `Plugins/iOS/wcdb.xcframework`.
- Only `ios-arm64` is included.
- C# uses `DllImport("__Internal")` on iOS player builds while the plugin name remains `wcdb`.
