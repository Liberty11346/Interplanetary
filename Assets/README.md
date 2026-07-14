# Interplanetary (Assets)

Unity 기반 실시간 멀티플레이어 우주 전략 게임 클라이언트의 Assets 레벨 개요입니다.

> **참고**: 전체 프로젝트 개요 및 시작 방법은 루트 `README.md` 를 참조하세요.

## Unity 버전

- **Unity 6000.0.43f1** (필수, `ProjectSettings/ProjectVersion.txt`)

## 프로젝트 개요

이 프로젝트는 플레이어가 함대를 조종하여 행성으로 이루어진 은하계를 정복하는 멀티플레이어 게임의 클라이언트입니다. 실시간 네트워킹, 중앙 집중식 게임 관리 시스템, 그리고 분리된 시각화 레이어를 특징으로 합니다.

## 주요 기능

-   **중앙 집중식 상태 관리**: 게임 상태는 `GameManager` 싱글톤에 의해 관리됩니다.
-   **이벤트 기반 아키텍처**: UI 와 시각화는 이벤트를 사용하여 게임 로직과 분리됩니다.
-   **서버 권한 (Server Authority)**: 모든 게임 로직은 서버에서 실행되며, 클라이언트는 명령을 전송하고 상태를 시각화합니다.
-   **분리된 시각화 레이어**: `VisualizationManager` 는 게임 로직과 분리되어 모든 시각적 표현을 관리합니다 (legacy/editor 접근).

## 프로젝트 구조

-   `Assets/0.Scenes`: 게임 씬
    -   `Scene/mainScene.unity`: 시작 씬
-   `Assets/1.Scripts`: 모든 C# 소스 코드
    -   `CommonLib`: 공통 데이터 구조, 프로토콜, 명령, 싱글톤 코어
        -   `Commands/`: ProduceFleetCommand, MoveFleetCommand 등
        -   `Protocol.cs`, `ProtocolTypes.cs`: 네트워킹 프로토콜
        -   `GameDataStructures.cs`: 게임 데이터 구조
        -   `SingletonBase.cs`: 싱글톤 레지스트리 패턴 (Domain Reload 해결)
    -   `Game/`: 게임 로직 및 UI
        -   `GameManager.cs`: 게임 상태 싱글톤 (선택, 명령, 맵 로드)
        -   `GameSceneInitializer.cs`: 애플리케이션 초기화 (BeforeSceneLoad)
        -   `UI/`: UIGame, UIPlanet, UIFleet 등
    -   `Model/`: 게임 플레이 모델
        -   `GamePlayManager.cs`: 명령/상태 처리, GameStateChangeSet
    -   `Network/`: 네트워킹
        -   `ClientServerHandler.cs`: 서버 연결/전송
        -   `NetworkManager.cs`: TCP 연결, 메시지 디스패치 (메인 스레드)
        -   `ServerProfile.cs`: 서버 프로필 관리 (127.0.0.1:9000 등)
    -   `Visualization/`:
        -   `VisualizationManager.cs`: legacy/editor 접근 (현재는 UIGame 이 시각화 담당)
-   `Assets/2.Sprite`: 행성, 함대, UI 요소에 대한 이미지 에셋
-   `Assets/3.Prefab`: 행성, 함대 및 UI 요소에 대한 게임 오브젝트 프리팹
-   `Assets/4.Animation`: 애니메이션 에셋
-   `Assets/5.Audio`: 오디오 에셋
-   `Assets/6.renderTexture`: 렌더 텍스처 에셋
-   `Assets/7.TextMesh Pro`: TextMesh Pro 에셋 및 리소스
-   `Assets/Data`: 게임 데이터용 스크립터블 오브젝트 (예: `Map1Data.asset`)
-   `Assets/Editor`: 커스텀 Unity 에디터 스크립트 (예: `VisualizationManagerEditor.cs`)
-   `Assets/StreamingAssets`: `server_profiles.json` (서버 프로필)
-   `Packages`: Unity 패키지 매니페스트 (`manifest.json`) 및 잠금 파일
-   `ProjectSettings`: Unity 프로젝트 구성 파일

