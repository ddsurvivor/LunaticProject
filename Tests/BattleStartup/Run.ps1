$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo 'obj/BattleStartup/Harness'
New-Item -ItemType Directory -Force $output | Out-Null
$sources = @(
    'Assets/Scripts/Battle/Managers/BattleManager.Startup.cs',
    'Tests/BattleStartup/Stubs.cs',
    'Tests/BattleStartup/Program.cs'
)
$includes = $sources | ForEach-Object { '<Compile Include="' + [System.Security.SecurityElement]::Escape((Join-Path $repo $_)) + '" />' }
$project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup>' + ($includes -join "`n") + '</ItemGroup></Project>'
$projectPath = Join-Path $output 'BattleStartup.csproj'
[IO.File]::WriteAllText($projectPath, $project, [Text.UTF8Encoding]::new($false))
$previousTemp = $env:TEMP
$previousTmp = $env:TMP
try {
    $env:TEMP = $output
    $env:TMP = $output
    & dotnet run --project $projectPath --verbosity quiet -p:UseSharedCompilation=false
} finally {
    $env:TEMP = $previousTemp
    $env:TMP = $previousTmp
}
if ($LASTEXITCODE -ne 0) { throw 'Battle startup tests failed.' }
