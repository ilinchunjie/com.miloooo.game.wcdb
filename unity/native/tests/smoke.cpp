#include "wcdb_unity.h"

#include <cstdio>
#include <cstring>
#include <cstdlib>
#include <string>

static wcdb_value make_int(long long value)
{
    wcdb_value result = {};
    result.type = WCDB_VALUE_INT64;
    result.int64_value = value;
    return result;
}

static wcdb_value make_text(const char* value)
{
    wcdb_value result = {};
    result.type = WCDB_VALUE_TEXT;
    result.text_value = value;
    return result;
}

int main()
{
    std::string path;
#if defined(_WIN32)
    const char* temp = std::getenv("TEMP");
    path = (temp != nullptr ? temp : ".");
    path += "\\wcdb_unity_smoke.db";
#else
    path = "/tmp/wcdb_unity_smoke.db";
#endif

    wcdb_database* database = wcdb_open(path.c_str(), 0);
    if (database == nullptr) {
        return 1;
    }

    wcdb_execute(database, "DROP TABLE IF EXISTS smoke_players", nullptr, 0);

    wcdb_column_def columns[2] = {};
    columns[0].name = "id";
    columns[0].declared_type = "INTEGER";
    columns[0].flags = WCDB_COLUMN_PRIMARY_KEY;
    columns[1].name = "name";
    columns[1].declared_type = "TEXT";
    columns[1].flags = WCDB_COLUMN_NOT_NULL;

    if (wcdb_create_table(database, "smoke_players", columns, 2, 1) != WCDB_STATUS_OK) {
        std::fprintf(stderr, "%s\n", wcdb_last_error_message(database));
        wcdb_close(database);
        return 2;
    }

    const char* insert_columns[2] = { "id", "name" };
    wcdb_value insert_values[2] = { make_int(1), make_text("smoke") };
    if (wcdb_insert(database, "smoke_players", insert_columns, insert_values, 2, 0)
        != WCDB_STATUS_OK) {
        std::fprintf(stderr, "%s\n", wcdb_last_error_message(database));
        wcdb_close(database);
        return 3;
    }

    wcdb_query_options options = {};
    options.where_sql = "id = ?";
    wcdb_value where_values[1] = { make_int(1) };
    wcdb_statement* statement
    = wcdb_query_begin(database, "smoke_players", nullptr, 0, &options, where_values, 1);
    if (statement == nullptr) {
        std::fprintf(stderr, "%s\n", wcdb_last_error_message(database));
        wcdb_close(database);
        return 4;
    }

    if (wcdb_statement_step(statement) != WCDB_STATUS_ROW) {
        std::fprintf(stderr, "%s\n", wcdb_last_error_message(database));
        wcdb_statement_finalize(statement);
        wcdb_close(database);
        return 5;
    }

    const char* fetched = wcdb_statement_column_text(statement, 1);
    if (fetched == nullptr || std::strcmp(fetched, "smoke") != 0) {
        wcdb_statement_finalize(statement);
        wcdb_close(database);
        return 6;
    }

    wcdb_statement_finalize(statement);
    wcdb_close(database);
    return 0;
}
