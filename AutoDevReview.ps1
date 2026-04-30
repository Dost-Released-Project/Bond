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
    return Get-Content $config.GuidelineFile -Raw -Encoding UTF8
}

function Run-Reviewer {
    param ([string]$intent, [string]$guide, [string]$srcPath, [string]$dstPath)

    if (-not (Test-Path $srcPath)) {
        Write-Host "❌ 대상 파일 없음: $srcPath" -ForegroundColor Red
        return
    }

    $code = Get-Content $srcPath -Raw -Encoding UTF8

    $stdinContent = @"
[Guide]
$guide
[Code]
$code
"@

    $promptInstruction = "Platform: $Platform | Lang: $lang | Intent: $intent | Rules: 가이드라인 §4 코드 리뷰 양식 엄수. DLV 무결성 및 Unity6 최적화 비판적 분석. 서론/결론/인사/코드블록 금지. 첫 헤더부터 즉시 출력."

    Write-Host "`n🔍 리뷰 중: $srcPath" -ForegroundColor Yellow

    $result = $stdinContent | gemini --approval-mode plan -p $promptInstruction

    # 출력 디렉토리 자동 생성
    $dstDir = Split-Path $dstPath -Parent
    if ($dstDir -and -not (Test-Path $dstDir)) {
        New-Item -ItemType Directory -Path $dstDir -Force | Out-Null
    }

    $result | Tee-Object -FilePath $dstPath
    Write-Host "`n📝 저장 완료: $dstPath" -ForegroundColor Green
}

# --- Main ---
if (-not $targetFilePath) { $targetFilePath = $config.TempCode }
if (-not $userPrompt)     { $userPrompt = "DLV 구조와 Unity6 최적화 상태를 검토해줘." }

Write-Host "`n=== GEMINI CODE REVIEWER ===" -ForegroundColor Magenta
$guide = Get-Guideline
Run-Reviewer -intent $userPrompt -guide $guide -srcPath $targetFilePath -dstPath $config.ReviewFile
Write-Host "============================" -ForegroundColor Magenta