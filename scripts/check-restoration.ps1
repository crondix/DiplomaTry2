param(
    [switch]$SkipIntegrationTests
)

$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)

dotnet restore DiplomaTry2.sln --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet build DiplomaTry2.sln --no-restore --nologo -m:1 -nodeReuse:false
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not $SkipIntegrationTests) {
    if ([string]::IsNullOrWhiteSpace($env:DIPLOMA_TEST_CONNECTION)) {
        throw 'Set DIPLOMA_TEST_CONNECTION to a disposable SQL Server test database before running integration tests.'
    }
    dotnet test DiplomaTry2.ImportTests/DiplomaTry2.ImportTests.csproj --no-build --no-restore --nologo -m:1 -nodeReuse:false
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
