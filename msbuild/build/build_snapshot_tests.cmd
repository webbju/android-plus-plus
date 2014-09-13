:: 
:: Build a snapshot of Android++ test projects for MSBuild 
:: 

@setlocal

@set TESTS_SLN=../tests/Tests.sln

@set TESTS_PROJECTS=native-library-a native-library-b native-library-jni native-neon native-pch native-stl

:: Visual Studio 2010
@for %%A in (%TESTS_PROJECTS%) do ( 
"%VS100COMNTOOLS%..\IDE\devenv.exe" %TESTS_SLN% /rebuild "Debug|Android++" /project "%%A" /out "%%A_vs10.0_debug.txt"
@if ERRORLEVEL 1 echo Build failed...
)
@for %%A in (%TESTS_PROJECTS%) do ( 
"%VS100COMNTOOLS%..\IDE\devenv.exe" %TESTS_SLN% /rebuild "Release|Android++" /project "%%A" /out "%%A_vs10.0_release.txt"
@if ERRORLEVEL 1 echo Build failed...
)

:: Visual Studio 2012
@for %%A in (%TESTS_PROJECTS%) do ( 
"%VS110COMNTOOLS%..\IDE\devenv.exe" %TESTS_SLN% /rebuild "Debug|Android++" /project "%%A" /out "%%A_vs11.0_debug.txt"
@if ERRORLEVEL 1 echo Build failed...
)
@for %%A in (%TESTS_PROJECTS%) do ( 
"%VS110COMNTOOLS%..\IDE\devenv.exe" %TESTS_SLN% /rebuild "Release|Android++" /project "%%A" /out "%%A_vs11.0_release.txt"
@if ERRORLEVEL 1 echo Build failed...
)

:: Visual Studio 2013
@for %%A in (%TESTS_PROJECTS%) do ( 
"%VS120COMNTOOLS%..\IDE\devenv.exe" %TESTS_SLN% /rebuild "Debug|Android++" /project "%%A" /out "%%A_vs12.0_debug.txt"
@if ERRORLEVEL 1 echo Build failed...
)
@for %%A in (%TESTS_PROJECTS%) do ( 
"%VS120COMNTOOLS%..\IDE\devenv.exe" %TESTS_SLN% /rebuild "Release|Android++" /project "%%A" /out "%%A_vs12.0_release.txt"
@if ERRORLEVEL 1 echo Build failed...
)
