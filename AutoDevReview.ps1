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
}

function Get-Guideline {
    if (-not (Test-Path $config.GuidelineFile)) {
        Write-Host "⚠️ 가이드라인 파일 없음: $($config.GuidelineFile)" -ForegroundColor Yellow
        return "N/A"
    }
    return Get-Content $config.GuidelineFile -Raw
}

function Run-Reviewer {
    param ([string]$intent, [string]$guide, [string]$srcPath, [string]$dstPath)

    if (-not (Test-Path $srcPath)) {
        Write-Host "❌ 대상 파일 없음: $srcPath" -ForegroundColor Red
        return
    }

    $code = Get-Content $srcPath -Raw

    $reviewPrompt = @"
Platform: $Platform | Lang: $lang | Intent: $intent
[Guide]
$guide
[Code]
$code
[Rules]
- 가이드라인 §4 코드 리뷰 양식을 엄격히 준수하라.
- DLV 무결성 및 Unity6 최적화 상태를 비판적으로 분석하라.
- 서론/결론/인사/``코드블록 금지. 첫 헤더부터 즉시 출력하라.
"@

    # 임시 파일에 프롬프트 저장
    $tmpPrompt = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "gemini_prompt_$(Get-Random).txt")
    [System.IO.File]::WriteAllText($tmpPrompt, $reviewPrompt, [System.Text.Encoding]::UTF8)

    Write-Host "`n🔍 리뷰 중: $srcPath" -ForegroundColor Yellow

    # Get-Content로 읽어 파이프 → PowerShell에서 stdin 리다이렉션 대체
    $result = Get-Content -Path $tmpPrompt -Raw | gemini --approval-mode plan
    $result | Tee-Object -FilePath $dstPath

    Remove-Item $tmpPrompt -Force -ErrorAction SilentlyContinue
    Write-Host "`n📝 저장 완료: $dstPath" -ForegroundColor Green
}

# --- Main ---
if (-not $targetFilePath) { $targetFilePath = $config.TempCode }
if (-not $userPrompt)     { $userPrompt = "DLV 구조와 Unity6 최적화 상태를 검토해줘." }

Write-Host "`n=== GEMINI CODE REVIEWER ===" -ForegroundColor Magenta

$guide = Get-Guideline
Run-Reviewer -intent $userPrompt -guide $guide -srcPath $targetFilePath -dstPath $config.ReviewFile

Write-Host "============================" -ForegroundColor Magenta