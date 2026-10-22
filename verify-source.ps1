param([string]$Bin64='D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64')
$ErrorActionPreference='Stop'
$env:DOTNET_CLI_HOME=Join-Path $PSScriptRoot '.cache\dotnet'
$env:NUGET_PACKAGES=Join-Path $PSScriptRoot '.cache\nuget'
[xml]$manifest=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Marigold.xml') -Raw
if($manifest.PluginData.Description.Length -gt 1000){throw 'PluginHub description exceeds 1000 characters.'}
$sourceFiles=@()
foreach($directory in $manifest.PluginData.SourceDirectories.Directory){
if($directory -ne 'src/Marigold.Plugin'){throw 'Unexpected production source directory.'}
$sourceFiles+=Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot $directory) -Filter *.cs -File
}
if($sourceFiles.Count -eq 0){throw 'No production source files.'}
$check=Join-Path $PSScriptRoot 'artifacts\SourceCheck'
New-Item -ItemType Directory -Path $check -Force | Out-Null
[xml]$project=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'src\Marigold.Plugin\Marigold.Plugin.csproj') -Raw
$project.Project.PropertyGroup.DefineConstants=''
$project.Project.PropertyGroup.AssemblyName='Marigold.SourceCheck'
foreach($pair in @(@('EnableDefaultCompileItems','false'),@('GenerateAssemblyInfo','false'))){
$node=$project.CreateElement($pair[0]);$node.InnerText=$pair[1];$null=$project.Project.PropertyGroup.AppendChild($node)
}
$group=$project.CreateElement('ItemGroup')
foreach($file in $sourceFiles){$node=$project.CreateElement('Compile');$node.SetAttribute('Include',$file.FullName);$null=$group.AppendChild($node)}
$null=$project.Project.AppendChild($group)
$projectPath=Join-Path $check 'SourceCheck.csproj'
$project.Save($projectPath)
dotnet build $projectPath -c Release --nologo -v minimal -p:UseSharedCompilation=false "-p:Bin64=$Bin64"
if($LASTEXITCODE -ne 0){throw 'Source-only compilation failed.'}
Write-Output "PASS: $($sourceFiles.Count) manifest-selected source files compiled without LOCAL_BUILD or generated assembly attributes."
Write-Output 'This checks source compilation; live Pulsar loading still requires the in-game checklist.'
