param([string]$Commit='HEAD')
$ErrorActionPreference='Stop'
$revision=(& git -C $PSScriptRoot rev-parse --verify "$Commit^{commit}").Trim()
if($LASTEXITCODE -ne 0 -or $revision -notmatch '^[0-9a-f]{40}$'){throw 'A valid source commit is required.'}
$status=& git -C $PSScriptRoot status --porcelain -- src/Marigold.Plugin Marigold.xml
if($status){throw 'Commit production changes before generating a pinned manifest.'}
$manifestText=& git -C $PSScriptRoot show "${revision}:Marigold.xml"
if($LASTEXITCODE -ne 0){throw 'Pinned commit has no Marigold.xml.'}
[xml]$manifest=$manifestText -join "`n"
foreach($pair in @(@('RepoId','Taeleus/Marigold'),@('Commit',$revision),@('Runtimes','NETFramework'))){
$node=$manifest.CreateElement($pair[0]);$node.InnerText=$pair[1];$null=$manifest.PluginData.AppendChild($node)
}
$folder=Join-Path $PSScriptRoot 'artifacts\PluginHub'
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$path=Join-Path $folder 'Marigold.xml'
$manifest.Save($path)
Write-Output "Prepared $path pinned to $revision. No PluginHub submission was made."
