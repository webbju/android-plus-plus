:: 
:: Bootstrap and launch Samples solution in Visual Studio 2012
:: 

@echo off

setlocal

cd %~dp0\..\

call bootstrap_vs2012.cmd

call "%VS110COMNTOOLS%vsvars32.bat"

"%DevEnvDir%devenv.exe" .\samples\Samples.sln

endlocal