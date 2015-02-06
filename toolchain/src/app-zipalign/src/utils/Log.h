
#if defined(_DEBUG)
#define LOGD(...) fprintf (stdout, __VA_ARGS__)
#define ALOGD(...) fprintf (stdout, __VA_ARGS__)
#else
#define LOGD(...)
#define ALOGD(...)
#endif

#if defined(_DEBUG)
#define LOGV(...) fprintf (stdout, __VA_ARGS__)
#define ALOGV(...) fprintf (stdout, __VA_ARGS__)
#else
#define LOGV(...)
#define ALOGV(...)
#endif

#if defined(_DEBUG) || defined(NDEBUG)
#define LOGW(...) fprintf (stderr, __VA_ARGS__)
#define ALOGW(...) fprintf (stderr, __VA_ARGS__)
#else
#define LOGW(...)
#define ALOGW(...)
#endif

#if defined(_DEBUG) || defined(NDEBUG)
#define LOGE(...) fprintf (stderr, __VA_ARGS__)
#define ALOGE(...) fprintf (stderr, __VA_ARGS__)
#else
#define LOGE(...)
#define ALOGE(...)
#endif
