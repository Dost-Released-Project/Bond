param (
    [string]$userPrompt,
    [string]$targetFilePath, # 에디터에서 넘겨준 수정 대상 파일 (옵션)
    [string]$lang = "C# (Unity 6.0)",
    [string]$Platform = "Unity Engine (6000.3.10f1)"
)

# 인코딩 설정
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

# 1. 환경 설정
$config = @{
    Lang           = $lang
    Extension      = "cs"
    TempFile       = "96. WT/TempCode" 
    ReviewFile     = "96. WT/TempReview.md" 
    GuidelinePath  = "C:\Users\kot77\Desktop\Unity\Bond\Assets\Ignore\GEMINI"
    ReviewPath     = "C:\Users\kot77\Desktop\Unity\Bond\Assets\Ignore\GEMINI_REVIEW"
    WaitTime       = 30
}
# 타겟 파일 인자가 없으면 기본 TempCode를 대상으로 함
if (-not $targetFilePath) { $targetFilePath = "$($config.TempFile).$($config.Extension)" }

# ---------------------------------------------------------
# 함수 정의 구역
# ---------------------------------------------------------

function Get-GuidelineContent([string]$path, [string]$type) {
    if (-not (Test-Path -Path $path -PathType Container)) {
        Write-Host "⚠️ [경고] $type 폴더가 존재하지 않습니다: $path" -ForegroundColor Yellow
        return ""
    }
    $mdFiles = Get-ChildItem -Path "$path\*.md" -ErrorAction SilentlyContinue
    if ($null -eq $mdFiles) {
        Write-Host "⚠️ [경고] $type 가이드라인 파일(.md)이 없습니다." -ForegroundColor Yellow
        return ""
    }
    return Get-Content -Path "$path\*.md" -ErrorAction SilentlyContinue | Out-String
}

function Run-Writer {
    param ([string]$prompt, [string]$guideline, [string]$targetPath, [string]$language)
    Write-Host "[1/2] CLI 1: 작성자 작업 중 ($language)..." -ForegroundColor Cyan
    
    # [핵심] 기존 코드가 있다면 읽어오기
    $originalCode = ""
    if (Test-Path $targetPath) {
        $originalCode = Get-Content $targetPath -Raw
        Write-Host "🔍 기존 코드를 발견했습니다. 리팩토링 모드로 전환합니다." -ForegroundColor Gray
    }

    $fullPrompt = @"
[Context]
Platform: $Platform
Required: Unity 6 C# Script
Language: $language
Core Methodology: DLV (Data-Logic-Visual) Chain

[Guidelines]
$guideline

[Original Code to Refactor]
$originalCode

[User Command]
$prompt

[Instruction]
- [Original Code to Refactor]가 존재할 경우, 이를 기반으로 유효한 로직은 유지하되 사용자의 명령과 가이드라인(DLV)에 맞게 리팩토링하라.
- 유니티 최신 버전(Unity 6)의 API 표준을 준수할 것.
- 필요한 경우 UnityEngine, TMPro, UnityEngine.UI 등의 네임스페이스를 포함할 것.
- Output ONLY the code. No explanations.
- 유니티 기준이기 때문에 ```csharp ... 는 필요하지 않음
"@
    $fullPrompt | gemini --approval-mode auto_edit | Out-File -FilePath $targetPath -Encoding utf8
    return ($LASTEXITCODE -eq 0)
}

function Run-Reviewer {
    param (
        [string]$userIntent,
        [string]$writerGuide,
        [string]$reviewGuide,
        [string]$targetPath, 
        [string]$language, 
        [string]$resultPath
    )
    
    if (-not (Test-Path $targetPath)) { return }
    Write-Host "`n[2/2] CLI 2: 외부 양식에 따른 비판적 검토 중..." -ForegroundColor Yellow
    
    $code = Get-Content $targetPath -Raw
    
    $reviewPrompt = @"
[Context]
- Platform: $Platform
- Language: $language
- User Intent: $userIntent

[Writer's Design Guideline]
$writerGuide

[Mandatory Review Format & Standards]
$reviewGuide

### SOURCE CODE TO REVIEW
$code

[Final Instruction]
반드시 위 [Mandatory Review Format & Standards]에 정의된 양식과 헤더 구조를 엄격히 준수하여 리뷰를 작성하라.
작성 지침(DLV)이 코드에 반영되었는지 분석하고, Unity 6 환경에서의 결함을 비판적으로 보고하라.
서론/결론 없이 양식의 첫 항목부터 즉시 출력을 시작하라.
"@
    $reviewPrompt | gemini --approval-mode plan | Tee-Object -FilePath $resultPath
    Write-Host "`n📝 검토 보고서가 저장되었습니다: $resultPath" -ForegroundColor Green
}

# ---------------------------------------------------------
# 메인 실행 구역
# ---------------------------------------------------------

if (-not $userPrompt) { $userPrompt = "DLV 체인 구조에 맞게 코드를 개선해줘." }

Write-Host "`n====================================================" -ForegroundColor Magenta
Write-Host "🚀 GEMINI AI DEVELOPMENT CHAIN (Unity 6 Optimized)" -ForegroundColor Magenta
Write-Host "====================================================`n"

$writerGuide = Get-GuidelineContent $config.GuidelinePath "작성(Writer)"
$reviewerGuide = Get-GuidelineContent $config.ReviewPath "검토(Reviewer)"

# [수정] $targetFilePath를 직접 사용합니다.
$success = Run-Writer -prompt $userPrompt -guideline $writerGuide -targetPath $targetFilePath -language $config.Lang

if ($success) {
    Write-Host "✅ 코드 작업 완료: $targetFilePath" -ForegroundColor Green
    Write-Host "⏳ $($config.WaitTime)초간 대기 (API Quota 리셋)..." -ForegroundColor Gray
    Start-Sleep -Seconds $config.WaitTime
    
    Run-Reviewer -userIntent $userPrompt -writerGuide $writerGuide -reviewGuide $reviewerGuide -targetPath $targetFilePath -language $config.Lang -resultPath $config.ReviewFile
} else {
    Write-Host "❌ CLI 1 실행 실패." -ForegroundColor Red
}

Write-Host "`n====================================================" -ForegroundColor Magenta
Write-Host "✨ 모든 공정 완료" -ForegroundColor Magenta
Write-Host "===================================================="