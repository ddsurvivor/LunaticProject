$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo 'obj/EnemyTargetLines/Harness'
New-Item -ItemType Directory -Force $output | Out-Null
$sources = @(
    'Assets/Scripts/Battle/Controllers/AIController.Targeting.cs',
    'Assets/Scripts/Battle/Controllers/EnemyController.TargetLine.cs',
    'Assets/Scripts/Battle/Managers/ClickManager.TargetLines.cs',
    'Tests/EnemyTargetLines/Stubs.cs',
    'Tests/EnemyTargetLines/Program.cs'
)
$includes = $sources | ForEach-Object { '<Compile Include="' + [System.Security.SecurityElement]::Escape((Join-Path $repo $_)) + '" />' }
$project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup>' + ($includes -join "`n") + '</ItemGroup></Project>'
$projectPath = Join-Path $output 'EnemyTargetLines.csproj'
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
if ($LASTEXITCODE -ne 0) { throw 'Enemy target line tests failed.' }
