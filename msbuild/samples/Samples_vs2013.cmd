:: 
:: Launch solution in Visual Studio 2013
:: 

@echo off

setlocal

set ANDROID_PLUS_PLUS=%CD%\..\..\

call "%VS120COMNTOOLS%vsvars32.bat"

"%DevEnvDir%devenv.exe" Samples.sln