param (
    [string]$userPrompt,
    [string]$targetFilePath, # 에디터에서 넘겨준 현재 작업 중인 파일 (Source)
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
    TempFile       = "96. WT/TempCode" # 수정된 코드가 저장될 경로 (확장자 제외)
    GuidelinePath  = "C:\Users\kot77\Desktop\Unity\Bond\Assets\Ignore\GEMINI"
}

# 최종 저장 경로 설정
$savePath = "$($config.TempFile).$($config.Extension)"

# ---------------------------------------------------------
# 함수 정의 구역
# ---------------------------------------------------------

function Get-GuidelineContent([string]$path, [string]$type) {
    if (-not (Test-Path -Path $path -PathType Container)) {
        Write-Host "⚠️ [경고] $type 가이드라인 폴더를 찾을 수 없습니다: $path" -ForegroundColor Yellow
        return ""
    }
    $mdFiles = Get-ChildItem -Path "$path\*.md" -ErrorAction SilentlyContinue
    if ($null -eq $mdFiles) {
        return ""
    }
    return Get-Content -Path "$path\*.md" -ErrorAction SilentlyContinue | Out-String
}

function Run-Writer {
    param (
        [string]$prompt,
        [string]$guideline,
        [string]$sourcePath,
        [string]$destinationPath,
        [string]$language
    )

    Write-Host "🚀 CLI Writer: DLV 체인 분석 및 코드 생성 시작..." -ForegroundColor Cyan

    # 원본 파일이 존재하면 읽어오기 (Refactor 모드)
    $originalCode = ""
    if ($sourcePath -and (Test-Path $sourcePath)) {
        $originalCode = Get-Content $sourcePath -Raw
        Write-Host "🔍 원본 코드 읽기 완료: $sourcePath" -ForegroundColor Gray
    } else {
        Write-Host "ℹ️ 원본 파일이 없어 신규 코드로 작성을 진행합니다." -ForegroundColor Gray
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
- [Original Code to Refactor]를 분석하여 로직을 유지하되 DLV 체인 구조로 최적화하라.
- Unity 6 (6000.x) 최신 API 표준 및 C# 최신 기능을 활용할 것.
- 반드시 코드로만 응답하고 설명이나 마크다운 기호(```csharp)는 생략하라.
- 이 코드는 $destinationPath 에 저장될 예정이므로 해당 맥락에 맞게 작성하라.
"@

    # 코드 생성 및 TempCode 파일로 출력
    $fullPrompt | gemini --approval-mode auto_edit | Out-File -FilePath $destinationPath -Encoding utf8
    return ($LASTEXITCODE -eq 0)
}

# ---------------------------------------------------------
# 메인 실행 구역
# ---------------------------------------------------------

if (-not $userPrompt) { $userPrompt = "DLV 체인 구조에 맞게 코드를 개선해줘." }

Write-Host "`n====================================================" -ForegroundColor Magenta
Write-Host "🛠️ AUTO DEV CODE WRITER (Refactoring Mode)"
Write-Host "====================================================`n"

# 1. 가이드라인 로드
$writerGuide = Get-GuidelineContent $config.GuidelinePath "작성(Writer)"

# 2. 실행: 원본(targetFilePath)을 읽고 결과(savePath)를 저장
$success = Run-Writer -prompt $userPrompt `
                      -guideline $writerGuide `
                      -sourcePath $targetFilePath `
                      -destinationPath $savePath `
                      -language $config.Lang

if ($success) {
    Write-Host "`n✅ 작업이 완료되었습니다." -ForegroundColor Green
    Write-Host "📄 참조 원본: $targetFilePath" -ForegroundColor Gray
    Write-Host "📍 저장 완료: $savePath" -ForegroundColor Yellow
} else {
    Write-Host "`n❌ 코드 작성 중 오류가 발생했습니다." -ForegroundColor Red
}

Write-Host "`n====================================================" -ForegroundColor Magenta