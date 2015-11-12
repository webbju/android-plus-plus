:: 
:: Build a snapshot of Android++ for MSBuild
:: 

"%VS120COMNTOOLS%..\IDE\devenv.exe" ../src/app-java-builder/app-java-builder.sln /build "Release|x86"

"%VS120COMNTOOLS%..\IDE\devenv.exe" ../src/app-ndk-depends/app-ndk-depends.sln /build "Release|Win32"

"%VS120COMNTOOLS%..\IDE\devenv.exe" ../src/app-zipalign/app-zipalign.sln /build "Release|Win32"
