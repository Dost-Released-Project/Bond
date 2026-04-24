param (
    [string]$userPrompt,
    [string]$targetFilePath, # 에디터 매크로($FilePath$) 대응용
    [string]$lang = "C# (Unity 6.0)",
    [string]$Platform = "Unity Engine (6000.3.10f1)"
)

# 인코딩 설정 (한글 깨짐 방지)
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

# 1. 환경 설정
$config = @{
    Lang           = $lang
    Extension      = "cs"
    ReviewFile     = "96. WT/TempReview.md" 
    GuidelinePath  = "C:\Users\kot77\Desktop\Unity\Bond\Assets\Ignore\GEMINI"
    ReviewPath     = "C:\Users\kot77\Desktop\Unity\Bond\Assets\Ignore\GEMINI_REVIEW"
}

# ---------------------------------------------------------
# 함수 정의 구역
# ---------------------------------------------------------

function Get-GuidelineContent([string]$path, [string]$type) {
    if (-not (Test-Path -Path $path -PathType Container)) {
        Write-Host "⚠️ [경고] $type 폴더가 존재하지 않습니다: $path" -ForegroundColor Yellow
        return "N/A"
    }
    $mdFiles = Get-ChildItem -Path "$path\*.md" -ErrorAction SilentlyContinue
    if ($null -eq $mdFiles) {
        Write-Host "⚠️ [경고] $type 가이드라인 파일(.md)이 없습니다." -ForegroundColor Yellow
        return "N/A"
    }
    return Get-Content -Path "$path\*.md" -ErrorAction SilentlyContinue | Out-String
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
    
    if (-not (Test-Path $targetPath)) { 
        Write-Host "❌ 분석 대상 파일이 없습니다: $targetPath" -ForegroundColor Red
        return 
    }
    
    Write-Host "`nCLI Review: [$targetPath] 분석 및 비판적 검토 중..." -ForegroundColor Yellow
    
    $code = Get-Content $targetPath -Raw
    
    # 1. 프롬프트 내 코드 블록(```) 제거 (결과물 순도 유지)
    # 2. 섹션 헤더를 명확히 하여 AI 혼동 방지
    $reviewPrompt = @"
### ANALYSIS CONTEXT
- Platform: $Platform
- Language: $language
- User Intent: $userIntent

### REFERENCE: DESIGN GUIDELINES
$writerGuide

### MANDATORY REVIEW FORMAT & STANDARDS
$reviewGuide

### SOURCE CODE TO ANALYSIS
$code

### FINAL REVIEW INSTRUCTION
1. 'MANDATORY REVIEW FORMAT & STANDARDS' 섹션에 정의된 양식과 헤더 구조를 엄격히 준수하여 리뷰를 작성하라.
2. 분석 대상 코드는 'SOURCE CODE TO ANALYSIS' 섹션에 제공된 텍스트이다.
3. 서론, 결론, 인사말, 코드 블록 기호(```)를 절대 포함하지 마라.
4. 양식에 정의된 첫 번째 헤더 항목부터 즉시 출력을 시작하라.
5. Unity 6 환경의 최적화 상태와 DLV 체인 무결성을 비판적으로 분석하라.
"@

    # 결과 전송 및 저장
    $reviewPrompt | gemini --approval-mode plan | Tee-Object -FilePath $resultPath
    
    Write-Host "`n📝 검토 보고서가 저장되었습니다: $resultPath" -ForegroundColor Green
}

# ---------------------------------------------------------
# 메인 실행 구역
# ---------------------------------------------------------

# 타겟 파일 경로 결정 (에디터 인자 우선, 없으면 기본 경로)
if (-not $targetFilePath) { $targetFilePath = "96. WT/TempCode.cs" }
if (-not $userPrompt) { $userPrompt = "현재 코드의 DLV 구조와 Unity 6 최적화 상태를 검토해줘." }

Write-Host "`n====================================================" -ForegroundColor Magenta
Write-Host "🚀 GEMINI CODE REVIEWER (Current File Analysis)" -ForegroundColor Magenta
Write-Host "====================================================`n"

# [수정] 작성 지침(Writer)과 리뷰 양식(Reviewer)을 각각 로드
$writerGuideContent = Get-GuidelineContent $config.GuidelinePath "작성(Writer)"
$reviewerGuideContent = Get-GuidelineContent $config.ReviewPath "검토(Reviewer)"

# 리뷰 프로세스 실행
Run-Reviewer -userIntent $userPrompt `
             -writerGuide $writerGuideContent `
             -reviewGuide $reviewerGuideContent `
             -targetPath $targetFilePath `
             -language $config.Lang `
             -resultPath $config.ReviewFile

Write-Host "`n====================================================" -ForegroundColor Magenta
Write-Host "✨ 모든 공정 완료" -ForegroundColor Magenta
Write-Host "===================================================="