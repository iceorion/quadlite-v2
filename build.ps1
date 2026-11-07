<#
  build.ps1 —— R1 真实单文件构建入口（零 NuGet / C# 5 / net48 / WPF）。

  把 Core / Fs / App / Host 四层源码一次性交给系统 C# 编译器编成单个
  QuadLite.exe，不产生旁路程序集，也不需要 ILMerge 之类第三方合并工具。
  BUILD.txt 按 §5.5 / B7 的七字段采集真实 exe 的大小与 SHA256，
  工作树脏照实写并附 Warning；编译未执行时只写「未验证」，不用「没报错」冒充通过。

  CI（windows-latest）通过 real-exe-build 步骤实跑本脚本；
  本机受执行策略与编译器调用限制未实跑，标「未验证」。
  规格 design-v2.md v1.2 未改动。
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$FrameworkPathOverride = '',
    [string]$OutDir = '',
    [string]$CscPath = '',
    [switch]$SingleFile
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $MyInvocation.MyCommand.Path
if ($OutDir -eq '') { $OutDir = Join-Path $repo 'dist\single' }
if (-not (Test-Path -LiteralPath $OutDir)) {
    New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
}

function Get-GitText {
    param([string[]]$GitArgs)
    $text = & git -C $repo @GitArgs 2>$null
    if ($LASTEXITCODE -ne 0) { return '' }
    return ($text -join "`n")
}

$head = Get-GitText @('rev-parse', 'HEAD')
if ($head -eq '') { $head = 'unknown' }
$dirty = Get-GitText @('status', '--porcelain')
if ($dirty -eq '') { $tree = 'clean' } else { $tree = 'dirty' }
$exePath = Join-Path $OutDir 'QuadLite.exe'
$manifestPath = [System.IO.Path]::GetFullPath((Join-Path $repo 'src\QuadLite.Host\app.manifest'))

function Write-BuildManifest {
    param(
        [string]$ExeSize,
        [string]$ExeHash,
        [string]$Selfcheck,
        [string]$Resources,
        [string]$Note
    )
    $lines = @(
        "compile-time = $([DateTime]::UtcNow.ToString('o'))",
        "commit       = $head",
        "worktree     = $tree",
        "exe-size     = $ExeSize",
        "sha256       = $ExeHash",
        "embedded-resources = $Resources",
        "package-selfcheck = $Selfcheck",
        "configuration = $Configuration",
        "framework-path-override = $(if ($FrameworkPathOverride -eq '') { 'default' } else { $FrameworkPathOverride })",
        "single-file packaging = applied (single assembly; no satellite dll, no NuGet, no ILMerge)",
        $Note
    )
    if ($tree -eq 'dirty') { $lines += 'Warning: worktree is dirty; BUILD.txt records it verbatim' }
    Set-Content -LiteralPath (Join-Path $OutDir 'BUILD.txt') -Value $lines -Encoding UTF8
}

