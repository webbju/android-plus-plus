:: 
:: Build a snapshot of Android++ for Visual Studio 2012
:: 

"%VS110COMNTOOLS%..\IDE\devenv.exe" ../src/AndroidPlusPlus.vs2012.sln /rebuild "Debug|Any CPU" /project "AndroidPlusPlus.VsIntegratedPackage" /out "AndroidPlusPlus.VsIntegratedPackage_vs11.0_debug.txt"

@if ERRORLEVEL 1 echo Build failed...

"%VS110COMNTOOLS%..\IDE\devenv.exe" ../src/AndroidPlusPlus.vs2012.sln /rebuild "Release|Any CPU" /project "AndroidPlusPlus.VsIntegratedPackage" /out "AndroidPlusPlus.VsIntegratedPackage_vs11.0_release.txt"

@if ERRORLEVEL 1 echo Build failed...