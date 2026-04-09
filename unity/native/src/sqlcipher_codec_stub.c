#include "sqlite3.h"

#if defined(SQLITE_HAS_CODEC)

#include "crypto.h"
#include "sqlcipher.h"

#include <string.h>

static int unity_stub_activate(void* ctx)
{
    (void) ctx;
    return SQLITE_OK;
}

static int unity_stub_deactivate(void* ctx)
{
    (void) ctx;
    return SQLITE_OK;
}

static const char* unity_stub_provider_name(void* ctx)
{
    (void) ctx;
    return "unity-stub";
}

static int unity_stub_add_random(void* ctx, void* buffer, int length)
{
    (void) ctx;
    (void) buffer;
    (void) length;
    return SQLITE_OK;
}

static int unity_stub_random(void* ctx, void* buffer, int length)
{
    (void) ctx;
    memset(buffer, 0, (size_t) length);
    return SQLITE_OK;
}

static int unity_stub_hmac(void* ctx,
                           int algorithm,
                           unsigned char* hmac_key,
                           int key_sz,
                           unsigned char* in,
                           int in_sz,
                           unsigned char* in2,
                           int in2_sz,
                           unsigned char* out)
{
    (void) ctx;
    (void) algorithm;
    (void) hmac_key;
    (void) key_sz;
    (void) in;
    (void) in_sz;
    (void) in2;
    (void) in2_sz;
    (void) out;
    return SQLITE_ERROR;
}

static int unity_stub_kdf(void* ctx,
                          int algorithm,
                          const unsigned char* pass,
                          int pass_sz,
                          unsigned char* salt,
                          int salt_sz,
                          int workfactor,
                          int key_sz,
                          unsigned char* key)
{
    (void) ctx;
    (void) algorithm;
    (void) pass;
    (void) pass_sz;
    (void) salt;
    (void) salt_sz;
    (void) workfactor;
    if (key != NULL && key_sz > 0) {
        memset(key, 0, (size_t) key_sz);
    }
    return SQLITE_ERROR;
}

static int unity_stub_cipher(void* ctx,
                             int mode,
                             unsigned char* key,
                             int key_sz,
                             unsigned char* iv,
                             unsigned char* in,
                             int in_sz,
                             unsigned char* out)
{
    (void) ctx;
    (void) mode;
    (void) key;
    (void) key_sz;
    (void) iv;
    if (out != NULL && in != NULL && in_sz > 0) {
        memcpy(out, in, (size_t) in_sz);
    }
    return SQLITE_ERROR;
}

static const char* unity_stub_cipher_name(void* ctx)
{
    (void) ctx;
    return "disabled";
}

static int unity_stub_key_size(void* ctx)
{
    (void) ctx;
    return 32;
}

static int unity_stub_iv_size(void* ctx)
{
    (void) ctx;
    return 16;
}

static int unity_stub_block_size(void* ctx)
{
    (void) ctx;
    return 16;
}

static int unity_stub_hmac_size(void* ctx, int algorithm)
{
    (void) ctx;
    (void) algorithm;
    return 0;
}

static int unity_stub_ctx_copy(void* target_ctx, void* source_ctx)
{
    (void) target_ctx;
    (void) source_ctx;
    return SQLITE_OK;
}

static int unity_stub_ctx_cmp(void* c1, void* c2)
{
    (void) c1;
    (void) c2;
    return 1;
}

static int unity_stub_ctx_init(void** ctx)
{
    if (ctx != NULL) {
        *ctx = NULL;
    }
    return SQLITE_OK;
}

static int unity_stub_ctx_free(void** ctx)
{
    if (ctx != NULL) {
        *ctx = NULL;
    }
    return SQLITE_OK;
}

static int unity_stub_fips_status(void* ctx)
{
    (void) ctx;
    return 0;
}

static const char* unity_stub_provider_version(void* ctx)
{
    (void) ctx;
    return "disabled";
}

