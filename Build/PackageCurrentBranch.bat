@echo off
setlocal
set _currentpath=%~dp0
pushd "%_currentpath%"

del /S /Q TempPackage
md TempPackage
copy ..\TessBed\bin\Release\net472\* TempPackage
del TempPackage\*.pdb

LibZ\libz.exe inject-dll --assembly TempPackage\TessBed.exe --include TempPackage\Poly2Tri.dll --include TempPackage\nunit.framework.dll --move

copy ..\LibTessDotNet\bin\ReleaseDouble\netstandard2.0\LibTessDotNet.Double.dll TempPackage\LibTessDotNet.Double.dll
copy Instructions.txt TempPackage\Instructions.txt
copy ..\LICENSE.txt TempPackage\MITLicense.txt

if not "%1" == "" (
    set _version=%1
) else if not "%appveyor_build_version%" == "" (
    set _version=%appveyor_build_version%
) else (
    set /P _version=Enter version || set _version=NONE
)
if "%_version%"=="NONE" goto :error
set _version="LibTessDotNet-v%_version%"

move TempPackage "%_version%"

Zip\zip.exe -r "%_version%.zip" "%_version%"

del /S /Q "%_version%"
rd /S /Q "%_version%"

goto :eof

:error
echo Version required
goto :eof

popd