$fwDir = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
if ($CscPath -eq '') { $CscPath = Join-Path $fwDir 'csc.exe' }
if (-not (Test-Path -LiteralPath $CscPath)) {
    Write-Output 'BUILD: C# compiler unavailable; no build executed'
    Write-BuildManifest -ExeSize 'n/a' -ExeHash 'n/a' -Selfcheck '未验证' `
        -Resources 'none (build not executed)' -Note 'Warning: build not executed; status=未验证'
    exit 2
}

# WPF 引用程序集：优先参数化目录 / .NET Framework 4.8 参考程序集；
# 回落系统运行时目录（WPF 子目录 + 父目录的 System.Xaml.dll）。
$refRoot = ''
$candidates = @()
if ($FrameworkPathOverride -ne '') { $candidates += $FrameworkPathOverride }
$pf86 = ${env:ProgramFiles(x86)}
if ($pf86 -eq '' -or $pf86 -eq $null) { $pf86 = 'C:\Program Files (x86)' }
$candidates += (Join-Path $pf86 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8')
foreach ($candidate in $candidates) {
    if ((Test-Path -LiteralPath (Join-Path $candidate 'PresentationFramework.dll')) -and
        (Test-Path -LiteralPath (Join-Path $candidate 'System.Xaml.dll'))) {
        $refRoot = $candidate
        break
    }
}

$refArgs = @()
if ($refRoot -ne '') {
    foreach ($name in @('PresentationFramework.dll', 'PresentationCore.dll',
                        'WindowsBase.dll', 'System.Xaml.dll')) {
        $refArgs += ('/r:' + (Join-Path $refRoot $name))
    }
} else {
    $wpfDir = Join-Path $fwDir 'WPF'
    foreach ($name in @('PresentationFramework.dll', 'PresentationCore.dll', 'WindowsBase.dll')) {
        $refArgs += ('/r:' + (Join-Path $wpfDir $name))
    }
    $refArgs += ('/r:' + (Join-Path $fwDir 'System.Xaml.dll'))
}
$refArgs += '/r:System.dll'
$refArgs += '/r:System.Core.dll'

$sources = @()
foreach ($layer in @('QuadLite.Core', 'QuadLite.Fs', 'QuadLite.App', 'QuadLite.Host')) {
    $pattern = Join-Path $repo ('src\' + $layer + '\*.cs')
    $sources += @(Get-ChildItem -Path $pattern -ErrorAction Stop | ForEach-Object { $_.FullName })
}
# 当前四层合计 98 个 .cs（Core 78 + Fs 5 + App 12 + Host 3）。
# 这里只防「源码集意外为空 / 漏收某层」，不把具体文件数写成硬契约。
if ($sources.Count -lt 90) {
    Write-Output "BUILD: unexpected source count $($sources.Count)"
    Write-BuildManifest -ExeSize 'n/a' -ExeHash 'n/a' -Selfcheck '未验证' `
        -Resources 'none (source set incomplete)' -Note 'Warning: build aborted; status=未验证'
    exit 2
}

$cscArgs = @('/nologo', '/target:winexe', '/langversion:5', '/codepage:65001',
             '/optimize+', '/debug-', '/platform:anycpu')
if ($Configuration -ne 'Release') {
    $cscArgs = @('/nologo', '/target:winexe', '/langversion:5', '/codepage:65001',
                 '/debug+', '/platform:anycpu')
}
$cscArgs += ('/out:' + $exePath)
$cscArgs += ('/win32manifest:' + $manifestPath)
$cscArgs += $refArgs
$cscArgs += $sources

& $CscPath @cscArgs
if ($LASTEXITCODE -ne 0) {
    Write-Output "BUILD: compile failed with exit $LASTEXITCODE"
    Write-BuildManifest -ExeSize 'n/a' -ExeHash 'n/a' -Selfcheck '未验证' `
        -Resources 'none (compile failed)' -Note 'Warning: compile failed; status=未验证'
    exit $LASTEXITCODE
}
if (-not (Test-Path -LiteralPath $exePath)) {
    Write-Output 'BUILD: compiler returned 0 but exe is missing'
    Write-BuildManifest -ExeSize 'n/a' -ExeHash 'n/a' -Selfcheck '未验证' `
        -Resources 'none (exe missing)' -Note 'Warning: exe missing; status=未验证'
    exit 2
}

$exeItem = Get-Item -LiteralPath $exePath
$exeHash = (Get-FileHash -LiteralPath $exePath -Algorithm SHA256).Hash
$satellites = @(Get-ChildItem -LiteralPath $OutDir -Filter 'QuadLite.*.dll' -ErrorAction SilentlyContinue)
if ($satellites.Count -gt 0) {
    $selfcheck = 'FAIL (satellite assemblies present)'
} else {
    $selfcheck = 'PASS (single assembly, BUILD.txt only companion)'
}
if ($SingleFile) { $note = 'Note: -SingleFile explicitly requested; single assembly is already the default.' }
else { $note = 'Note: real WPF host built; shell COM / native menu / thumbnails / DPI evidence still 未验证.' }

Write-BuildManifest -ExeSize $exeItem.Length -ExeHash $exeHash -Selfcheck $selfcheck `
    -Resources 'none (all four layers compiled into QuadLite.exe)' -Note $note
Write-Output "BUILD: QuadLite.exe written ($($exeItem.Length) bytes); BUILD.txt collected"
exit 0
