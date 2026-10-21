$ErrorActionPreference = "Stop"

$dataRoot = $PSScriptRoot
$tableRoot = Join-Path $dataRoot "Tables"
$jsonRoot = Join-Path $dataRoot "Data"
$executable = Join-Path $dataRoot "json-excel\json-excel.exe"
$statePath = Join-Path $jsonRoot "_Excel2Json.state.json"

$files = @(
    "AttackDefine.txt",
    "CharacterDefine.txt",
    "MonsterAbility.txt",
    "MonsterAI.txt",
    "MonsterAnimation.txt",
    "MonsterBehavior.txt",
    "MonsterBehaviorCondition.txt"
)

if (Test-Path -LiteralPath $statePath) {
    $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
    foreach ($name in $files) {
        $jsonPath = Join-Path $jsonRoot $name
        $currentHash = (Get-FileHash -LiteralPath $jsonPath -Algorithm SHA256).Hash
        $expectedHash = $state.files.$name
        if ($currentHash -ne $expectedHash) {
            Write-Host "检测到 Data/Data 在最近一次 Excel 导入后被修改。" -ForegroundColor Red
            Write-Host "为防止覆盖工作台修改，Excel2Json 已停止。" -ForegroundColor Red
            Write-Host "如需明确使用 Excel 覆盖 JSON，请先删除：" -ForegroundColor Yellow
            Write-Host $statePath
            exit 2
        }
    }
}

$process = Start-Process `
    -FilePath $executable `
    -ArgumentList @("json", "Tables\", "Data\") `
    -WorkingDirectory $dataRoot `
    -NoNewWindow `
    -Wait `
    -PassThru
if ($process.ExitCode -ne 0) {
    Write-Host "Excel2Json 转换失败，未更新保护状态。" -ForegroundColor Red
    exit $process.ExitCode
}

$generated = [ordered]@{}
foreach ($name in $files) {
    $jsonPath = Join-Path $jsonRoot $name
    $generated[$name] = (Get-FileHash -LiteralPath $jsonPath -Algorithm SHA256).Hash
}

$state = [ordered]@{ files = $generated }
$state | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $statePath -Encoding UTF8
Write-Host "JSON 修改保护状态已更新。" -ForegroundColor Green
