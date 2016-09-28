@echo off
pushd %~dp0
setlocal

if exist bin goto Build
mkdir bin

:Build

REM Find the most recent 32bit MSBuild.exe on the system. Require v14.0 (installed with VS2015) or later since .NET 4.5
REM is not supported. Also handle x86 operating systems, where %ProgramFiles(x86)% is not defined. Always quote the
REM %MSBuild% value when setting the variable and never quote %MSBuild% references.
set MSBuild="%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe"
if not exist %MSBuild% @set MSBuild="%ProgramFiles%\MSBuild\14.0\Bin\MSBuild.exe"
if not exist %MSBuild% @set MSBuild="%ProgramFiles(x86)%\MSBuild\12.0\Bin\MSBuild.exe"
if not exist %MSBuild% @set MSBuild="%ProgramFiles%\MSBuild\12.0\Bin\MSBuild.exe"

if "%1" == "" goto BuildDefaults

%MSBuild% RESTier.msbuild /m /nr:false /t:%* /p:Platform="Any CPU" /p:Desktop=true /v:M /fl /flp:LogFile=bin\msbuild.log;Verbosity=Normal
if %ERRORLEVEL% neq 0 goto BuildFail
goto BuildSuccess

:BuildDefaults
%MSBuild% RESTier.msbuild /m /nr:false /p:Platform="Any CPU" /p:Desktop=true /v:M /fl /flp:LogFile=bin\msbuild.log;Verbosity=Normal
if %ERRORLEVEL% neq 0 goto BuildFail
goto BuildSuccess

:BuildFail
echo.
echo *** BUILD FAILED ***
goto End

:BuildSuccess
echo.
echo **** BUILD SUCCESSFUL ***
goto End

:End
popd
endlocal
