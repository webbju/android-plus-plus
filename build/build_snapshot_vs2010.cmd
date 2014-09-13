:: 
:: Build a snapshot of Android++ for Visual Studio 2010
:: 

"%VS100COMNTOOLS%..\IDE\devenv.exe" ../src/AndroidPlusPlus.vs2010.sln /rebuild "Debug|Any CPU" /project "AndroidPlusPlus.VsIntegratedPackage.vs2010" /out "AndroidPlusPlus.VsIntegratedPackage_vs10.0_debug.txt"

@if ERRORLEVEL 1 echo Build failed...

"%VS100COMNTOOLS%..\IDE\devenv.exe" ../src/AndroidPlusPlus.vs2010.sln /rebuild "Release|Any CPU" /project "AndroidPlusPlus.VsIntegratedPackage.vs2010" /out "AndroidPlusPlus.VsIntegratedPackage_vs10.0_release.txt"

@if ERRORLEVEL 1 echo Build failed...