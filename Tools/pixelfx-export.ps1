# pixelfx 이펙트를 스프라이트 시트(PNG + JSON)로 렌더해서 Unity 프로젝트에 넣는다.
# Unity로 돌아가면 PixelFxSheetPostprocessor가 자동으로 자르고 PixelFxClip, 재생 프리팹을 만든다.
#
#   powershell -File Tools/pixelfx-export.ps1 spawn/poof
#   powershell -File Tools/pixelfx-export.ps1 fire/torch -Name torch-blue -Opts '{"palette":["#102","#215","#43a","#8cf","#fff"]}'
#   powershell -File Tools/pixelfx-export.ps1 explosions/small-explosion -Fps 15
#
# 결과: Assets/_Project/05. Art/PixelFx/<카테고리>/<이름>.png, .json, PxFx_<이름>.asset, PxFx_<이름>.prefab
param(
    [Parameter(Mandatory = $true, Position = 0)][string]$Id,
    [string]$Name,
    [string]$Opts,
    [int]$Fps,
    [int]$Seed
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$pixelfx = Join-Path $PSScriptRoot 'pixelfx'

$node = (Get-Command node -ErrorAction SilentlyContinue).Source
if (-not $node) { $node = Join-Path $env:ProgramFiles 'nodejs\node.exe' }
if (-not (Test-Path $node)) { throw 'Node.js 18 이상이 필요하다: winget install OpenJS.NodeJS.LTS' }

$category, $effect = $Id.Split('/')
if (-not $effect) { throw "id는 카테고리/이름 꼴이어야 한다: $Id" }
if (-not $Name) { $Name = $effect }

$work = Join-Path $pixelfx 'records\_export'
$renderArgs = @('tools/render.mjs', $Id, '--sheet', '--no-actor', '--out', $work)
if ($Opts) { $renderArgs += @('--opts', $Opts) }
if ($Fps) { $renderArgs += @('--fps', "$Fps") }
if ($Seed) { $renderArgs += @('--seed', "$Seed") }

Push-Location $pixelfx
try { & $node @renderArgs; if ($LASTEXITCODE -ne 0) { throw "렌더 실패: $Id" } }
finally { Pop-Location }

$from = Join-Path $work $Id
$to = Join-Path $root "Assets\_Project\05. Art\PixelFx\$category"
New-Item -ItemType Directory -Force $to | Out-Null

Copy-Item (Join-Path $from "$effect.png") (Join-Path $to "$Name.png") -Force
# 이름을 바꿨으면 JSON이 가리키는 그림 이름도 같이 바꾼다. BOM 없는 UTF-8로 쓴다.
$json = [IO.File]::ReadAllText((Join-Path $from "$effect.json"))
$json = $json -replace '"image":\s*"[^"]*"', "`"image`": `"$Name.png`""
[IO.File]::WriteAllText((Join-Path $to "$Name.json"), $json, (New-Object Text.UTF8Encoding $false))

Write-Host "-> $to\$Name.png, $Name.json  (Unity로 돌아가면 자동으로 가져온다)"
