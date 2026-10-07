$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo 'obj/BattlePresentation/Harness'
New-Item -ItemType Directory -Force $output | Out-Null
$sources = @(
    'Assets/Scripts/Battle/Controllers/PieceDisplay.cs',
    'Assets/Scripts/Battle/Managers/TimeManager.cs',
    'Assets/Scripts/Battle/Tool/LifeTime.cs',
    'Tests/BattlePresentation/Stubs.cs',
    'Tests/BattlePresentation/Program.cs'
)
$includes = $sources | ForEach-Object { '<Compile Include="' + [System.Security.SecurityElement]::Escape((Join-Path $repo $_)) + '" />' }
$project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup>' + ($includes -join "`n") + '</ItemGroup></Project>'
$projectPath = Join-Path $output 'BattlePresentation.csproj'
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
if ($LASTEXITCODE -ne 0) { throw 'Battle presentation tests failed.' }
