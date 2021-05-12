#include <jni.h>

extern "C" JNIEXPORT void JNICALL Java_com_example_hellogdbserver_HelloGdbServer_invokeCrash(JNIEnv *env, jclass clazz)
{
	int *crasher = 0x0;
	*crasher = 0xdeaddead;
}
