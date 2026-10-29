param(
    [string]$Bin64 = 'D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64',

    [ValidateSet('Legacy', 'Interim')]
    [string]$Runtime = 'Legacy'
)

$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot '.cache\dotnet'
$env:NUGET_PACKAGES = Join-Path $PSScriptRoot '.cache\nuget'
$framework = if ($Runtime -eq 'Legacy') {
    'net48'
} else {
    'net10.0'
}
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

& dotnet build (Join-Path $PSScriptRoot 'src\Marigold.Plugin\Marigold.Plugin.csproj') -c Release -f $framework --nologo -v minimal -p:UseSharedCompilation=false "-p:Bin64=$Bin64"
if ($LASTEXITCODE -ne 0) {
    throw 'Plugin build failed.'
}

& dotnet run --project (Join-Path $PSScriptRoot 'tests\Marigold.ObserverChecks') -c Release -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) {
    throw 'Observer checks failed.'
}

# Keep Pulsar's display name stable across releases.
[xml]$project = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'src\Marigold.Plugin\Marigold.Plugin.csproj') -Raw
$pluginVersion = [string]$project.Project.PropertyGroup.Version
if ($pluginVersion -notmatch '^\d+\.\d+\.\d+$') {
    throw 'Expected a major.minor.patch plugin version.'
}

$manifestPath = Join-Path $PSScriptRoot 'Marigold.xml'
$manifestText = [System.IO.File]::ReadAllText($manifestPath)
if ([regex]::Matches($manifestText, '<FriendlyName>[^<]*</FriendlyName>').Count -ne 1) {
    throw 'Expected one FriendlyName in Marigold.xml.'
}

$manifestText = [regex]::Replace($manifestText, '<FriendlyName>[^<]*</FriendlyName>', '<FriendlyName>Marigold</FriendlyName>')
[System.IO.File]::WriteAllText($manifestPath, $manifestText, [System.Text.UTF8Encoding]::new($false))

$packagePath = Join-Path $PSScriptRoot $(if ($Runtime -eq 'Legacy') {
    'artifacts\Marigold'
} else {
    'artifacts\Marigold-Interim'
})

New-Item -ItemType Directory -Path $packagePath -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "src\Marigold.Plugin\bin\Release\$framework\Marigold.dll") -Destination (Join-Path $packagePath 'plugin.dll')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "src\Marigold.Plugin\bin\Release\$framework\Marigold.pdb") -Destination (Join-Path $packagePath 'plugin.pdb')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Marigold.xml') -Destination (Join-Path $packagePath 'plugin.xml')
$runtimeName = if ($Runtime -eq 'Legacy') {
    'CLR'
} else {
    'CoreCLR'
}

$packageManifest = Join-Path $packagePath 'plugin.xml'
$packageXml = [IO.File]::ReadAllText($packageManifest)
if ($packageXml -match '<Runtimes>[^<]*</Runtimes>') {
    $packageXml = [regex]::Replace($packageXml, '<Runtimes>[^<]*</Runtimes>', "<Runtimes>$runtimeName</Runtimes>")
} else {
    $packageXml = $packageXml.Replace('</PluginData>', "  <Runtimes>$runtimeName</Runtimes>`n</PluginData>")
}
[IO.File]::WriteAllText($packageManifest, $packageXml, [Text.UTF8Encoding]::new($false))
Write-Output "Local package ready: $packagePath (not deployed)."
