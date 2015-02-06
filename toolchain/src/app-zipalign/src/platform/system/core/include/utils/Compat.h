/*
 * Copyright (C) 2010 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#ifndef __LIB_UTILS_COMPAT_H
#define __LIB_UTILS_COMPAT_H

#include <unistd.h>

#if defined(__APPLE__)

/* Mac OS has always had a 64-bit off_t, so it doesn't have off64_t. */

typedef off_t off64_t;

static inline off64_t lseek64(int fd, off64_t offset, int whence) {
    return lseek(fd, offset, whence);
}

static inline ssize_t pread64(int fd, void* buf, size_t nbytes, off64_t offset) {
    return pread(fd, buf, nbytes, offset);
}

#elif defined(_WIN32)

#include <sys/types.h>
#include <stdio.h>

/* Compatibility definitions for non-Linux (i.e., BSD-based) hosts. */
#ifndef HAVE_OFF64_T
#if _FILE_OFFSET_BITS < 64
#error "_FILE_OFFSET_BITS < 64; large files are not supported on this platform"
#endif /* _FILE_OFFSET_BITS < 64 */

typedef __int64 off64_t;
#define _OFF_T_DEFINED
#endif

static inline off64_t lseek64(int fd, off64_t offset, int whence) {
    return ::_lseeki64(fd, offset, whence);
}

static inline off64_t tell64(int fd) {
    return ::_telli64(fd);
}

static inline int chsize64(int fd, int64_t size) {
    return _chsize_s(fd, size);
}

static inline char getc(int fd) {
    char c;
    ::read(fd, &c, sizeof(c));
    return c;
}

#endif

#ifndef __BEGIN_DECLS
#if __cplusplus
#define __BEGIN_DECLS extern "C" {
#else
#define __BEGIN_DECLS 
#endif
#endif

#ifndef __END_DECLS
#if __cplusplus
#define __END_DECLS }
#else
#define __END_DECLS 
#endif
#endif

#if HAVE_PRINTF_ZD
#  define ZD "%zd"
#  define ZD_TYPE ssize_t
#else
#  define ZD "%ld"
#  define ZD_TYPE long
#endif

/*
 * Needed for cases where something should be constexpr if possible, but not
 * being constexpr is fine if in pre-C++11 code (such as a const static float
 * member variable).
 */
#if __cplusplus >= 201103L
#define CONSTEXPR constexpr
#else
#define CONSTEXPR
#endif

/*
 * TEMP_FAILURE_RETRY is defined by some, but not all, versions of
 * <unistd.h>. (Alas, it is not as standard as we'd hoped!) So, if it's
 * not already defined, then define it here.
 */
#ifndef TEMP_FAILURE_RETRY
/* Used to retry syscalls that can return EINTR. */
#define TEMP_FAILURE_RETRY(exp) ({         \
    typeof (exp) _rc;                      \
    do {                                   \
        _rc = (exp);                       \
    } while (_rc == -1 && errno == EINTR); \
    _rc; })
#endif

#endif /* __LIB_UTILS_COMPAT_H */
