:: 
:: Bootstrap and launch Tests solution in Visual Studio 2012
:: 

@echo off

setlocal

cd %~dp0\..\

call bootstrap_vs2012.cmd

call "%VS110COMNTOOLS%vsvars32.bat"

"%DevEnvDir%devenv.exe" .\tests\Tests.sln

endlocal