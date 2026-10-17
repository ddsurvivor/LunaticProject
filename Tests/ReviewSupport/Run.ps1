$ErrorActionPreference = 'Stop'
$reviewRepo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$reviewOutput = Join-Path $reviewRepo 'obj/ReviewSupport'
New-Item -ItemType Directory -Force $reviewOutput | Out-Null
$reviewSources = @('Assets/ReviewSupport/ReviewSaveStore.cs', 'Assets/ReviewSupport/ReviewBuildRuntime.cs', 'Tests/ReviewSupport/Program.cs')
$reviewIncludes = $reviewSources | ForEach-Object { '<Compile Include="' + [Security.SecurityElement]::Escape((Join-Path $reviewRepo $_)) + '" />' }
$reviewEditorRoot = 'D:/Applications/Unity/Unity 2021.3.23f1/Editor/Data/Managed'
$reviewReferences = @('Library/ScriptAssemblies/Assembly-CSharp.dll', 'Library/ScriptAssemblies/UnityEngine.UI.dll') | ForEach-Object { Join-Path $reviewRepo $_ }
$reviewReferences += @('UnityEngine.CoreModule.dll', 'UnityEngine.InputLegacyModule.dll', 'UnityEngine.TextRenderingModule.dll', 'UnityEngine.UIModule.dll') | ForEach-Object { Join-Path $reviewEditorRoot "UnityEngine/$_" }
$reviewJsonPackage = Get-ChildItem (Join-Path $reviewRepo 'Library/PackageCache') -Directory -Filter 'com.unity.nuget.newtonsoft-json@*' | Select-Object -First 1
if (!$reviewJsonPackage) { throw 'Unity Newtonsoft dependency is missing.' }
$reviewReferences += Join-Path $reviewJsonPackage.FullName 'Runtime/Newtonsoft.Json.dll'
$reviewReferences += Join-Path $reviewRepo 'Assets/Plugins/Sirenix/Assemblies/Sirenix.Serialization.dll'
$reviewReferenceXml = $reviewReferences | ForEach-Object { '<Reference Include="' + [IO.Path]::GetFileNameWithoutExtension($_) + '"><HintPath>' + [Security.SecurityElement]::Escape($_) + '</HintPath></Reference>' }
$reviewProject = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><DefineConstants>DEVELOPMENT_BUILD;UNITY_STANDALONE_WIN</DefineConstants></PropertyGroup><ItemGroup>' + ($reviewIncludes -join "`n") + ($reviewReferenceXml -join "`n") + '</ItemGroup></Project>'
$reviewProjectPath = Join-Path $reviewOutput 'ReviewSupport.csproj'
[IO.File]::WriteAllText($reviewProjectPath, $reviewProject)
& dotnet run --project $reviewProjectPath --verbosity quiet -p:UseSharedCompilation=false -- (Join-Path $reviewRepo 'Assets/StreamingAssets/ReviewSaves') (Join-Path $reviewOutput 'FileTests')
if ($LASTEXITCODE -ne 0) { throw 'Review support tests failed.' }

# 同时编译非开发包，确认运行脚本和存档安装类型均被预处理器排除。
$reviewReleaseIncludes = $reviewIncludes | Select-Object -First 2
$reviewReleaseProject = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup>' + ($reviewReleaseIncludes -join "`n") + '</ItemGroup></Project>'
$reviewReleasePath = Join-Path $reviewOutput 'ReviewSupport.Release.csproj'
[IO.File]::WriteAllText($reviewReleasePath, $reviewReleaseProject)
& dotnet build $reviewReleasePath --verbosity quiet -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'Review support release exclusion check failed.' }

$reviewEditorIncludes = @('Assets/ReviewSupport/ReviewSaveStore.cs', 'Assets/ReviewSupport/ReviewBuildRuntime.cs', 'Assets/ReviewSupport/Editor/ReviewBuildValidator.cs') | ForEach-Object { '<Compile Include="' + [Security.SecurityElement]::Escape((Join-Path $reviewRepo $_)) + '" />' }
$reviewEditorReferences = @((Join-Path $reviewEditorRoot 'UnityEditor.dll')) | ForEach-Object { '<Reference Include="' + [IO.Path]::GetFileNameWithoutExtension($_) + '"><HintPath>' + [Security.SecurityElement]::Escape($_) + '</HintPath></Reference>' }
$reviewEditorProject = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><DefineConstants>UNITY_EDITOR</DefineConstants></PropertyGroup><ItemGroup>' + ($reviewEditorIncludes -join "`n") + ($reviewReferenceXml -join "`n") + ($reviewEditorReferences -join "`n") + '</ItemGroup></Project>'
$reviewEditorPath = Join-Path $reviewOutput 'ReviewSupport.Editor.csproj'
[IO.File]::WriteAllText($reviewEditorPath, $reviewEditorProject)
& dotnet build $reviewEditorPath --verbosity quiet -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'Review support editor validator compile failed.' }