## 핵심 구성 요소

-   **`GameManager.cs`**: 게임 상태, 플레이어 데이터, 선택 로직을 관리하는 중앙 싱글톤.
    -   Public API: `SelectPlanet(int)`, `SelectFleet(int)`, `CommandFleetMovement(int, int)`, `CommandFleetSpawn(int)`, `GetCurrentTick()`, `LoadMapAsync(int)`
    -   Events: `OnPlanetSelected(int)`, `OnFleetSelected(int)`
-   **`GamePlayManager.cs`**: 명령/상태 처리, GameStateChangeSet 생성.
    -   Public API: `RequestProduceFleet(int)`, `RequestMoveFleet(int, int)`, `SendChatMessage(string)`, `CreateRoom(string, int, bool)`, `JoinRoom(string, int, int)`, `RequestReady(bool)`
    -   Events: `GameStateReceived(GameState, long)`, `GameStateChanged(GameStateChangeSet)`
-   **`ClientServerHandler.cs`**: 서버 연결/전송 핸들러.
    -   Public API: `ConnectAsync(string, int)`, `Disconnect()`, `AsyncSend(Protocol)`, `RegisterHandler(int, handler)`, `IsConnected`
-   **`NetworkManager.cs`**: TCP 연결, 메시지 디스패치 (메인 스레드).
    -   Public API: `ConnectAsync(string, int)`, `Disconnect()`, `RegisterHandler(int, handler)`, `HandleIncomingProtocol(Protocol)` (unitySyncContext.Post)
-   **`ServerProfile.cs`**: 서버 프로필 관리 (127.0.0.1:9000, 125.137.73.37:9000).
    -   Public API: `LoadProfile(string, bool)`, `SaveAllProfiles()`, `GetAllProfiles()`, `OnProfileChanged`
-   **`UIGame.cs`**: 게임 UI 시각화.
    -   Public API: `CreateOrUpdateFleet(FleetInfo)`, `CreateOrUpdatePlanet(PlanetInfo)`, `CreateOrUpdatePath(PathData)`, `RemoveFleet(long)`
-   **`GameSceneInitializer.cs`**: 애플리케이션 초기화 ([RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]).
    -   생성: MainThreadDispatcher, ServerProfile, ClientServerHandler, ApplicationLifecycleManager

## Legacy/Historical Components

아래 컴포넌트는 역사적 레이어로 표시됩니다 (현재 코드에 존재하지만 사용되지 않거나 대체됨):

-   **`UnityGameClient.cs`**: 이전 네트워크 클라이언트 (현재 `ClientServerHandler` + `NetworkManager` 가 대체).
-   **`PlayerController.cs`**: 플레이어 입력 처리 (stub, 현재 `GamePlayManager` 가 명령 처리).
-   **`GameUIManager.cs`**: 이전 UI 관리자 (현재 `UIGame` 이 시각화 담당).
-   **`VisualizationManager.cs`**: legacy/editor 접근, 현재는 `UIGame` 이 시각화 담당.

## 사용 기술

-   **Unity**: 6000.0.43f1 (필수)
-   **C#**: 최신 C# (nullable reference types 사용)
-   **네트워킹**: TCP (System.Net.Sockets.TcpClient)
-   **프로토콜**: Binary Header (18 bytes) + JSON Body (UTF-8)
-   **주요 Unity 패키지**:
    -   `com.unity.inputsystem`: 1.13.1
    -   `com.unity.nuget.newtonsoft-json`: 3.2.1
    -   `com.unity.multiplayer.center`: 1.0.0
    -   `com.unity.multiplayer.playmode`: 1.3.3
    -   `com.unity.timeline`: 1.8.7
    -   `com.unity.ugui`: 2.0.0
    -   `com.unity.visualscripting`: 1.9.5
    -   기타 Unity 모듈 (UI, Physics, Audio 등)
