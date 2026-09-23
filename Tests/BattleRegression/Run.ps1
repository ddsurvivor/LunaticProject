$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo 'obj/BattleRegression/Harness'
New-Item -ItemType Directory -Force $output | Out-Null
$sources = @(
    'Assets/Scripts/Battle/Controllers/UnitAttrCenter.cs',
    'Assets/Scripts/Battle/Managers/DamageManager.cs',
    'Assets/Scripts/Battle/Managers/BuffManager.cs',
    'Assets/Scripts/Battle/Managers/SkillManager.cs',
    'Assets/Scripts/Battle/Managers/DiceCheckManager.cs',
    'Assets/Scripts/Battle/Controllers/PassiveSkill/CharacterSkillManager.cs',
    'Assets/Scripts/Battle/Tool/DamageCalculator.cs',
    'Assets/Scripts/Battle/Tool/GameEnum.cs',
    'Assets/Scripts/Battle/Data&Config/AttrCenter.cs',
    'Assets/Scripts/Battle/Data&Config/AttackPack.cs',
    'Assets/Scripts/Battle/Data&Config/BuffState.cs',
    'Assets/Scripts/Battle/Data&Config/EnemyLevelGrowth.cs',
    'Assets/Scripts/Battle/Controllers/PassiveSkill/BasePassiveSkill.cs',
    'Assets/Scripts/Battle/Controllers/PassiveSkill/PassiveSkills.cs',
    'Assets/Scripts/玩家系统/交易/ItemPack.cs',
    'Tests/BattleRegression/Stubs.cs',
    'Tests/BattleRegression/Program.cs',
    'Tests/BattleRegression/Batch2.cs'
)
$includes = $sources | ForEach-Object { '<Compile Include="' + [System.Security.SecurityElement]::Escape((Join-Path $repo $_)) + '" />' }
$project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><NoWarn>0162;0169;0414;0649</NoWarn></PropertyGroup><ItemGroup>' + ($includes -join "`n") + '</ItemGroup></Project>'
$projectPath = Join-Path $output 'BattleRegression.csproj'
[IO.File]::WriteAllText($projectPath, $project, [Text.UTF8Encoding]::new($false))
# No packages or external services are needed.
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
if ($LASTEXITCODE -ne 0) { throw 'Battle regression tests failed.' }
