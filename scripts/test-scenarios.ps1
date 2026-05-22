param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $repoRoot "project\\OsEngine.Tests\\OsEngine.Tests.csproj"

& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe" `
  $testProject `
  /t:Build `
  /p:Configuration=$Configuration `
  /p:Platform=AnyCPU `
  /m

if ($LASTEXITCODE -ne 0) {
  throw "MSBuild failed with exit code $LASTEXITCODE"
}

& "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" `
  (Join-Path $repoRoot "project\\OsEngine.Tests\\bin\\$Configuration\\net48\\OsEngine.Tests.dll") `
  /TestCaseFilter:"TestCategory=Scenario"

if ($LASTEXITCODE -ne 0) {
  throw "vstest failed with exit code $LASTEXITCODE"
}
