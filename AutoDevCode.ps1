param (
    [string]$userPrompt,
    [string]$targetFilePath,
    [string]$lang = "C# (Unity 6.0)",
    [string]$Platform = "Unity Engine (6000.3.10f1)"
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$config = @{
    GuidelineFile = "C:\Users\kot77\Desktop\Unity\Bond\Assets\Ignore\Guide\통합_AI_작업_가이드라인.md"
}

function Get-Guideline {
    if (-not (Test-Path $config.GuidelineFile)) {
        Write-Host "⚠️ 가이드라인 파일 없음: $($config.GuidelineFile)" -ForegroundColor Yellow
        return ""
    }
    return Get-Content $config.GuidelineFile -Raw
}

function Run-Writer {
    param ([string]$prompt, [string]$guide, [string]$srcPath, [string]$dstPath)

    $originalCode = ""
    if ($srcPath -and (Test-Path $srcPath)) {
        $originalCode = Get-Content $srcPath -Raw
        Write-Host "🔍 리팩토링 모드: $srcPath" -ForegroundColor Gray
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
[Rules] Unity6 API 준수. 코드만 출력(설명·```csharp 금지).
"@

    Write-Host "🚀 코드 생성 중..." -ForegroundColor Cyan
    $fullPrompt | gemini --approval-mode auto_edit | Out-File -FilePath $dstPath -Encoding utf8
    return ($LASTEXITCODE -eq 0)
}

# --- Main ---
if (-not $userPrompt) { $userPrompt = "DLV 구조에 맞게 코드를 개선해줘." }

Write-Host "`n=== AUTO DEV CODE WRITER ===" -ForegroundColor Magenta

$guide = Get-Guideline
$dst   = if ($targetFilePath) { $targetFilePath } else { $config.TempFile }

$success = Run-Writer -prompt $userPrompt -guide $guide -srcPath $targetFilePath -dstPath $dst

if ($success) {
    Write-Host "✅ 저장 완료: $dst" -ForegroundColor Green
} else {
    Write-Host "❌ 오류 발생" -ForegroundColor Red
}
Write-Host "============================" -ForegroundColor Magenta
