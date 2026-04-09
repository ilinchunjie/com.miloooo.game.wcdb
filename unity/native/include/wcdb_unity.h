#pragma once

#include <stddef.h>
#include <stdint.h>

#if defined(_WIN32)
#if defined(WCDB_BUILD_SHARED)
#define WCDB_UNITY_EXPORT __declspec(dllexport)
#else
#define WCDB_UNITY_EXPORT __declspec(dllimport)
#endif
#else
#define WCDB_UNITY_EXPORT __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

typedef struct wcdb_database wcdb_database;
typedef struct wcdb_statement wcdb_statement;

typedef enum wcdb_status {
    WCDB_STATUS_OK = 0,
    WCDB_STATUS_ERROR = 1,
    WCDB_STATUS_ROW = 100,
    WCDB_STATUS_DONE = 101,
} wcdb_status;

typedef enum wcdb_value_type {
    WCDB_VALUE_NULL = 0,
    WCDB_VALUE_INT64 = 1,
    WCDB_VALUE_DOUBLE = 2,
    WCDB_VALUE_TEXT = 3,
    WCDB_VALUE_BLOB = 4,
    WCDB_VALUE_BOOL = 5,
} wcdb_value_type;

typedef enum wcdb_column_flags {
    WCDB_COLUMN_NONE = 0,
    WCDB_COLUMN_PRIMARY_KEY = 1 << 0,
    WCDB_COLUMN_NOT_NULL = 1 << 1,
    WCDB_COLUMN_UNIQUE = 1 << 2,
    WCDB_COLUMN_AUTOINCREMENT = 1 << 3,
} wcdb_column_flags;

typedef enum wcdb_query_option_flags {
    WCDB_QUERY_OPTION_NONE = 0,
    WCDB_QUERY_OPTION_HAS_LIMIT = 1 << 0,
    WCDB_QUERY_OPTION_HAS_OFFSET = 1 << 1,
} wcdb_query_option_flags;

typedef enum wcdb_feature {
    WCDB_FEATURE_CIPHER = 1,
    WCDB_FEATURE_JSON1 = 2,
    WCDB_FEATURE_FTS = 3,
    WCDB_FEATURE_RTREE = 4,
    WCDB_FEATURE_SESSION = 5,
    WCDB_FEATURE_PREUPDATE_HOOK = 6,
    WCDB_FEATURE_ZSTD = 7,
    WCDB_FEATURE_TRACE = 8,
} wcdb_feature;

typedef struct wcdb_blob {
    const void* bytes;
    int length;
} wcdb_blob;

typedef struct wcdb_value {
    int type;
    int bool_value;
    int64_t int64_value;
    double double_value;
    const char* text_value;
    wcdb_blob blob_value;
} wcdb_value;

typedef struct wcdb_column_def {
    const char* name;
    const char* declared_type;
    int flags;
    const char* default_value_sql;
} wcdb_column_def;

typedef struct wcdb_query_options {
    const char* where_sql;
    const char* order_by_sql;
    int limit;
    int offset;
    int flags;
} wcdb_query_options;

WCDB_UNITY_EXPORT int wcdb_has_feature(int feature);

WCDB_UNITY_EXPORT wcdb_database* wcdb_open(const char* path, int read_only);
WCDB_UNITY_EXPORT void wcdb_close(wcdb_database* database);
WCDB_UNITY_EXPORT int wcdb_last_error_code(const wcdb_database* database);
WCDB_UNITY_EXPORT const char* wcdb_last_error_message(const wcdb_database* database);
WCDB_UNITY_EXPORT int64_t wcdb_last_insert_rowid(wcdb_database* database);
WCDB_UNITY_EXPORT int wcdb_changes(wcdb_database* database);

WCDB_UNITY_EXPORT wcdb_status wcdb_config_cipher(wcdb_database* database,
                                                 const void* key_bytes,
                                                 int key_length);

