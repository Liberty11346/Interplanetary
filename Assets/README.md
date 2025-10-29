# Interplanetary

Unity로 개발된 실시간 멀티플레이어 우주 전략 게임 프로토타입입니다.

## 프로젝트 개요

이 프로젝트는 플레이어가 함대를 조종하여 행성으로 이루어진 은하계를 정복하는 멀티플레이어 게임의 클라이언트입니다. 실시간 네트워킹, 중앙 집중식 게임 관리 시스템, 그리고 분리된 시각화 레이어를 특징으로 합니다.

## 주요 기능

-   **중앙 집중식 상태 관리**: 게임 상태는 `GameManager` 싱글톤에 의해 관리됩니다.
-   **이벤트 기반 아키텍처**: UI와 시각화는 이벤트를 사용하여 게임 로직과 분리됩니다.
-   **멀티플레이어 지원**: 게임 서버에 연결하기 위한 네트워크 클라이언트를 포함합니다.
-   **커스텀 에디터 도구**: 더 빠른 개발 및 테스트를 위한 헬퍼 도구를 포함합니다.

## 프로젝트 구조

이 프로젝트는 표준 Unity 프로젝트 구조를 따릅니다. 주요 디렉토리 및 내용은 다음과 같습니다:

-   `Assets/0.Scenes`: 주요 게임 씬을 포함합니다 (예: `mainScreen.unity`, `game1Real.unity`).
-   `Assets/1.Scripts`: 게임의 모든 C# 소스 코드를 포함합니다.
    -   `CommonLib`: 공통 데이터 구조, 통신 프로토콜 및 명령을 포함합니다.
        -   `Commands`: 다양한 명령 정의를 포함합니다.
        -   `GameDataStructures.cs`: 핵심 게임 데이터 구조를 정의합니다.
        -   `PlanetData.cs`: 행성 정보에 대한 데이터 구조입니다.
        -   `Protocol.cs`, `ProtocolTypes.cs`: 네트워킹 프로토콜 정의입니다.
    -   `Game`: 핵심 게임 플레이 로직 및 관리자입니다.
        -   `GameManager.cs`: 게임 상태, 플레이어 데이터 및 선택 로직을 위한 중앙 싱글톤입니다.
        -   `PlayerController.cs`: 플레이어 입력 및 동작을 처리합니다.
        -   `VisualizationManager.cs`: 게임 로직과 분리되어 모든 시각적 표현을 관리합니다.
        -   `FleetUIButton.cs`, `PlanetUIButton.cs`: 함대 및 행성과의 UI 상호 작용을 위한 스크립트입니다.
    -   `Network`: 네트워킹 관련 스크립트입니다.
        -   `UnityGameClient.cs`: 게임 서버와의 통신을 처리합니다.
    -   `UI`: UI 관련 스크립트입니다.
        -   `GameUIManager.cs`: 게임의 사용자 인터페이스를 관리합니다.
-   `Assets/2.Sprite`: 행성, 함대, UI 요소에 대한 이미지 에셋 (예: `Boom.png`, `Explosion.png`).
-   `Assets/3.Prefab`: 행성, 함대 및 UI 요소에 대한 게임 오브젝트 프리팹 (예: `enemyBattleCruiser.prefab`, `playerScout.prefab`).
-   `Assets/4.Animation`: 애니메이션 에셋 (예: `fleetDead.anim`).
-   `Assets/5.Audio`: 오디오 에셋 (예: `gameMusic`).
-   `Assets/6.renderTexture`: 렌더 텍스처 에셋.
-   `Assets/7.TextMesh Pro`: TextMesh Pro 에셋 및 리소스.
-   `Assets/Data`: 게임 데이터용 스크립터블 오브젝트 (예: `Map1Data.asset`).
-   `Assets/Editor`: 커스텀 Unity 에디터 스크립트를 포함합니다 (예: `VisualizationManagerEditor.cs`).
-   `Packages`: Unity 패키지 매니페스트 (`manifest.json`) 및 잠금 파일을 포함합니다.
-   `ProjectSettings`: Unity 프로젝트 구성 파일.

## 핵심 구성 요소/스크립트

-   **`GameManager.cs`**: 게임의 전반적인 상태, 플레이어 데이터 및 선택 로직을 관리하는 중앙 싱글톤입니다. 게임 이벤트의 주요 허브 역할을 합니다.
-   **`VisualizationManager.cs`**: 게임의 모든 시각적 표현을 담당합니다. `GameManager`의 이벤트를 수신하고 행성 및 함대의 위치와 상태를 업데이트합니다. 자체적으로 게임 로직을 포함하지 않습니다.
-   **`GameUIManager.cs`**: 리소스 표시, 채팅 창 및 선택 정보 등 게임의 사용자 인터페이스를 관리합니다.
-   **`UnityGameClient.cs`**: 게임 서버와의 모든 통신을 처리하고, 플레이어 명령을 전송하며 게임 상태 업데이트를 수신합니다.
-   **`PlayerController.cs`**: 플레이어 입력을 관리하고 이를 게임 동작으로 변환합니다.

## 사용 기술

-   **Unity Engine**: 주요 게임 개발 플랫폼.
-   **C#**: 주요 프로그래밍 언어.
-   **Unity 패키지** (`Packages/manifest.json`에서):
    -   `com.unity.collab-proxy`
    -   `com.unity.feature.2d`
    -   `com.unity.ide.rider`
    -   `com.unity.ide.visualstudio`
    -   `com.unity.inputsystem`
    -   `com.unity.multiplayer.center`
    -   `com.unity.nuget.newtonsoft-json`
    -   `com.unity.test-framework`
    -   `com.unity.timeline`
    -   `com.unity.toolchain.win-x86_64-linux-x86_64`
    -   `com.unity.ugui`
    -   `com.unity.visualscripting`
    -   다양한 Unity 모듈 (예: `com.unity.modules.ui`, `com.unity.modules.physics`)
