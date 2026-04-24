param (
    [string]$userPrompt,
    [string]$targetFilePath,
    [string]$lang = "C# (Unity 6.0)",
    [string]$Platform = "Unity Engine (6000.3.10f1)"
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$config = @{
    ReviewFile    = "Assets/96. WT/TempReview.md"
    GuidelineFile = "C:\Users\kot77\Desktop\Unity\Bond\Assets\Ignore\Guide\통합_AI_작업_가이드라인.md"
    WaitTime      = 30
}

if (-not $targetFilePath) { $targetFilePath = $config.TempCode }

function Get-Guideline {
    if (-not (Test-Path $config.GuidelineFile)) {
        Write-Host "⚠️ 가이드라인 파일 없음: $($config.GuidelineFile)" -ForegroundColor Yellow
        return ""
    }
    return Get-Content $config.GuidelineFile -Raw
}

function Run-Writer {
    param ([string]$prompt, [string]$guide, [string]$targetPath)

    $originalCode = ""
    if (Test-Path $targetPath) {
        $originalCode = Get-Content $targetPath -Raw
        Write-Host "🔍 리팩토링 모드: $targetPath" -ForegroundColor Gray
    }

    $refactorBlock = if ($originalCode) {
        "`n[Original Code]`n$originalCode`n[/Original Code]"
    } else { "" }

    $fullPrompt = @"
Platform: $Platform | Lang: $lang
[Guide]
$guide
$refactorBlock
[Task]
$prompt
[Rules] 
Unity6 API 준수. 코드만 출력(설명·```csharp 금지).
Unity에서 사용한다는 것을 전제로 코드를 작성하라
"@

    Write-Host "[1/2] 코드 생성 중..." -ForegroundColor Cyan
    $fullPrompt | gemini --approval-mode auto_edit | Out-File -FilePath $targetPath -Encoding utf8
    return ($LASTEXITCODE -eq 0)
}

function Run-Reviewer {
    param ([string]$intent, [string]$guide, [string]$targetPath, [string]$dstPath)

    if (-not (Test-Path $targetPath)) { return }

    $code = Get-Content $targetPath -Raw

    $reviewPrompt = @"
Platform: $Platform | Lang: $lang | Intent: $intent
[Guide]
$guide
[Code]
$code
[Rules]
- 가이드라인 §4 코드 리뷰 양식을 엄격히 준수하라.
- DLV 무결성 및 Unity6 최적화 상태를 비판적으로 분석하라.
- 서론/결론/인사/```코드블록 금지. 첫 헤더부터 즉시 출력하라.
"@

    Write-Host "`n[2/2] 리뷰 중..." -ForegroundColor Yellow
    $reviewPrompt | gemini --approval-mode plan | Tee-Object -FilePath $dstPath
    Write-Host "`n📝 저장 완료: $dstPath" -ForegroundColor Green
}

# --- Main ---
if (-not $userPrompt) { $userPrompt = "DLV 구조에 맞게 코드를 개선해줘." }

Write-Host "`n=== GEMINI AI DEV CHAIN ===" -ForegroundColor Magenta

# 가이드라인은 1회만 로드 → Writer·Reviewer 공유
$guide = Get-Guideline

$success = Run-Writer -prompt $userPrompt -guide $guide -targetPath $targetFilePath

if ($success) {
    Write-Host "✅ 코드 완료: $targetFilePath" -ForegroundColor Green
    Write-Host "⏳ $($config.WaitTime)s 대기 (API Quota)..." -ForegroundColor Gray
    Start-Sleep -Seconds $config.WaitTime
    Run-Reviewer -intent $userPrompt -guide $guide -targetPath $targetFilePath -dstPath $config.ReviewFile
} else {
    Write-Host "❌ 코드 생성 실패" -ForegroundColor Red
}

Write-Host "============================" -ForegroundColor Magenta
