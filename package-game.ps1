$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    $version = (Get-Content -LiteralPath 'package.json' -Raw | ConvertFrom-Json).version
    $output = Join-Path $PSScriptRoot 'dist'
    New-Item -ItemType Directory -Path $output -Force | Out-Null
    $archive = Join-Path $output "newASTER-prototype-$version.zip"
    # Stage only game sources and shareable documents; keep local records out.
    $stage = Join-Path $output ('package-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $stage | Out-Null
    try {
        Copy-Item -Path 'index.html','style.css','src','tests','README.md','package.json','server.mjs','start-game.cmd','package-game.ps1' -Destination $stage -Recurse
        $stageDocs = New-Item -ItemType Directory -Path (Join-Path $stage 'docs')
        Get-ChildItem -LiteralPath 'docs' -File | Where-Object { $_.Name -notin @('angelica-preservation.md', 'reference-inventory.json') } | Copy-Item -Destination $stageDocs.FullName
        $stageTools = New-Item -ItemType Directory -Path (Join-Path $stage 'tools')
        Copy-Item -LiteralPath 'tools/battle-parity.mjs' -Destination $stageTools.FullName
        Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $archive -Force
    } finally {
        $resolvedStage = [IO.Path]::GetFullPath($stage)
        $resolvedOutput = [IO.Path]::GetFullPath($output).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
        if (-not $resolvedStage.StartsWith($resolvedOutput, [StringComparison]::OrdinalIgnoreCase)) { throw 'Package staging path is outside dist.' }
        Remove-Item -LiteralPath $resolvedStage -Recurse -Force
    }
    Write-Output $archive
} finally {
    Pop-Location
}
