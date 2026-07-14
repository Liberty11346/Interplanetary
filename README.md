# Interplanetary Client

Unity 기반 실시간 멀티플레이어 우주 전략 게임 클라이언트입니다.

---

## ⚠️ 현재 상태 및 블록킹 이슈

### 필수 환경 요구사항 (아직 설치 안 됨)
- **Unity Editor 6000.0.43f1** (필수)
  - 현재 프로젝트는 Unity 6000.0.43f1 로 설정되어 있습니다 (`ProjectSettings/ProjectVersion.txt`).
  - 설치되지 않은 경우: Unity Hub → "Add Editor" → 6000.0.43f1 선택.

### 알려진 블록킹 이슈
| ID | 항목 | 상태 |
|----|------|------|
| ENV-1 | Unity 6000.0.43f1 설치 안 됨 | BLOCKED |
| TEST-3 | DB 스키마/시드 데이터 미확인 | BLOCKED |
| SEC-4 | Production endpoint (125.137.73.37:9000) 결정 대기 | BLOCKED |
| COR-1 | `WaittingRoomUser` → `WaitingRoomUser` casing drift | TODO |

> **Production endpoint**: `Assets/StreamingAssets/server_profiles.json` 에 Production 프로필로 `125.137.73.37:9000` 이 커밋되어 있습니다. 변경 계획은 사용자 결정을 기다립니다 (문서만 업데이트, 코드 변경 없음).

---

## 프로젝트 개요

플레이어가 함대를 조종하여 행성으로 이루어진 은하계를 정복하는 실시간 멀티플레이어 전략 게임입니다.

- **중앙 집중식 상태 관리**: `GameManager` 싱글톤이 게임 상태, 플레이어 데이터, 선택 로직을 관리합니다.
- **이벤트 기반 아키텍처**: UI 와 시각화는 이벤트를 사용하여 게임 로직과 분리됩니다.
- **서버 권한 (Server Authority)**: 모든 게임 로직은 서버에서 실행되며, 클라이언트는 명령을 전송하고 상태를 시각화합니다.
- **분리된 시각화 레이어**: `VisualizationManager` 는 게임 로직과 분리되어 모든 시각적 표현을 관리합니다 (legacy/editor 접근).

---

## 기술 스택

- **Unity Engine**: 6000.0.43f1 (필수)
- **C#**: 최신 C# (nullable reference types 사용)
- **네트워킹**: TCP (System.Net.Sockets.TcpClient)
- **프로토콜**: Binary Header (18 bytes) + JSON Body (UTF-8)
- **주요 Unity 패키지** (`Packages/manifest.json`):
  - `com.unity.inputsystem`: 1.13.1
  - `com.unity.nuget.newtonsoft-json`: 3.2.1
  - `com.unity.multiplayer.center`: 1.0.0
  - `com.unity.multiplayer.playmode`: 1.3.3
  - `com.unity.timeline`: 1.8.7
  - `com.unity.visualscripting`: 1.9.5
  - `com.unity.ugui`: 2.0.0
  - 기타 Unity 모듈 (UI, Physics, Audio 등)

---

## 시작하기

### 1. Unity 설치 및 프로젝트 오픈
```bash
# Unity 6000.0.43f1 설치 (필수)
# Unity Hub → Add Editor → 6000.0.43f1

# 프로젝트 오픈
# Unity Hub → Add Project → J:\prj\Interplanetery\Interplanetary_client 선택
```

### 2. 서버 실행
```powershell
# 서버 리포가 형제 폴더에 있는 경우 (자동 감지)
.\StartServer.ps1

# 명시적 경로 지정
.\StartServer.ps1 -ServerPath "D:\repos\InterPlanetery_server\Servers\BaseServer"

# 환경 변수 사용
$env:INTERPLANETARY_SERVER_PATH = "D:\repos\InterPlanetery_server\Servers\BaseServer"
.\StartServer.ps1
```

서버는 기본적으로 `127.0.0.1:9000` 에 바인드됩니다.

### 3. 클라이언트 서버 프로필 설정
`Assets/StreamingAssets/server_profiles.json` 에서 활성화할 서버 프로필을 선택합니다:

- **Default**: `127.0.0.1:9000` (로컬 테스트)
- **Production**: `125.137.73.37:9000` (커밋됨, 결정 대기)

### 4. 씬 로드
Unity 에디터에서 `Assets/0.Scenes/Scene/mainScene.unity` 를 로드합니다.