WCDB_UNITY_EXPORT wcdb_status wcdb_begin_transaction(wcdb_database* database);
WCDB_UNITY_EXPORT wcdb_status wcdb_commit_transaction(wcdb_database* database);
WCDB_UNITY_EXPORT wcdb_status wcdb_rollback_transaction(wcdb_database* database);

WCDB_UNITY_EXPORT wcdb_status wcdb_execute(wcdb_database* database,
                                           const char* sql,
                                           const wcdb_value* parameters,
                                           int parameter_count);

WCDB_UNITY_EXPORT wcdb_statement* wcdb_prepare(wcdb_database* database, const char* sql);
WCDB_UNITY_EXPORT void wcdb_statement_finalize(wcdb_statement* statement);
WCDB_UNITY_EXPORT wcdb_status wcdb_statement_bind_value(wcdb_statement* statement,
                                                        int index,
                                                        const wcdb_value* value);
WCDB_UNITY_EXPORT void wcdb_statement_clear_bindings(wcdb_statement* statement);
WCDB_UNITY_EXPORT void wcdb_statement_reset(wcdb_statement* statement);
WCDB_UNITY_EXPORT int wcdb_statement_parameter_index(wcdb_statement* statement,
                                                     const char* parameter_name);
WCDB_UNITY_EXPORT int wcdb_statement_column_count(wcdb_statement* statement);
WCDB_UNITY_EXPORT const char* wcdb_statement_column_name(wcdb_statement* statement, int index);
WCDB_UNITY_EXPORT int wcdb_statement_column_type(wcdb_statement* statement, int index);
WCDB_UNITY_EXPORT int64_t wcdb_statement_column_int64(wcdb_statement* statement, int index);
WCDB_UNITY_EXPORT double wcdb_statement_column_double(wcdb_statement* statement, int index);
WCDB_UNITY_EXPORT const char* wcdb_statement_column_text(wcdb_statement* statement, int index);
WCDB_UNITY_EXPORT const void* wcdb_statement_column_blob(wcdb_statement* statement, int index);
WCDB_UNITY_EXPORT int wcdb_statement_column_bytes(wcdb_statement* statement, int index);
WCDB_UNITY_EXPORT wcdb_status wcdb_statement_step(wcdb_statement* statement);

WCDB_UNITY_EXPORT wcdb_status wcdb_create_table(wcdb_database* database,
                                                const char* table_name,
                                                const wcdb_column_def* columns,
                                                int column_count,
                                                int if_not_exists);

WCDB_UNITY_EXPORT wcdb_status wcdb_insert(wcdb_database* database,
                                          const char* table_name,
                                          const char* const* column_names,
                                          const wcdb_value* values,
                                          int column_count,
                                          int or_replace);

WCDB_UNITY_EXPORT wcdb_status wcdb_insert_many(wcdb_database* database,
                                               const char* table_name,
                                               const char* const* column_names,
                                               int column_count,
                                               const wcdb_value* values,
                                               int row_count,
                                               int or_replace);

WCDB_UNITY_EXPORT wcdb_status wcdb_update(wcdb_database* database,
                                          const char* table_name,
                                          const char* const* column_names,
                                          const wcdb_value* values,
                                          int column_count,
                                          const char* where_sql,
                                          const wcdb_value* where_parameters,
                                          int where_parameter_count);

WCDB_UNITY_EXPORT wcdb_status wcdb_delete(wcdb_database* database,
                                          const char* table_name,
                                          const char* where_sql,
                                          const wcdb_value* where_parameters,
                                          int where_parameter_count);

WCDB_UNITY_EXPORT wcdb_statement* wcdb_query_begin(wcdb_database* database,
                                                   const char* table_name,
                                                   const char* const* selected_columns,
                                                   int selected_column_count,
                                                   const wcdb_query_options* options,
                                                   const wcdb_value* where_parameters,
                                                   int where_parameter_count);

#ifdef __cplusplus
}
#endif
