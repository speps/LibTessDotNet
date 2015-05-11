@echo off
setlocal
set _currentpath=%~dp0
pushd "%_currentpath%"
set _msbuildpath=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe
"%_msbuildpath%" ..\LibTessDotNet.sln /t:Clean /p:Configuration=Release
"%_msbuildpath%" ..\LibTessDotNet.sln /t:Build /p:Configuration=Release
popd
