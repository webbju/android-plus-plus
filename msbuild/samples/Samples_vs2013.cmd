:: 
:: Launch Samples solution in Visual Studio 2013
:: 

@echo off

setlocal

cd %~dp0\..\

call "%VS120COMNTOOLS%vsvars32.bat"

"%DevEnvDir%devenv.exe" .\samples\Samples.sln

endlocal