---

## 프로젝트 구조

```
Interplanetary_client/
├── Assets/
│   ├── 0.Scenes/           # 게임 씬
│   │   └── Scene/
│   │       └── mainScene.unity  # 시작 씬
│   ├── 1.Scripts/          # 모든 C# 소스 코드
│   │   ├── CommonLib/      # 공통 데이터 구조, 프로토콜, 명령
│   │   │   ├── Commands/   # ProduceFleetCommand, MoveFleetCommand 등
│   │   │   ├── Protocol.cs, ProtocolTypes.cs
│   │   │   ├── GameDataStructures.cs
│   │   │   └── SingletonBase.cs (싱글톤 레지스트리 패턴)
│   │   ├── Game/           # 핵심 게임 로직 및 관리자
│   │   │   ├── GameManager.cs          # 게임 상태 싱글톤
│   │   │   ├── GameSceneInitializer.cs # 애플리케이션 초기화 (BeforeSceneLoad)
│   │   │   └── UI/         # UI 레이어 스크립트
│   │   ├── Model/          # 게임 플레이 모델
│   │   │   └── GamePlayManager.cs     # 명령/상태 처리, GameStateChangeSet
│   │   ├── Network/        # 네트워킹
│   │   │   ├── ClientServerHandler.cs # 서버 연결/전송 핸들러
│   │   │   ├── NetworkManager.cs      # TCP 연결, 메시지 디스패치
│   │   │   └── ServerProfile.cs       # 서버 프로필 관리 (127.0.0.1:9000 등)
│   │   └── Visualization/  # 시각화 레이어 (legacy/editor)
│   │       └── VisualizationManager.cs
│   ├── 2.Sprite/           # 행성, 함대, UI 에셋
│   ├── 3.Prefab/           # 프리팹
│   ├── 4.Animation/        # 애니메이션
│   ├── 5.Audio/            # 오디오
│   ├── 6.renderTexture/    # 렌더 텍스처
│   ├── 7.TextMesh Pro/     # TextMesh Pro 리소스
│   ├── Data/               # ScriptableObjects (Map1Data.asset 등)
│   ├── Editor/             # 커스텀 에디터 도구
│   │   └── VisualizationManagerEditor.cs
│   ├── StreamingAssets/    # server_profiles.json (서버 프로필)
│   ├── CHANGELOG_SingletonFix.md  # 2025-11-28 싱글톤 수정 기록
│   ├── NetworkLogicAnalysis.md    # 네트워크 로직 분석
│   ├── Protocol관리.md    # 프로토콜 목록
│   ├── README.md           # 이 파일 (Assets 버전)
│   └── 유저_플로우.md     # 서버 플로우 분석
├── Packages/               # Unity 패키지 (manifest.json)
├── ProjectSettings/        # 프로젝트 설정 (ProjectVersion.txt)
├── ClientStateTests/       # headless 클라이언트 테스트 (untracked)
├── StartServer.ps1         # 서버 실행 도우미
└── README.md               # 이 파일 (루트 버전)
```

---

## 핵심 구성 요소

### 게임 로직
- **`GameManager`**: 게임 상태, 플레이어 데이터, 선택 로직을 관리하는 중앙 싱글톤.
  - Public API: `SelectPlanet(int)`, `SelectFleet(int)`, `CommandFleetMovement(int, int)`, `CommandFleetSpawn(int)`, `GetCurrentTick()`, `LoadMapAsync(int)`
  - Events: `OnPlanetSelected(int)`, `OnFleetSelected(int)`
- **`GamePlayManager`**: 명령/상태 처리, GameStateChangeSet 생성.
  - Public API: `RequestProduceFleet(int)`, `RequestMoveFleet(int, int)`, `SendChatMessage(string)`, `CreateRoom(string, int, bool)`, `JoinRoom(string, int, int)`, `RequestReady(bool)`
  - Events: `GameStateReceived(GameState, long)`, `GameStateChanged(GameStateChangeSet)`
- **`GameSceneInitializer`**: 애플리케이션 초기화 (BeforeSceneLoad).
  - [RuntimeInitializeOnLoadMethod] `InitializeApplicationServices()`: MainThreadDispatcher, ServerProfile, ClientServerHandler, ApplicationLifecycleManager 생성.

