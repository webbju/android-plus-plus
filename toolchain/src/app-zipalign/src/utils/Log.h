
#if defined(_DEBUG)
#define LOGD(...) printf (__VA_ARGS__)
#define ALOGD(...) printf (__VA_ARGS__)
#else
#define LOGD(...)
#define ALOGD(...)
#endif

#if defined(_DEBUG)
#define LOGV(...) printf (__VA_ARGS__)
#define ALOGV(...) printf (__VA_ARGS__)
#else
#define LOGV(...)
#define ALOGV(...)
#endif

#if defined(_DEBUG) || defined(NDEBUG)
#define LOGW(...) printf (__VA_ARGS__)
#define ALOGW(...) printf (__VA_ARGS__)
#else
#define LOGW(...)
#define ALOGW(...)
#endif

#if defined(_DEBUG) || defined(NDEBUG)
#define LOGE(...) printf (__VA_ARGS__)
#define ALOGE(...) printf (__VA_ARGS__)
#else
#define LOGE(...)
#define ALOGE(...)
#endif
