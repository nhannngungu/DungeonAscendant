$contentRoot = $PSScriptRoot
$mgcbPath = Join-Path $contentRoot "Content.mgcb"
$lines = Get-Content -LiteralPath $mgcbPath
$audioStart = [Array]::IndexOf($lines, "#begin Audio/Combat/player_swing_light_01.wav")
if ($audioStart -ge 0) {
    $lines = $lines[0..($audioStart - 1)]
}

$entries = foreach ($file in Get-ChildItem -LiteralPath (Join-Path $contentRoot "Audio") -Recurse -Filter *.wav | Sort-Object FullName) {
    $relative = $file.FullName.Substring($contentRoot.Length + 1).Replace("\", "/")
    "#begin $relative"
    "/importer:WavImporter"
    "/processor:SoundEffectProcessor"
    "/processorParam:Quality=Best"
    "/build:$relative"
    ""
}
$entries += @(
    "#begin Audio/audio_manifest.json",
    "/copy:Audio/audio_manifest.json",
    "",
    "#begin Audio/epic_music_manifest.json",
    "/copy:Audio/epic_music_manifest.json",
    ""
)

Set-Content -LiteralPath $mgcbPath -Value ($lines + $entries) -Encoding utf8