### 네트워킹
- **`ClientServerHandler`**: 서버 연결/전송 핸들러.
  - Public API: `ConnectAsync(string, int)`, `Disconnect()`, `AsyncSend(Protocol)`, `SendAndWaitAsync(Protocol)`, `RegisterHandler(int, handler)`, `IsConnected`
- **`NetworkManager`**: TCP 연결, 메시지 디스패치 (메인 스레드).
  - Public API: `ConnectAsync(string, int)`, `Disconnect()`, `RegisterHandler(int, handler)`, `SendAsync(Protocol)`, `HandleIncomingProtocol(Protocol)` (unitySyncContext.Post)
- **`ServerProfile`**: 서버 프로필 관리 (127.0.0.1:9000, 125.137.73.37:9000).
  - Public API: `LoadProfile(string, bool)`, `SaveAllProfiles()`, `GetAllProfiles()`, `OnProfileChanged`

### 시각화 (Legacy/Editor)
- **`VisualizationManager`**: 게임 로직과 분리된 시각적 표현 (legacy/editor 접근).
  - Legacy: `CreatePlanetWithOwner`, `CreateFleet` (Editor 에서 직접 호출 가능)
  - 현재는 `UIGame` 이 시각화를 담당하며, VisualizationManager 는 역사적 레이어로 표시됩니다.

### UI
- **`UIGame`**: 게임 UI 시각화.
  - Public API: `CreateOrUpdateFleet(FleetInfo)`, `CreateOrUpdatePlanet(PlanetInfo)`, `CreateOrUpdatePath(PathData)`, `RemoveFleet(long)`

---

## 네트워크 프로토콜

### 헤더 (18 bytes)
| 필드 | 크기 | 설명 |
|------|------|------|
| TotalSize | 4 bytes | 헤더 포함 전체 패킷 크기 |
| Type | 4 bytes | 프로토콜 ID |
| Timestamp | 8 bytes | 생성 시간 |
| ParamCount | 2 bytes | 파라미터 개수 |

### 바디
UTF-8 인코딩 JSON 문자열.

### 주요 프로토콜 (C→S)
| ID | 이름 | 설명 | 파라미터 |
|----|------|------|----------|
| 10000 | REQUEST_LOGIN | 로그인 요청 | username, password |
| 10001 | REQUEST_LOGOUT | 로그아웃 요청 | - |
| 10002 | CHAT_MESSAGE | 메시지 전송 | message |
| 10003 | HEARTBEAT | 하트비트 | timestamp |
| 10004 | REQUEST_TABLEDATA | 테이블 데이터 요청 | table_name |
| 10005 | REQUEST_REGISTER | 회원가입 요청 | username, password |
| 10006 | REQUEST_REGISTER_AUTO | 자동 회원가입 | - |
| 10010 | REQUEST_JOIN_LOBBY | 로비 입장 요청 | Page |
| 10011 | REFRESH_LOBBY | 로비 새로고침 | - |
| 10012 | REQUEST_CREATE_ROOM | 방 생성 요청 | roomName, mapId, isPrivate |
| 10013 | REQUEST_JOIN_ROOM | 방 입장 요청 | userId, roomId, slot |
| 10014 | REQUEST_READY | 게임 레디 | isReady |
| 10015 | REQUEST_LEFT_ROOM | 방 퇴장 요청 | - |
| **30100** | **SUBMIT_COMMAND** | **명령 제출** | **commandType, tick, target/target_fleet/target_planet** |

### 주요 프로토콜 (S→C)
| ID | 이름 | 설명 |
|----|------|------|
| 20000 | RESPONSE | 전체 공통 응답처리 |
| 20001 | BRODCAST_SYSTEM | 시스템 공통 알림 |
| 20002 | BRODCAST_CHAT_MESSAGE | 메시지 브로드캐스트 |
| 20003 | HEARTBEAT_ACK | 하트비트 응답 |
| 20010 | USER_JOINED | 유저 접속 알림 |
| 20011 | USER_LEFT | 유저 이탈 알림 |
| 20012 | ROOM_INFO_CHANGED | 방 정보 변경 알림 |
| 20013 | ROOM_CLOSED | 방 삭제 알림 |
| 20200 | GAME_SET | 게임 초기화 데이터 |
| 20201 | GAME_STARTED | 게임 시작 |
| 20202 | GAME_STATE | 게임 상태 브로드캐스트 (매 틱) |
| 20026 | GAME_ENDED | 게임 종료 |

