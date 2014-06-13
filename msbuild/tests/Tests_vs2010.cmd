:: 
:: Bootstrap and launch Tests solution in Visual Studio 2010
:: 

@echo off

setlocal

cd %~dp0\..\

call bootstrap_vs2010.cmd

call "%VS100COMNTOOLS%vsvars32.bat"

"%DevEnvDir%devenv.exe" .\tests\Tests.sln

endlocal