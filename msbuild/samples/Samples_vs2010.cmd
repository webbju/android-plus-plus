:: 
:: Launch solution in Visual Studio 2010
:: 

@echo off

setlocal

set ANDROID_PLUS_PLUS=%CD%\..\..\

call "%VS100COMNTOOLS%vsvars32.bat"

"%DevEnvDir%devenv.exe" Samples.sln