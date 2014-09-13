:: 
:: Build a snapshot of Android++ samples for MSBuild 
:: 

@setlocal

@set SAMPLES_SLN=../samples/Samples.sln

@set SAMPLES_PROJECTS=hello-gdbserver hello-jni native-activity native-audio sdl-demo

:: Visual Studio 2010
@for %%A in (%SAMPLES_PROJECTS%) do ( 
"%VS100COMNTOOLS%..\IDE\devenv.exe" %SAMPLES_SLN% /rebuild "Debug|Android++" /project "%%A" /out "%%A_vs10.0_debug.txt"
@if ERRORLEVEL 1 echo Build failed...
)
@for %%A in (%SAMPLES_PROJECTS%) do ( 
"%VS100COMNTOOLS%..\IDE\devenv.exe" %SAMPLES_SLN% /rebuild "Release|Android++" /project "%%A" /out "%%A_vs10.0_release.txt"
@if ERRORLEVEL 1 echo Build failed...
)

:: Visual Studio 2012
@for %%A in (%SAMPLES_PROJECTS%) do ( 
"%VS110COMNTOOLS%..\IDE\devenv.exe" %SAMPLES_SLN% /rebuild "Debug|Android++" /project "%%A" /out "%%A_vs11.0_debug.txt"
@if ERRORLEVEL 1 echo Build failed...
)
@for %%A in (%SAMPLES_PROJECTS%) do ( 
"%VS110COMNTOOLS%..\IDE\devenv.exe" %SAMPLES_SLN% /rebuild "Release|Android++" /project "%%A" /out "%%A_vs11.0_release.txt"
@if ERRORLEVEL 1 echo Build failed...
)

:: Visual Studio 2013
@for %%A in (%SAMPLES_PROJECTS%) do ( 
"%VS120COMNTOOLS%..\IDE\devenv.exe" %SAMPLES_SLN% /rebuild "Debug|Android++" /project "%%A" /out "%%A_vs12.0_debug.txt"
@if ERRORLEVEL 1 echo Build failed...
)
@for %%A in (%SAMPLES_PROJECTS%) do ( 
"%VS120COMNTOOLS%..\IDE\devenv.exe" %SAMPLES_SLN% /rebuild "Release|Android++" /project "%%A" /out "%%A_vs12.0_release.txt"
@if ERRORLEVEL 1 echo Build failed...
)