int sqlite3_key(sqlite3* db, const void* pKey, int nKey)
{
    (void) db;
    (void) pKey;
    (void) nKey;
    return SQLITE_ERROR;
}

int sqlite3_key_v2(sqlite3* db, const char* zDb, const void* pKey, int nKey)
{
    (void) db;
    (void) zDb;
    (void) pKey;
    (void) nKey;
    return SQLITE_ERROR;
}

int sqlite3_rekey(sqlite3* db, const void* pKey, int nKey)
{
    (void) db;
    (void) pKey;
    (void) nKey;
    return SQLITE_ERROR;
}

int sqlite3_rekey_v2(sqlite3* db, const char* zDb, const void* pKey, int nKey)
{
    (void) db;
    (void) zDb;
    (void) pKey;
    (void) nKey;
    return SQLITE_ERROR;
}

int sqlcipher_codec_pragma(sqlite3* db, int iDb, Parse* pParse, const char* zLeft, const char* zRight)
{
    (void) db;
    (void) iDb;
    (void) pParse;
    (void) zLeft;
    (void) zRight;
    return 0;
}

int sqlcipher_find_db_index(sqlite3* db, const char* zDb)
{
    int db_index;
    if (db == NULL || zDb == NULL) {
        return 0;
    }
    for (db_index = 0; db_index < db->nDb; ++db_index) {
        const char* schema_name = db->aDb[db_index].zDbSName;
        if (schema_name != NULL && strcmp(schema_name, zDb) == 0) {
            return db_index;
        }
    }
    return 0;
}

void sqlite3CodecGetKey(sqlite3* db, int nDb, void** zKey, int* nKey)
{
    (void) db;
    (void) nDb;
    if (zKey != NULL) {
        *zKey = NULL;
    }
    if (nKey != NULL) {
        *nKey = 0;
    }
}

void sqlite3_activate_see(const char* in)
{
    (void) in;
}

void* sqlite3_getCipherContext(sqlite3* db, const char* schema)
{
    (void) db;
    (void) schema;
    return NULL;
}

int sqlcipher_codec_ctx_get_reservesize(codec_ctx* ctx)
{
    (void) ctx;
    return 0;
}

void* sqlite3Codec(void* iCtx, void* data, unsigned int pgno, int mode)
{
    (void) iCtx;
    (void) pgno;
    (void) mode;
    return data;
}

void sqlcipher_exportFunc(sqlite3_context* context, int argc, sqlite3_value** argv)
{
    (void) argc;
    (void) argv;
    sqlite3_result_error(context, "sqlcipher_export is disabled in this build.", -1);
}

void sqlcipher_activate(void)
{
    sqlcipher_provider* provider = sqlcipher_malloc(sizeof(sqlcipher_provider));
    if (provider == NULL) {
        return;
    }

    memset(provider, 0, sizeof(sqlcipher_provider));
    provider->activate = unity_stub_activate;
    provider->deactivate = unity_stub_deactivate;
    provider->get_provider_name = unity_stub_provider_name;
    provider->add_random = unity_stub_add_random;
    provider->random = unity_stub_random;
    provider->hmac = unity_stub_hmac;
    provider->kdf = unity_stub_kdf;
    provider->cipher = unity_stub_cipher;
    provider->get_cipher = unity_stub_cipher_name;
    provider->get_key_sz = unity_stub_key_size;
    provider->get_iv_sz = unity_stub_iv_size;
    provider->get_block_sz = unity_stub_block_size;
    provider->get_hmac_sz = unity_stub_hmac_size;
    provider->ctx_copy = unity_stub_ctx_copy;
    provider->ctx_cmp = unity_stub_ctx_cmp;
    provider->ctx_init = unity_stub_ctx_init;
    provider->ctx_free = unity_stub_ctx_free;
    provider->fips_status = unity_stub_fips_status;
    provider->get_provider_version = unity_stub_provider_version;

    sqlcipher_register_provider(provider);
}

#endif
