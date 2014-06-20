
set LLVM_DIR=L:\dev\buildtools\android-ndk-r8e\toolchains\llvm-3.2\prebuilt\windows-x86_64\bin

REM %LLVM_DIR%\clang -O3 -emit-llvm basic-c-compile\basic-c-compile.c -IL:\dev\buildtools\android-ndk-r8e\platforms\android-14\arch-arm\usr\include -c -o basic-c-compile.bc

REM %LLVM_DIR%\llc basic-c-compile.bc -march arm -o basic-c-compile.s

REM L:\dev\buildtools\android-ndk-r8e\toolchains\arm-linux-androideabi-4.7\prebuilt\windows-x86_64\bin\arm-linux-androideabi-gcc basic-c-compile.s -o basic-c-compile.exe

%LLVM_DIR%\clang -O3 -target armv7a-none-linux-androideabi --sysroot L:\dev\buildtools\android-ndk-r8e\platforms\android-14\arch-arm -gcc-toolchain L:\dev\buildtools\android-ndk-r8e\toolchains\arm-linux-androideabi-4.7\prebuilt\windows-x86_64 -v -c basic-c-compile\basic-c-compile.c -o basic-c-compile.o

pause