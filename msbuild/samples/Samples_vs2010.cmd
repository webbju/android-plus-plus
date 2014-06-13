:: 
:: Bootstrap and launch Samples solution in Visual Studio 2010
:: 

@echo off

setlocal

cd %~dp0\..\

call bootstrap_vs2010.cmd

call "%VS100COMNTOOLS%vsvars32.bat"

"%DevEnvDir%devenv.exe" .\samples\Samples.sln

endlocal