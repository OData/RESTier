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

if exist bin\nuget\%config% goto Build
md bin\nuget\%config%

:Build
set params=-Prop Configuration=%config% -OutputDirectory bin\nuget\%config%

.nuget\NuGet pack src\Microsoft.Data.Domain\Microsoft.Data.Domain.csproj %params%
.nuget\NuGet pack src\Microsoft.Data.Domain.Conventions\Microsoft.Data.Domain.Conventions.csproj %params%
.nuget\NuGet pack src\Microsoft.Data.Domain.Security\Microsoft.Data.Domain.Security.csproj %params%
.nuget\NuGet pack src\Microsoft.Data.Domain.EntityFramework\Microsoft.Data.Domain.EntityFramework.csproj %params%
.nuget\NuGet pack src\System.Web.OData.Domain\System.Web.OData.Domain.csproj %params%

popd
endlocal
