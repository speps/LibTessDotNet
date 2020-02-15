@echo off
setlocal
set _currentpath=%~dp0
pushd "%_currentpath%"

del /S /Q TempNuGet
md TempNuGet\lib\netstandard2.0
copy ..\LibTessDotNet\bin\Release\netstandard2.0 TempNuGet\lib\netstandard2.0
copy ..\LibTessDotNet\bin\ReleaseDouble\netstandard2.0 TempNuGet\lib\netstandard2.0

pushd TempNuGet
..\nuget.exe spec -force -a lib\netstandard2.0\LibTessDotNet.dll
..\nuget.exe spec -force -a lib\netstandard2.0\LibTessDotNet.Double.dll
python ..\PackageNuGet.py
..\nuget.exe pack LibTessDotNet.nuspec -Symbols -SymbolPackageFormat snupkg
..\nuget.exe pack LibTessDotNet.Double.nuspec -Symbols -SymbolPackageFormat snupkg
popd

popd
