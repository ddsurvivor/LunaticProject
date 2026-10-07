$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $repo 'obj/AvgReaderInput/Harness'
New-Item -ItemType Directory -Force $output | Out-Null
$sources = @('Assets/Scripts/剧本/剧本System.Input.cs', 'Tests/AvgReaderInput/Stubs.cs', 'Tests/AvgReaderInput/Program.cs')
$includes = $sources | ForEach-Object { '<Compile Include="' + [System.Security.SecurityElement]::Escape((Join-Path $repo $_)) + '" />' }
$project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup>' + ($includes -join "`n") + '</ItemGroup></Project>'
$projectPath = Join-Path $output 'AvgReaderInput.csproj'
[IO.File]::WriteAllText($projectPath, $project, [Text.UTF8Encoding]::new($false))
& dotnet run --project $projectPath --verbosity quiet -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'AVG reader input tests failed.' }
