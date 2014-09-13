:: 
:: Build samples and test MSBuild projects for all Visual Studio versions
:: 

@set BUILD_SCRIPTS_ROOT=%CD%

@cd %BUILD_SCRIPTS_ROOT%\..\bootstrap

call msbuild_install_all.cmd

@cd %BUILD_SCRIPTS_ROOT%\..\msbuild\build

call build_snapshot_samples.cmd

call build_snapshot_tests.cmd

pause