### 명령 파라미터 (SUBMIT_COMMAND)
- **ProduceFleet**: `commandType=ProduceFleet`, `tick`, `target=fleetType`
- **MoveFleet**: `commandType=MoveFleet`, `tick`, `target_fleet=fleetId`, `target_planet=targetPlanetId`

### 채팅 파라미터 (CHAT_MESSAGE)
- `message=string`

---

## 게임 상태 동기화

### 틱 시스템
- **고정 틱 레이트**: 20 TPS (50ms/틱)
- **매 틱**: CommandProcess, ProductionProcess, MovementProcess, ConquerProcess, BroadcastEvent
- **4 틱마다**: ResourceProduction
- **10 틱마다**: CheckWinCondition

### 상태 동기화 패턴
- 서버가 매 틱 `GAME_STATE(20202)` 브로드캐스트.
- 클라이언트 `GamePlayManager` 가 `GameStateChangeSet.Create(previous, current)` 로 변경 집합을 계산.
- `GameManager` 가 `OnGameStateChanged` 이벤트를 통해 한 번만 렌더링.

---

## 아키텍처 노트

### 메인 스레드 디스패치
- `NetworkManager.HandleIncomingProtocol` 은 `unitySyncContext.Post` 를 사용하여 Unity API 접근을 안전하게 처리합니다.
- 이전의 `_incomingProtocols` 큐 + `Update` 패턴은 legacy 로 표시됩니다.

### 싱글톤 패턴
- **`SingletonRegistry`** (2025-11-28 추가): Unity 에디터 Domain Reload 문제 해결.
  - `IsQuitting` 전역 상태 관리.
  - 종료 중에도 인스턴스가 살아있다면 반환하여 `NullReferenceException` 방지.

### Bootstrap
- `GameSceneInitializer.InitializeApplicationServices` ([RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]) 이 MainThreadDispatcher, ServerProfile, ClientServerHandler, ApplicationLifecycleManager 를 생성합니다.

---

## Legacy/Historical Components

아래 컴포넌트는 역사적 레이어로 표시됩니다 (현재 코드에 존재하지만 사용되지 않거나 대체됨):

- **`UnityGameClient.cs`**: 이전 네트워크 클라이언트 (현재 `ClientServerHandler` + `NetworkManager` 가 대체).
- **`PlayerController.cs`**: 플레이어 입력 처리 (stub, 현재 `GamePlayManager` 가 명령 처리).
- **`GameUIManager.cs`**: 이전 UI 관리자 (현재 `UIGame` 이 시각화 담당).
- **`VisualizationManager.cs`**: legacy/editor 접근, 현재는 `UIGame` 이 시각화 담당.
- **`_incomingProtocols` 큐**: legacy 디스패치 패턴 (현재 `unitySyncContext.Post` 사용).

---

## 검증 및 테스트

### git diff --check (whitespace errors)
```bash
git diff --check
```

### PowerShell parse-check
```powershell
.\StartServer.ps1 -WhatIf  # 파라미터만 검증 (실행 안 함)
```

### ClientStateTests (headless)
`ClientStateTests/` 디렉토리에 headless 클라이언트 테스트가 있습니다 (gitignore, untracked).

---

## 문서

- **`Assets/README.md`**: Assets 레벨 개요.
- **`Assets/NetworkLogicAnalysis.md`**: 네트워크 로직 분석 (디스패치, 상태 동기화).
- **`Assets/Protocol관리.md`**: 프로토콜 목록.
- **`Assets/유저_플로우.md`**: 서버 플로우 분석 (서버 리포의 BaseServer).
- **`Assets/CHANGELOG_SingletonFix.md`**: 2025-11-28 싱글톤 수정 기록.
- **`Assets/0.Scenes/----new-----/README.md`**: 씬 레벨 설명 (VisualizationManager legacy).

---

## Git 상태

현재 수정된 소스 파일 (변경 금지):
- `Assets/1.Scripts/Game/GameManager.cs`
- `Assets/1.Scripts/Game/GameSceneInitializer.cs`
- `Assets/1.Scripts/Model/GamePlayManager.cs`
- `Assets/1.Scripts/Network/ClientServerHandler.cs`

문서/스크립트만 업데이트됩니다 (소스 변경 없음).

---

## 버전 정보

- **Unity**: 6000.0.43f1
- **.NET**: BaseServer (net8.0), TestClient (net9.0)
- **프로토콜 버전**: 1.0.0
