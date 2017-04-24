@ECHO OFF
pushd %~dp0
setlocal

if exist .nuget\nuget.exe goto Prepare
echo Downloading Nuget.exe
call build.cmd DownloadNuGet >NUL

:Prepare
if exist bin\nuget goto Configure
md bin\nuget

:Configure
set config=%1
if not defined config set config=Debug

:Configure
set version=%2
if not defined version set version=1.0.0-beta2

if exist bin\nuget\%config% goto Build
md bin\nuget\%config%



:Build
set params=-Prop Configuration=%config% -OutputDirectory bin\nuget\%config%

.nuget\NuGet pack src\Microsoft.Restier.Core\Microsoft.Restier.Core.csproj %params% -version %version%
.nuget\NuGet pack src\Microsoft.Restier.Providers.EntityFramework\Microsoft.Restier.Providers.EntityFramework.csproj %params% -version %version%
.nuget\NuGet pack src\Microsoft.Restier.Publishers.OData\Microsoft.Restier.Publishers.OData.csproj %params% -version %version%
.nuget\NuGet pack src\Microsoft.Restier\Microsoft.Restier.nuspec %params% -version %version%

popd
endlocal
