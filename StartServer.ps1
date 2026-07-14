# StartServer.ps1 - 게임 서버(BaseServer) 실행 도우미 스크립트
#
# 개인 절대 경로를 하드코딩하지 않습니다. 대신 아래 순서로 서버 경로를 찾습니다:
#   1) -ServerPath 파라미터로 명시적 전달
#   2) 환경 변수 INTERPLANETARY_SERVER_PATH
#   3) 이 스크립트 위치 기준의 저장소 상대 경로(형제 서버 리포)
# 모든 실패 지점은 즉시 조치 가능한 메시지를 출력합니다.
#
# 사용 예:
#   .\StartServer.ps1
#   .\StartServer.ps1 -ServerPath "D:\repos\InterPlanetery_server\Servers\BaseServer"
#
# 서버는 기본적으로 127.0.0.1:9000 에 바인드됩니다(BaseServer/appsettings.json 의 Server:Host/Port).
# 클라이언트 기본 프로필(Assets/StreamingAssets/server_profiles.json 의 "Default")도 127.0.0.1:9000 입니다.

[CmdletBinding()]
param(
    [string]$ServerPath = ""
)

$ErrorActionPreference = "Stop"

function Resolve-ServerPath {
    param([string]$Explicit)

    # 1) 명시적 파라미터
    if (-not [string]::IsNullOrWhiteSpace($Explicit)) {
        return $Explicit
    }

    # 2) 환경 변수
    $envPath = $env:INTERPLANETARY_SERVER_PATH
    if (-not [string]::IsNullOrWhiteSpace($envPath)) {
        return $envPath
    }

    # 3) 스크립트 위치 기준 저장소 상대 경로 후보들
    $scriptDir = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptDir)) {
        $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    }
    $candidates = @(
        (Join-Path $scriptDir "..\InterPlanetery_server\Servers\BaseServer"),
        (Join-Path $scriptDir "..\..\InterPlanetery_server\Servers\BaseServer")
    )
    foreach ($candidate in $candidates) {
        $resolveResult = Resolve-Path $candidate -ErrorAction SilentlyContinue
        $resolved = if ($null -ne $resolveResult) { $resolveResult.Path } else { $null }
        if (-not [string]::IsNullOrWhiteSpace($resolved)) {
            return $resolved
        }
    }

    return $null
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "인터플래너터리 게임 서버 시작" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. dotnet CLI 확인 (조치 가능한 에러)
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnet) {
    Write-Host "❌ 'dotnet' 명령을 찾을 수 없습니다. .NET 8 SDK 이상을 설치하세요:" -ForegroundColor Red
    Write-Host "   https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    Write-Host "   (BaseServer는 net8.0, TestClient는 net9.0 을 대상으로 합니다.)" -ForegroundColor Yellow
    exit 1
}

# 2. 서버 경로 탐색 (파라미터 > 환경 변수 > 저장소 상대 경로)
$ServerPath = Resolve-ServerPath -Explicit $ServerPath

if ([string]::IsNullOrWhiteSpace($ServerPath)) {
    Write-Host "❌ BaseServer 경로를 자동으로 찾지 못했습니다." -ForegroundColor Red
    Write-Host "   다음 중 하나로 지정하세요:" -ForegroundColor Yellow
    Write-Host "     .\StartServer.ps1 -ServerPath '<서버 리포>\Servers\BaseServer'" -ForegroundColor Yellow
    Write-Host "   또는 환경 변수:" -ForegroundColor Yellow
    Write-Host "     `$env:INTERPLANETARY_SERVER_PATH = '<서버 리포>\Servers\BaseServer'" -ForegroundColor Yellow
    Write-Host "   기대 위치 예: <리포 루트>\InterPlanetery_server\Servers\BaseServer" -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path $ServerPath -PathType Container)) {
    Write-Host "❌ 서버 디렉토리가 존재하지 않습니다: $ServerPath" -ForegroundColor Red
    Write-Host "   -ServerPath 로 올바른 경로를 전달하거나 환경 변수 INTERPLANETARY_SERVER_PATH 를 확인하세요." -ForegroundColor Yellow
    exit 1
}

$ProjectFile = Join-Path $ServerPath "BaseServer.csproj"
if (-not (Test-Path $ProjectFile -PathType Leaf)) {
    Write-Host "❌ 프로젝트 파일을 찾을 수 없습니다: $ProjectFile" -ForegroundColor Red
    Write-Host "   지정한 디렉토리가 'BaseServer.csproj' 를 포함하는 BaseServer 폴더인지 확인하세요." -ForegroundColor Yellow
    exit 1
}

Write-Host "📁 서버 경로: $ServerPath" -ForegroundColor Green
Write-Host "🔌 예상 바인드: 127.0.0.1:9000 (appsettings.json Server:Host/Port 기준)" -ForegroundColor Green
Write-Host "▶️  서버 시작 중... (dotnet run)" -ForegroundColor Yellow
Write-Host ""

# 3. 서버 실행
try {
    Push-Location $ServerPath
    & dotnet run
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ dotnet run 이 종료 코드 $LASTEXITCODE 로 실패했습니다." -ForegroundColor Red
        exit $LASTEXITCODE
    }
}
catch {
    Write-Host "❌ 서버 실행 중 오류 발생: $_" -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}
