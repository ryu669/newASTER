param(
    [string]$DotNet = 'C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Data\DotNetSdk\dotnet.exe'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$output = Join-Path $repo 'tmp/core-validation'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$utf8 = [System.Text.UTF8Encoding]::new($false)
$editor = [IO.File]::ReadAllText((Join-Path $repo 'game/unity/Assets/Game/Editor/PlayableBuild.cs'), $utf8)
$start = $editor.IndexOf('    private static void ValidateBattleDecisions()')
if ($start -lt 0) { throw 'Battle validation method missing' }
$method = $editor.Substring($start).TrimEnd()
# The last closing brace belongs to PlayableBuild, rather than the method.
$method = $method.Substring(0, $method.LastIndexOf('}'))
$program = @"
using System;
using System.Linq;
using NewAster.Core;
public static class Program {
    private static int assertions;
    private static void Check(bool condition, string message) {
        assertions++; if (!condition) throw new Exception(message);
    }
    public static void Main() {
        ValidateBattleDecisions();
        Console.WriteLine("CORE_BATTLE_VALIDATION_PASS " + assertions + " assertions");
    }
$method
}
"@
[IO.File]::WriteAllText((Join-Path $output 'Program.cs'), $program, $utf8)
$sdkRoot = Split-Path $DotNet -Parent
$sdk = Get-ChildItem (Join-Path $sdkRoot 'sdk') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$pack = Get-ChildItem (Join-Path $sdkRoot 'packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$references = Get-ChildItem (Join-Path $pack.FullName 'ref/net8.0') -Filter '*.dll'
$sources = Get-ChildItem (Join-Path $repo 'game/unity/Assets/Game/Scripts/Core'),(Join-Path $repo 'game/unity/Assets/Game/Scripts/Data') -Filter '*.cs'
$assembly = Join-Path $output 'CoreValidation.dll'
$argsFile = Join-Path $output 'compile.rsp'
$lines = @('-nologo','-nostdlib+','-target:exe','-langversion:9',('-out:"'+$assembly+'"'))
$lines += $references | ForEach-Object { '-r:"'+$_.FullName+'"' }
$lines += $sources | ForEach-Object { '"'+$_.FullName+'"' }
$lines += '"'+(Join-Path $output 'Program.cs')+'"'
[IO.File]::WriteAllLines($argsFile, $lines, $utf8)
& $DotNet (Join-Path $sdk.FullName 'Roslyn/bincore/csc.dll') ('@'+$argsFile)
if ($LASTEXITCODE -ne 0) { throw "Core compile failed: $LASTEXITCODE" }
[IO.File]::WriteAllText((Join-Path $output 'CoreValidation.runtimeconfig.json'), '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.0"}}}', $utf8)
& $DotNet $assembly
if ($LASTEXITCODE -ne 0) { throw "Core validation failed: $LASTEXITCODE" }
