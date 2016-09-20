
:setup
@setlocal
@set BUILD_PATH=%~dp0\..\build
@set BUILD_CONFIG=Release
@mkdir %BUILD_PATH%
@cd %BUILD_PATH%

:find_2013
@set VS_VERSION=2013
@echo Building for VS%VS_VERSION%...
@if "%VS120COMNTOOLS%" == "" (
  @echo Visual Studio 2013 not installed/found.
  goto find_2015
)

:build_2013
@set DEVENV_PATH="%VS120COMNTOOLS%..\IDE\devenv.exe"
@call %~dp0/build/extension.cmd %VS_VERSION% %BUILD_CONFIG% %*
@call %~dp0/build/msbuild.cmd %VS_VERSION% %BUILD_CONFIG% %*
@call %~dp0/build/toolchain.cmd %VS_VERSION% %BUILD_CONFIG% %*

:find_2015
@set VS_VERSION=2015
@echo Building for VS%VS_VERSION%...
@if "%VS140COMNTOOLS%" == "" (
  @echo Visual Studio 2015 not installed/found.
  goto exit
)

:build_2015
@set DEVENV_PATH="%VS140COMNTOOLS%..\IDE\devenv.exe"
@call %~dp0/build/extension.cmd %VS_VERSION% %BUILD_CONFIG% %*
@call %~dp0/build/msbuild.cmd %VS_VERSION% %BUILD_CONFIG% %*
@call %~dp0/build/toolchain.cmd %VS_VERSION% %BUILD_CONFIG% %*

:exit
@endlocal