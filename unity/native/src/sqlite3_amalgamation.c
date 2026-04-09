#if defined(__APPLE__) || defined(__linux__) || defined(__unix__) || defined(__ANDROID__)
#include <errno.h>
#include <fcntl.h>
#include <sys/mman.h>
#include <sys/stat.h>
#include <sys/time.h>
#include <sys/types.h>
#include <unistd.h>
#endif

#include "../../../wcdb/deprecated/android/sqlcipher/sqlite3.c"
