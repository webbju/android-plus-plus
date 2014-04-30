:: 
:: Launch solution in Visual Studio 2012
:: 

@echo off

setlocal

set ANDROID_PLUS_PLUS=%CD%\..\..\

call "%VS110COMNTOOLS%vsvars32.bat"

"%DevEnvDir%devenv.exe" Tests.sln