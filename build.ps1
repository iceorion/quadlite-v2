<#
  build.ps1 —— 构建与单文件出包入口（B0 骨架）

  参数化 FrameworkPathOverride，不绑定单机工具链路径；零 NuGet 依赖。
  B0 阶段 src 下尚无产品源码，单文件合并与包自检留待 B7。
  本机执行策略禁止运行 .ps1，故本脚本在 B0 未实跑，结论记「未验证」；
  CI 首次实跑与 B1 起引入源码后的证据见 docs/adr/ADR-v2-001.md。
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$FrameworkPathOverride = '',
    [string]$OutDir = '',
    [switch]$SingleFile
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $MyInvocation.MyCommand.Path
if ($OutDir -eq '') { $OutDir = Join-Path $repo 'dist\single' }

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
$exeSize = 'n/a'
$exeHash = 'n/a'
if (Test-Path -LiteralPath $exePath) {
    $item = Get-Item -LiteralPath $exePath
    $exeSize = $item.Length
    $exeHash = (Get-FileHash -LiteralPath $exePath -Algorithm SHA256).Hash
}

$singleFileLine = 'single-file packaging: not applied'
if ($SingleFile) {
    $singleFileLine = 'single-file packaging: NOT IMPLEMENTED (B7); -SingleFile accepted but not executed'
}

$msbuildCmd = Get-Command 'MSBuild.exe' -ErrorAction SilentlyContinue
if ($null -eq $msbuildCmd) {
    Write-Output 'BUILD: MSBuild unavailable on this machine'
    Write-Output 'status: 未验证（未执行构建，不用「没报错」冒充通过）'
    if (-not (Test-Path -LiteralPath $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }
    $lines = @(
        "build_time_utc = $([DateTime]::UtcNow.ToString('o'))",
        "commit         = $head",
        "worktree       = $tree",
        "configuration  = $Configuration",
        "framework_path_override = $(if ($FrameworkPathOverride -eq '') { 'default' } else { $FrameworkPathOverride })",
        "exe_size       = $exeSize",
        "exe_sha256     = $exeHash",
        "embedded_resources = none (B0 skeleton)",
        "package_selfcheck = 未验证",
        $singleFileLine,
        'Warning: build not executed; MSBuild unavailable'
    )
    Set-Content -LiteralPath (Join-Path $OutDir 'BUILD.txt') -Value $lines -Encoding UTF8
    exit 2
}

$project = Join-Path $repo 'src\QuadLite.App\QuadLite.App.csproj'
$msbuildArgs = @($project, "/p:Configuration=$Configuration", '/v:minimal')
if ($FrameworkPathOverride -ne '') {
    $msbuildArgs += "/p:FrameworkPathOverride=$FrameworkPathOverride"
}
& $msbuildCmd.Source @msbuildArgs
if ($LASTEXITCODE -ne 0) {
    Write-Output "BUILD: failed with exit $LASTEXITCODE"
    exit $LASTEXITCODE
}

if (-not (Test-Path -LiteralPath $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }
$lines = @(
    "build_time_utc = $([DateTime]::UtcNow.ToString('o'))",
    "commit         = $head",
    "worktree       = $tree",
    "configuration  = $Configuration",
    "framework_path_override = $(if ($FrameworkPathOverride -eq '') { 'default' } else { $FrameworkPathOverride })",
    "exe_size       = $exeSize",
    "exe_sha256     = $exeHash",
    "embedded_resources = none (B0 skeleton)",
    "package_selfcheck = 未验证",
    $singleFileLine,
    'Warning: single-file packaging not implemented in B0'
)
Set-Content -LiteralPath (Join-Path $OutDir 'BUILD.txt') -Value $lines -Encoding UTF8
Write-Output 'BUILD: msbuild finished; BUILD.txt written'
exit 0
