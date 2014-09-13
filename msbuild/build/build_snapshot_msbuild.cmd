:: 
:: Build a snapshot of Android++ for MSBuild (Visual Studio 2010)
:: 

@setlocal

@set MSBUILD_SLN=../src/AndroidPlusPlus.MsBuild.vs2010.sln

@set MSBUILD_PROJECTS=AndroidPlusPlus.MsBuild.Common.vs2010 AndroidPlusPlus.MsBuild.CppTasks.vs2010 AndroidPlusPlus.MsBuild.DeployTasks.vs2010 AndroidPlusPlus.MsBuild.Exporter.vs2010

@for %%A in (%MSBUILD_PROJECTS%) do ( 
"%VS100COMNTOOLS%..\IDE\devenv.exe" %MSBUILD_SLN% /rebuild "Debug|Any CPU" /project "%%A" /out "%%A_vs10.0_debug.txt"
@if ERRORLEVEL 1 echo Build failed...
)

@for %%A in (%MSBUILD_PROJECTS%) do ( 
"%VS100COMNTOOLS%..\IDE\devenv.exe" %MSBUILD_SLN% /rebuild "Release|Any CPU" /project "%%A" /out "%%A_vs10.0_release.txt"
@if ERRORLEVEL 1 echo Build failed...
)
