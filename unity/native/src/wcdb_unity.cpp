#include "wcdb_unity.h"

#include "sqlite3.h"

#include <algorithm>
#include <sstream>
#include <string>
#include <vector>

struct wcdb_database {
    sqlite3* handle = nullptr;
    int last_error_code = SQLITE_OK;
    std::string bootstrap_error;
};

struct wcdb_statement {
    wcdb_database* owner = nullptr;
    sqlite3_stmt* handle = nullptr;
    int last_step_code = SQLITE_OK;
};

namespace {

constexpr const char* kBeginImmediate = "BEGIN IMMEDIATE TRANSACTION";
constexpr const char* kCommit = "COMMIT";
constexpr const char* kRollback = "ROLLBACK";

std::string quote_identifier(const char* raw)
{
    std::string result = "\"";
    if (raw != nullptr) {
        for (const char* cursor = raw; *cursor != '\0'; ++cursor) {
            if (*cursor == '"') {
                result += "\"\"";
            } else {
                result.push_back(*cursor);
            }
        }
    }
    result.push_back('"');
    return result;
}

bool has_text(const char* text)
{
    return text != nullptr && text[0] != '\0';
}

void set_error(wcdb_database* database, int rc)
{
    if (database != nullptr) {
        database->last_error_code = rc;
    }
}

wcdb_status to_status(int rc)
{
    if (rc == SQLITE_OK) {
        return WCDB_STATUS_OK;
    }
    if (rc == SQLITE_ROW) {
        return WCDB_STATUS_ROW;
    }
    if (rc == SQLITE_DONE) {
        return WCDB_STATUS_DONE;
    }
    return WCDB_STATUS_ERROR;
}

wcdb_status bind_value(sqlite3_stmt* statement, int index, const wcdb_value* value)
{
    if (value == nullptr) {
        return to_status(sqlite3_bind_null(statement, index));
    }

    switch (value->type) {
    case WCDB_VALUE_NULL:
        return to_status(sqlite3_bind_null(statement, index));
    case WCDB_VALUE_INT64:
        return to_status(sqlite3_bind_int64(statement, index, value->int64_value));
    case WCDB_VALUE_DOUBLE:
        return to_status(sqlite3_bind_double(statement, index, value->double_value));
    case WCDB_VALUE_TEXT:
        return to_status(sqlite3_bind_text(
        statement, index, value->text_value, -1, SQLITE_TRANSIENT));
    case WCDB_VALUE_BLOB:
        return to_status(sqlite3_bind_blob(statement,
                                           index,
                                           value->blob_value.bytes,
                                           value->blob_value.length,
                                           SQLITE_TRANSIENT));
    case WCDB_VALUE_BOOL:
        return to_status(sqlite3_bind_int(statement, index, value->bool_value ? 1 : 0));
    default:
        return WCDB_STATUS_ERROR;
    }
}

wcdb_status bind_values(sqlite3_stmt* statement, const wcdb_value* values, int count, int offset)
{
    for (int index = 0; index < count; ++index) {
        wcdb_status status = bind_value(statement, offset + index + 1, &values[index]);
        if (status != WCDB_STATUS_OK) {
            return status;
        }
    }
    return WCDB_STATUS_OK;
}

wcdb_status prepare_statement(wcdb_database* database,
                              const std::string& sql,
                              sqlite3_stmt** statement)
{
    int rc = sqlite3_prepare_v2(database->handle, sql.c_str(), -1, statement, nullptr);
    set_error(database, rc);
    return to_status(rc);
}

wcdb_status execute_literal(wcdb_database* database, const char* sql)
{
    return wcdb_execute(database, sql, nullptr, 0);
}

bool database_is_in_transaction(wcdb_database* database)
{
    return sqlite3_get_autocommit(database->handle) == 0;
}

std::string build_insert_sql(const char* table_name,
                             const char* const* column_names,
                             int column_count,
                             int or_replace)
{
    std::ostringstream sql;
    sql << "INSERT ";
    if (or_replace) {
        sql << "OR REPLACE ";
    }
    sql << "INTO " << quote_identifier(table_name) << " (";
    for (int index = 0; index < column_count; ++index) {
        if (index > 0) {
            sql << ", ";
        }
        sql << quote_identifier(column_names[index]);
    }
    sql << ") VALUES (";
    for (int index = 0; index < column_count; ++index) {
        if (index > 0) {
            sql << ", ";
        }
        sql << '?';
    }
    sql << ")";
    return sql.str();
}

} // namespace

