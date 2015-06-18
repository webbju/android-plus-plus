:: 
:: Build a snapshot of Android++ for Visual Studio 2013
:: 

"%VS120COMNTOOLS%..\IDE\devenv.exe" ../src/AndroidPlusPlus.vs2013.sln /rebuild "Debug|Any CPU" /project "AndroidPlusPlus.VsIntegratedPackage" /out "AndroidPlusPlus.VsIntegratedPackage_vs12.0_debug.txt"

@if ERRORLEVEL 1 echo Build failed...

"%VS120COMNTOOLS%..\IDE\devenv.exe" ../src/AndroidPlusPlus.vs2013.sln /rebuild "Release|Any CPU" /project "AndroidPlusPlus.VsIntegratedPackage" /out "AndroidPlusPlus.VsIntegratedPackage_vs12.0_release.txt"

@if ERRORLEVEL 1 echo Build failed...