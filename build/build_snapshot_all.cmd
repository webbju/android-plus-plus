:: 
:: Build a snapshot of Android++ for all Visual Studio versions
:: 

@setlocal

@set BUILD_SCRIPTS_ROOT=%CD%

@cd %BUILD_SCRIPTS_ROOT%\..\msbuild\build

call build_snapshot_msbuild.cmd

@cd %BUILD_SCRIPTS_ROOT%\..\toolchain\build

call build_snapshot_toolchain.cmd

@cd %BUILD_SCRIPTS_ROOT%

call build_snapshot_vs2013.cmd

pause
