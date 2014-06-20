
set LLVM_DIR=L:\dev\buildtools\android-ndk-r8e\toolchains\llvm-3.2\prebuilt\windows-x86_64\bin

REM %LLVM_DIR%\clang -O3 -emit-llvm basic-c-compile\basic-c-compile.c -IL:\dev\buildtools\android-ndk-r8e\platforms\android-14\arch-arm\usr\include -c -o basic-c-compile.bc

REM %LLVM_DIR%\llc basic-c-compile.bc -march arm -o basic-c-compile.s

REM L:\dev\buildtools\android-ndk-r8e\toolchains\arm-linux-androideabi-4.7\prebuilt\windows-x86_64\bin\arm-linux-androideabi-gcc basic-c-compile.s -o basic-c-compile.exe

REM %LLVM_DIR%\clang -O3 -target armv7a-none-linux-androideabi --sysroot L:\dev\buildtools\android-ndk-r8e\platforms\android-14\arch-arm -gcc-toolchain L:\dev\buildtools\android-ndk-r8e\toolchains\arm-linux-androideabi-4.7\prebuilt\windows-x86_64 -v -c basic-c-compile\basic-c-compile.c -o basic-c-compile.o


"%LLVM_DIR%\clang.exe" -cc1 -triple armv7-none-linux-androideabi -S -disable-free -main-file-name basic-c-compile.c -mrelocation-model pic -pic-level 1 -mdisable-fp-elim -fmath-errno -mconstructor-aliases -fuse-init-array -target-abi aapcs-linux -target-cpu cortex-a8 -mfloat-abi soft -target-feature +soft-float-abi -backend-option -arm-enable-ehabi -backend-option -arm-enable-ehabi-descriptors -backend-option -arm-ignore-has-ras -target-linker-version 2.22 -momit-leaf-frame-pointer -v -coverage-file C:/Users/Justin/AppData/Local/Temp/basic-c-compile-524564.s -resource-dir "L:/dev/buildtools/android-ndk-r8e/toolchains/llvm-3.2/prebuilt/windows-x86_64/bin\\..\\lib\\clang\\3.2" -isysroot "L:\\dev\\buildtools\\android-ndk-r8e\\platforms\\android-14\\arch-arm" -fmodule-cache-path "C:\\Users\\Justin\\AppData\\Local\\Temp\\clang-module-cache" -internal-isystem "L:\\dev\\buildtools\\android-ndk-r8e\\platforms\\android-14\\arch-arm/usr/local/include" -internal-isystem L:/dev/buildtools/android-ndk-r8e/toolchains/llvm-3.2/prebuilt/windows-x86_64/bin/../lib/clang/3.2/include -internal-externc-isystem "L:\\dev\\buildtools\\android-ndk-r8e\\platforms\\android-14\\arch-arm/include" -internal-externc-isystem "L:\\dev\\buildtools\\android-ndk-r8e\\platforms\\android-14\\arch-arm/usr/include" -O3 -fno-dwarf-directory-asm -ferror-limit 19 -fmessage-length 80 -mstackrealign -fno-signed-char -fobjc-runtime=gcc -fdiagnostics-show-option -fcolor-diagnostics -o basic-c-compile-524564.s -x c "basic-c-compile\\basic-c-compile.c"

pause