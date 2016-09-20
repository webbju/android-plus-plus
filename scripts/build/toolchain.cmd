
@set TOOLCHAIN_PATH=%~dp0/../../toolchain
@set TOOLCHAIN_CONFIG=%2

%DEVENV_PATH% %TOOLCHAIN_PATH%/src/app-java-builder/app-java-builder.sln /build "%TOOLCHAIN_CONFIG%|x86"
%DEVENV_PATH% %TOOLCHAIN_PATH%/src/app-ndk-depends/app-ndk-depends.sln /build "%TOOLCHAIN_CONFIG%|Win32"
%DEVENV_PATH% %TOOLCHAIN_PATH%/src/app-zipalign/app-zipalign.sln /build "%TOOLCHAIN_CONFIG%|Win32"
