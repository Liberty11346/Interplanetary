# StartServer.ps1 - 게임 서버 자동 실행 스크립트

$ServerPath = "c:\Users\a0108\Git\ProjectShared\InterPlanetery_Server\Servers\BaseServer"
$ProjectFile = Join-Path $ServerPath "BaseServer.csproj"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "인터플래너터리 게임 서버 시작" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 서버 디렉토리 확인
if (-not (Test-Path $ServerPath)) {
    Write-Host "❌ 서버 경로를 찾을 수 없습니다: $ServerPath" -ForegroundColor Red
    exit 1
}

Write-Host "📁 서버 경로: $ServerPath" -ForegroundColor Green
Write-Host "▶️  서버 시작 중..." -ForegroundColor Yellow
Write-Host ""

# 서버 실행
try {
    Push-Location $ServerPath
    dotnet run
}
catch {
    Write-Host "❌ 서버 실행 중 오류 발생: $_" -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}
