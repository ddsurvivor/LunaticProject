$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo 'obj/AvgSaveRecovery/Harness'
New-Item -ItemType Directory -Force $output | Out-Null
$sources = @('Assets/Scripts/玩家系统/PLAYERPROFILE.cs', 'Assets/Scripts/剧本/剧本System.SaveState.cs', 'Assets/Scripts/Battle/Tool/JsonTool.cs', 'Tests/AvgSaveRecovery/Stubs.cs', 'Tests/AvgSaveRecovery/Program.cs')
$includes = $sources | ForEach-Object { '<Compile Include="' + [System.Security.SecurityElement]::Escape((Join-Path $repo $_)) + '" />' }
$json = Get-ChildItem (Join-Path $repo 'Library/PackageCache') -Directory -Filter 'com.unity.nuget.newtonsoft-json@*' | Select-Object -First 1
if (!$json) { throw 'Open the Unity project once to resolve its Newtonsoft JSON dependency.' }
$jsonDll = [System.Security.SecurityElement]::Escape((Join-Path $json.FullName 'Runtime/Newtonsoft.Json.dll'))
$project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><UseAppHost>false</UseAppHost><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup>' + ($includes -join "`n") + '<Reference Include="Newtonsoft.Json"><HintPath>' + $jsonDll + '</HintPath></Reference></ItemGroup></Project>'
$projectPath = Join-Path $output 'AvgSaveRecovery.csproj'
[IO.File]::WriteAllText($projectPath, $project, [Text.UTF8Encoding]::new($false))
& dotnet run --project $projectPath --verbosity quiet -p:UseSharedCompilation=false -- $output
if ($LASTEXITCODE -ne 0) { throw 'AVG save recovery tests failed.' }
