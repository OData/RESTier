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

.nuget\NuGet pack src\Microsoft.Restier.Core\Microsoft.Restier.Core.csproj %params%
.nuget\NuGet pack src\Microsoft.Restier.Security\Microsoft.Restier.Security.csproj %params%
.nuget\NuGet pack src\Microsoft.Restier.Provider.EntityFramework\Microsoft.Restier.Provider.EntityFramework.csproj %params%
.nuget\NuGet pack src\Microsoft.Restier.Publisher.OData\Microsoft.Restier.Publisher.OData.csproj %params%
.nuget\NuGet pack src\Microsoft.Restier\Microsoft.Restier.nuspec %params%

popd
endlocal
