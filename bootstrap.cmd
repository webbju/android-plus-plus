::
:: Bootstrap Android++ support for Visual Studio 2013
::

cd %~dp0

set ANDROID_PLUS_PLUS=%CD%

setx ANDROID_PLUS_PLUS "%CD%"

@setlocal

@call .\bootstrap\msbuild_install_vs2015.cmd

@call .\bootstrap\extension_uninstall_vs2015.cmd

@call .\bootstrap\extension_install_vs2015.cmd

@endlocal
