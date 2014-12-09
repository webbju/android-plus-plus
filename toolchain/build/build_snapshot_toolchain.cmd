:: 
:: Build a snapshot of Android++ for MSBuild
:: 

"%VS100COMNTOOLS%..\IDE\devenv.exe" ../src/app-jar-dependencies/app-jar-dependencies.sln /build "Release|x86"

"%VS100COMNTOOLS%..\IDE\devenv.exe" ../src/app-javac-dependencies/app-javac-dependencies.sln /build "Release|x86"

"%VS100COMNTOOLS%..\IDE\devenv.exe" ../src/app-zipalign/app-zipalign.sln /build "Release|Win32"