extern "C" {

int wcdb_has_feature(int feature)
{
    switch (feature) {
    case WCDB_FEATURE_CIPHER:
#if defined(SQLITE_HAS_CODEC)
        return 1;
#else
        return 0;
#endif
    case WCDB_FEATURE_JSON1:
#if defined(SQLITE_ENABLE_JSON1)
        return 1;
#else
        return 0;
#endif
    case WCDB_FEATURE_FTS:
#if defined(SQLITE_ENABLE_FTS5)
        return 1;
#else
        return 0;
#endif
    case WCDB_FEATURE_RTREE:
#if defined(SQLITE_ENABLE_RTREE)
        return 1;
#else
        return 0;
#endif
    case WCDB_FEATURE_SESSION:
#if defined(SQLITE_ENABLE_SESSION)
        return 1;
#else
        return 0;
#endif
    case WCDB_FEATURE_PREUPDATE_HOOK:
#if defined(SQLITE_ENABLE_PREUPDATE_HOOK)
        return 1;
#else
        return 0;
#endif
    case WCDB_FEATURE_ZSTD:
    case WCDB_FEATURE_TRACE:
    default:
        return 0;
    }
}

wcdb_database* wcdb_open(const char* path, int read_only)
{
    wcdb_database* database = new wcdb_database();
    int flags = read_only ? SQLITE_OPEN_READONLY : (SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE);
    int rc = sqlite3_open_v2(path, &database->handle, flags, nullptr);
    database->last_error_code = rc;
    if (database->handle != nullptr) {
        sqlite3_extended_result_codes(database->handle, 1);
    } else if (rc != SQLITE_OK) {
        database->bootstrap_error = "sqlite3_open_v2 failed";
    }
    return database;
}

void wcdb_close(wcdb_database* database)
{
    if (database == nullptr) {
        return;
    }
    if (database->handle != nullptr) {
        sqlite3_close_v2(database->handle);
        database->handle = nullptr;
    }
    delete database;
}

int wcdb_last_error_code(const wcdb_database* database)
{
    return database == nullptr ? SQLITE_MISUSE : database->last_error_code;
}

const char* wcdb_last_error_message(const wcdb_database* database)
{
    if (database == nullptr) {
        return "wcdb_database is null";
    }
    if (database->handle != nullptr) {
        return sqlite3_errmsg(database->handle);
    }
    if (!database->bootstrap_error.empty()) {
        return database->bootstrap_error.c_str();
    }
    return "database handle is closed";
}

int64_t wcdb_last_insert_rowid(wcdb_database* database)
{
    if (database == nullptr || database->handle == nullptr) {
        return 0;
    }
    return sqlite3_last_insert_rowid(database->handle);
}

int wcdb_changes(wcdb_database* database)
{
    if (database == nullptr || database->handle == nullptr) {
        return 0;
    }
    return sqlite3_changes(database->handle);
}

wcdb_status wcdb_config_cipher(wcdb_database* database, const void* key_bytes, int key_length)
{
    if (database == nullptr || database->handle == nullptr) {
        return WCDB_STATUS_ERROR;
    }
#if defined(SQLITE_HAS_CODEC)
    int rc = sqlite3_key(database->handle, key_bytes, key_length);
    set_error(database, rc);
    return to_status(rc);
#else
    (void) key_bytes;
    (void) key_length;
    database->last_error_code = SQLITE_MISUSE;
    database->bootstrap_error = "Cipher support is not enabled in this build.";
    return WCDB_STATUS_ERROR;
#endif
}

wcdb_status wcdb_begin_transaction(wcdb_database* database)
{
    return execute_literal(database, kBeginImmediate);
}

wcdb_status wcdb_commit_transaction(wcdb_database* database)
{
    return execute_literal(database, kCommit);
}

wcdb_status wcdb_rollback_transaction(wcdb_database* database)
{
    return execute_literal(database, kRollback);
}

wcdb_status wcdb_execute(wcdb_database* database,
                         const char* sql,
                         const wcdb_value* parameters,
                         int parameter_count)
{
    if (database == nullptr || database->handle == nullptr || !has_text(sql)) {
        return WCDB_STATUS_ERROR;
    }

    sqlite3_stmt* statement = nullptr;
    wcdb_status prepare_status = prepare_statement(database, sql, &statement);
    if (prepare_status != WCDB_STATUS_OK) {
        return prepare_status;
    }

    wcdb_status bind_status = bind_values(statement, parameters, parameter_count, 0);
    if (bind_status != WCDB_STATUS_OK) {
        sqlite3_finalize(statement);
        set_error(database, sqlite3_errcode(database->handle));
        return bind_status;
    }

    int rc = SQLITE_OK;
    do {
        rc = sqlite3_step(statement);
    } while (rc == SQLITE_ROW);

    sqlite3_finalize(statement);
    set_error(database, rc == SQLITE_DONE ? SQLITE_OK : rc);
    return to_status(rc == SQLITE_DONE ? SQLITE_OK : rc);
}

wcdb_statement* wcdb_prepare(wcdb_database* database, const char* sql)
{
    if (database == nullptr || database->handle == nullptr || !has_text(sql)) {
        return nullptr;
    }

    wcdb_statement* statement = new wcdb_statement();
    statement->owner = database;
    if (prepare_statement(database, sql, &statement->handle) != WCDB_STATUS_OK) {
        delete statement;
        return nullptr;
    }
    statement->last_step_code = SQLITE_OK;
    return statement;
}

void wcdb_statement_finalize(wcdb_statement* statement)
{
    if (statement == nullptr) {
        return;
    }
    if (statement->handle != nullptr) {
        sqlite3_finalize(statement->handle);
        statement->handle = nullptr;
    }
    delete statement;
}

wcdb_status wcdb_statement_bind_value(wcdb_statement* statement, int index, const wcdb_value* value)
{
    if (statement == nullptr || statement->handle == nullptr) {
        return WCDB_STATUS_ERROR;
    }
    wcdb_status status = bind_value(statement->handle, index, value);
    if (status != WCDB_STATUS_OK) {
        set_error(statement->owner, sqlite3_errcode(statement->owner->handle));
    }
    return status;
}

void wcdb_statement_clear_bindings(wcdb_statement* statement)
{
    if (statement == nullptr || statement->handle == nullptr) {
        return;
    }
    sqlite3_clear_bindings(statement->handle);
}

void wcdb_statement_reset(wcdb_statement* statement)
{
    if (statement == nullptr || statement->handle == nullptr) {
        return;
    }
    sqlite3_reset(statement->handle);
    statement->last_step_code = SQLITE_OK;
}

int wcdb_statement_parameter_index(wcdb_statement* statement, const char* parameter_name)
{
    if (statement == nullptr || statement->handle == nullptr || parameter_name == nullptr) {
        return 0;
    }
    return sqlite3_bind_parameter_index(statement->handle, parameter_name);
}

int wcdb_statement_column_count(wcdb_statement* statement)
{
    if (statement == nullptr || statement->handle == nullptr) {
        return 0;
    }
    return sqlite3_column_count(statement->handle);
}

const char* wcdb_statement_column_name(wcdb_statement* statement, int index)
{
    if (statement == nullptr || statement->handle == nullptr) {
        return nullptr;
    }
    return sqlite3_column_name(statement->handle, index);
}

int wcdb_statement_column_type(wcdb_statement* statement, int index)
{
    if (statement == nullptr || statement->handle == nullptr) {
        return WCDB_VALUE_NULL;
    }
    switch (sqlite3_column_type(statement->handle, index)) {
    case SQLITE_INTEGER:
        return WCDB_VALUE_INT64;
    case SQLITE_FLOAT:
        return WCDB_VALUE_DOUBLE;
    case SQLITE_TEXT:
        return WCDB_VALUE_TEXT;
    case SQLITE_BLOB:
        return WCDB_VALUE_BLOB;
    case SQLITE_NULL:
    default:
        return WCDB_VALUE_NULL;
    }
}

int64_t wcdb_statement_column_int64(wcdb_statement* statement, int index)
{
    if (statement == nullptr || statement->handle == nullptr) {
        return 0;
    }
    return sqlite3_column_int64(statement->handle, index);
}

double wcdb_statement_column_double(wcdb_statement* statement, int index)
{
    if (statement == nullptr || statement->handle == nullptr) {
        return 0.0;
    }
    return sqlite3_column_double(statement->handle, index);
}

const char* wcdb_statement_column_text(wcdb_statement* statement, int index)
{
    if (statement == nullptr || statement->handle == nullptr) {
        return nullptr;
    }
    return reinterpret_cast<const char*>(sqlite3_column_text(statement->handle, index));
}

const void* wcdb_statement_column_blob(wcdb_statement* statement, int index)
{
    if (statement == nullptr || statement->handle == nullptr) {
        return nullptr;
    }
    return sqlite3_column_blob(statement->handle, index);
}

int wcdb_statement_column_bytes(wcdb_statement* statement, int index)
{
    if (statement == nullptr || statement->handle == nullptr) {
        return 0;
    }
    return sqlite3_column_bytes(statement->handle, index);
}

wcdb_status wcdb_statement_step(wcdb_statement* statement)
{
    if (statement == nullptr || statement->handle == nullptr) {
        return WCDB_STATUS_ERROR;
    }
    int rc = sqlite3_step(statement->handle);
    statement->last_step_code = rc;
    set_error(statement->owner, (rc == SQLITE_ROW || rc == SQLITE_DONE) ? SQLITE_OK : rc);
    return to_status(rc);
}

wcdb_status wcdb_create_table(wcdb_database* database,
                              const char* table_name,
                              const wcdb_column_def* columns,
                              int column_count,
                              int if_not_exists)
{
    if (database == nullptr || !has_text(table_name) || columns == nullptr || column_count <= 0) {
        return WCDB_STATUS_ERROR;
    }

    std::ostringstream sql;
    sql << "CREATE TABLE ";
    if (if_not_exists) {
        sql << "IF NOT EXISTS ";
    }
    sql << quote_identifier(table_name) << " (";

    for (int index = 0; index < column_count; ++index) {
        const wcdb_column_def& column = columns[index];
        if (index > 0) {
            sql << ", ";
        }
        sql << quote_identifier(column.name);
        if (has_text(column.declared_type)) {
            sql << " " << column.declared_type;
        }
        if ((column.flags & WCDB_COLUMN_PRIMARY_KEY) != 0) {
            sql << " PRIMARY KEY";
        }
        if ((column.flags & WCDB_COLUMN_AUTOINCREMENT) != 0) {
            sql << " AUTOINCREMENT";
        }
        if ((column.flags & WCDB_COLUMN_NOT_NULL) != 0) {
            sql << " NOT NULL";
        }
        if ((column.flags & WCDB_COLUMN_UNIQUE) != 0) {
            sql << " UNIQUE";
        }
        if (has_text(column.default_value_sql)) {
            sql << " DEFAULT " << column.default_value_sql;
        }
    }

    sql << ")";
    return wcdb_execute(database, sql.str().c_str(), nullptr, 0);
}

wcdb_status wcdb_insert(wcdb_database* database,
                        const char* table_name,
                        const char* const* column_names,
                        const wcdb_value* values,
                        int column_count,
                        int or_replace)
{
    if (database == nullptr || !has_text(table_name) || column_names == nullptr || values == nullptr
        || column_count <= 0) {
        return WCDB_STATUS_ERROR;
    }

    sqlite3_stmt* statement = nullptr;
    std::string sql = build_insert_sql(table_name, column_names, column_count, or_replace);
    wcdb_status prepare_status = prepare_statement(database, sql, &statement);
    if (prepare_status != WCDB_STATUS_OK) {
        return prepare_status;
    }

    wcdb_status bind_status = bind_values(statement, values, column_count, 0);
    if (bind_status != WCDB_STATUS_OK) {
        sqlite3_finalize(statement);
        set_error(database, sqlite3_errcode(database->handle));
        return bind_status;
    }

    int rc = sqlite3_step(statement);
    sqlite3_finalize(statement);
    set_error(database, rc == SQLITE_DONE ? SQLITE_OK : rc);
    return to_status(rc == SQLITE_DONE ? SQLITE_OK : rc);
}

wcdb_status wcdb_insert_many(wcdb_database* database,
                             const char* table_name,
                             const char* const* column_names,
                             int column_count,
                             const wcdb_value* values,
                             int row_count,
                             int or_replace)
{
    if (database == nullptr || !has_text(table_name) || column_names == nullptr || values == nullptr
        || column_count <= 0 || row_count <= 0) {
        return WCDB_STATUS_ERROR;
    }

    const bool owns_transaction = !database_is_in_transaction(database);
    wcdb_status status = WCDB_STATUS_OK;
    if (owns_transaction) {
        status = wcdb_begin_transaction(database);
        if (status != WCDB_STATUS_OK) {
            return status;
        }
    }

    sqlite3_stmt* statement = nullptr;
    std::string sql = build_insert_sql(table_name, column_names, column_count, or_replace);
    status = prepare_statement(database, sql, &statement);
    if (status != WCDB_STATUS_OK) {
        if (owns_transaction) {
            wcdb_rollback_transaction(database);
        }
        return status;
    }

    for (int row = 0; row < row_count; ++row) {
        const wcdb_value* row_values = values + (row * column_count);
        status = bind_values(statement, row_values, column_count, 0);
        if (status != WCDB_STATUS_OK) {
            sqlite3_finalize(statement);
            if (owns_transaction) {
                wcdb_rollback_transaction(database);
            }
            return status;
        }
        int rc = sqlite3_step(statement);
        if (rc != SQLITE_DONE) {
            sqlite3_finalize(statement);
            set_error(database, rc);
            if (owns_transaction) {
                wcdb_rollback_transaction(database);
            }
            return to_status(rc);
        }
        sqlite3_reset(statement);
        sqlite3_clear_bindings(statement);
    }

    sqlite3_finalize(statement);
    if (owns_transaction) {
        status = wcdb_commit_transaction(database);
    }
    return status;
}

wcdb_status wcdb_update(wcdb_database* database,
                        const char* table_name,
                        const char* const* column_names,
                        const wcdb_value* values,
                        int column_count,
                        const char* where_sql,
                        const wcdb_value* where_parameters,
                        int where_parameter_count)
{
    if (database == nullptr || !has_text(table_name) || column_names == nullptr || values == nullptr
        || column_count <= 0) {
        return WCDB_STATUS_ERROR;
    }

    std::ostringstream sql;
    sql << "UPDATE " << quote_identifier(table_name) << " SET ";
    for (int index = 0; index < column_count; ++index) {
        if (index > 0) {
            sql << ", ";
        }
        sql << quote_identifier(column_names[index]) << " = ?";
    }
    if (has_text(where_sql)) {
        sql << " WHERE " << where_sql;
    }

    sqlite3_stmt* statement = nullptr;
    wcdb_status status = prepare_statement(database, sql.str(), &statement);
    if (status != WCDB_STATUS_OK) {
        return status;
    }

    status = bind_values(statement, values, column_count, 0);
    if (status == WCDB_STATUS_OK) {
        status = bind_values(statement, where_parameters, where_parameter_count, column_count);
    }
    if (status != WCDB_STATUS_OK) {
        sqlite3_finalize(statement);
        set_error(database, sqlite3_errcode(database->handle));
        return status;
    }

    int rc = sqlite3_step(statement);
    sqlite3_finalize(statement);
    set_error(database, rc == SQLITE_DONE ? SQLITE_OK : rc);
    return to_status(rc == SQLITE_DONE ? SQLITE_OK : rc);
}

wcdb_status wcdb_delete(wcdb_database* database,
                        const char* table_name,
                        const char* where_sql,
                        const wcdb_value* where_parameters,
                        int where_parameter_count)
{
    if (database == nullptr || !has_text(table_name)) {
        return WCDB_STATUS_ERROR;
    }

    std::ostringstream sql;
    sql << "DELETE FROM " << quote_identifier(table_name);
    if (has_text(where_sql)) {
        sql << " WHERE " << where_sql;
    }

    sqlite3_stmt* statement = nullptr;
    wcdb_status status = prepare_statement(database, sql.str(), &statement);
    if (status != WCDB_STATUS_OK) {
        return status;
    }

    status = bind_values(statement, where_parameters, where_parameter_count, 0);
    if (status != WCDB_STATUS_OK) {
        sqlite3_finalize(statement);
        set_error(database, sqlite3_errcode(database->handle));
        return status;
    }

    int rc = sqlite3_step(statement);
    sqlite3_finalize(statement);
    set_error(database, rc == SQLITE_DONE ? SQLITE_OK : rc);
    return to_status(rc == SQLITE_DONE ? SQLITE_OK : rc);
}

wcdb_statement* wcdb_query_begin(wcdb_database* database,
                                 const char* table_name,
                                 const char* const* selected_columns,
                                 int selected_column_count,
                                 const wcdb_query_options* options,
                                 const wcdb_value* where_parameters,
                                 int where_parameter_count)
{
    if (database == nullptr || !has_text(table_name)) {
        return nullptr;
    }

    std::ostringstream sql;
    sql << "SELECT ";
    if (selected_column_count <= 0 || selected_columns == nullptr) {
        sql << '*';
    } else {
        for (int index = 0; index < selected_column_count; ++index) {
            if (index > 0) {
                sql << ", ";
            }
            sql << quote_identifier(selected_columns[index]);
        }
    }
    sql << " FROM " << quote_identifier(table_name);

    if (options != nullptr) {
        if (has_text(options->where_sql)) {
            sql << " WHERE " << options->where_sql;
        }
        if (has_text(options->order_by_sql)) {
            sql << " ORDER BY " << options->order_by_sql;
        }
        if ((options->flags & WCDB_QUERY_OPTION_HAS_LIMIT) != 0) {
            sql << " LIMIT " << options->limit;
        }
        if ((options->flags & WCDB_QUERY_OPTION_HAS_OFFSET) != 0) {
            sql << " OFFSET " << options->offset;
        }
    }

    wcdb_statement* statement = wcdb_prepare(database, sql.str().c_str());
    if (statement == nullptr) {
        return nullptr;
    }

    if (bind_values(statement->handle, where_parameters, where_parameter_count, 0)
        != WCDB_STATUS_OK) {
        set_error(database, sqlite3_errcode(database->handle));
        wcdb_statement_finalize(statement);
        return nullptr;
    }
    return statement;
}

} // extern "C"
