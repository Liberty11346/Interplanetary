# Interplanetary 프로젝트 네트워크 로직 분석

이 문서는 `Interplanetary` 프로젝트의 클라이언트 구현을 기반으로, 현재 작동 중인 클라이언트 로직과 이에 대응하여 서버가 수행해야 할(또는 수행 중인) 로직을 설명합니다.

> **참고**: 이 문서는 클라이언트 코드를 분석하여 작성되었으며, 서버 측 로직은 클라이언트의 요청과 응답 처리를 기반으로 **추론 및 예상된 코드**를 염두에 두고 기술되었습니다. 실제 서버 구현과는 세부 사항에서 차이가 있을 수 있습니다.

## 1. 네트워크 아키텍처 개요

*   **통신 방식**: TCP (System.Net.Sockets.TcpClient)
*   **프로토콜 포맷**: 커스텀 바이너리 헤더 + JSON 바디
    *   **Header (18 bytes)**:
        *   `TotalSize` (4 bytes): 헤더 포함 전체 패킷 크기
        *   `Type` (4 bytes): 프로토콜 ID (예: 10010, 20020 등)
        *   `Timestamp` (8 bytes): 생성 시간
        *   `ParamCount` (2 bytes): 파라미터 개수
    *   **Body**: JSON 문자열 (UTF-8 인코딩)
*   **데이터 처리**:
    *   **수신**: 별도 스레드(`ReceiveLoop`)에서 스트림을 읽어 `Protocol` 객체로 역직렬화 후 큐(`_incomingProtocols`)에 저장.
    *   **처리**: 메인 스레드(`Update`)에서 큐를 비우며 핸들러 실행 (Unity API 접근 안전성 확보).

---

## 2. 주요 프로세스별 로직

### 2.1. 접속 및 인증 (Connection & Auth)

| 단계 | 클라이언트 로직 (`UnityGameClient`) | 예상 서버 로직 |
| :--- | :--- | :--- |
| **1. 연결** | `ConnectAsync()`로 TCP 연결 시도. 성공 시 `StartHeartbeat()` 시작. | TCP 연결 수락 (`AcceptTcpClient`). 세션 생성 및 관리. |
| **2. 하트비트** | 10초마다 `HEARTBEAT(10003)` 전송. | `HEARTBEAT` 수신 시 `HEARTBEAT_ACK(20003)` 응답. 일정 시간 미수신 시 연결 종료. |
| **3. 로그인** | (구현 필요) `REQUEST_LOGIN` 또는 `REQUEST_REGISTER_AUTO` 전송. | DB 조회/생성 후 유저 정보 반환. |

### 2.2. 룸 생성 및 입장 (Room & Lobby)

| 프로토콜 | 클라이언트 동작 | 예상 서버 동작 |
| :--- | :--- | :--- |
| **방 생성**<br>`REQUEST_CREATE_ROOM` | `CreateRoom(name, mapId)` 호출.<br>응답(`RESPONSE`) 성공 시 해당 방 ID로 자동 입장 시도. | 방 객체 생성, 맵 ID 설정.<br>성공 여부와 방 ID를 `RESPONSE`로 반환. |
| **방 입장**<br>`REQUEST_JOIN_ROOM` | `JoinRoom(roomId, slot)` 호출.<br>성공 시 `JoinRoomSuccess` 이벤트 발생 → `REQUEST_READY(true)` 자동 전송. | 해당 방/슬롯의 가용성 확인.<br>성공 시 방의 현재 상태(`RoomInfo`) 반환.<br>기존 유저들에게 `USER_JOINED` 브로드캐스트. |
| **준비**<br>`REQUEST_READY` | `RequestReady(bool)` 호출. | 해당 유저의 상태를 Ready로 변경.<br>모든 유저가 Ready 상태면 게임 시작 프로세스 진입. |

### 2.3. 게임 시작 및 초기화 (Game Initialization)

게임 시작은 **데이터 로딩(`GAME_SET`)**과 **실제 시작(`GAME_STARTED`)**의 2단계로 나뉩니다.

1.  **서버 (예상)**: 모든 유저 Ready 확인 → **`GAME_SET`** 프로토콜 전송 (맵, 행성, 플레이어 정보 포함).
2.  **클라이언트 (`HandleGameSet`)**:
    *   `GameStartData` 파싱 (MapInfo, PlanetInfo, Routes 등).
    *   `GameManager`가 맵, 행성, 함대 데이터를 초기화하고 시각화(`VisualizationManager`) 생성.
    *   로딩 완료 후 **`REQUEST_GAME_CL_READY`** 전송.
3.  **서버 (예상)**: 모든 클라이언트로부터 `REQUEST_GAME_CL_READY` 수신 확인 → **`GAME_STARTED`** 브로드캐스트.
4.  **클라이언트 (`HandleGameStarted`)**: 실제 게임 틱/타이머 시작.

### 2.4. 인게임 플레이 (In-Game)

서버가 게임 로직의 권한(Authority)을 가지며, 클라이언트는 명령을 보내고 결과를 시각화합니다.

#### A. 자원 및 상태 동기화
*   **서버 (예상)**: 주기적으로(또는 변경 시) 각 플레이어의 자원 상태를 계산하여 **`RESOURCES_UPDATED`** 전송.
*   **클라이언트**: `OnResourcesUpdated`에서 자원 UI 갱신 및 행성 소유권 색상 변경.

#### B. 함대 생산 (`ProduceFleet`)
*   **클라이언트**: `RequestProduceFleet(planetId, fleetType)` 전송 (`SUBMIT_COMMAND` 3010 사용).
*   **서버 (예상)**:
    *   요청한 `planetId`가 해당 유저 소유인지 확인.
    *   자원(`Minerals`, `Gas`) 및 인구수(`Supply`) 충분 여부 검증.
    *   성공 시 자원 차감 후 **`FLEET_SPAWNED`** 브로드캐스트.
*   **클라이언트**: `OnFleetSpawned` 수신 시 해당 위치에 함대 유닛 생성.

#### C. 함대 이동 (`MoveFleet`)
*   **클라이언트**: `RequestMoveFleet(fleetId, targetPlanetId)` 전송 (`SUBMIT_COMMAND` 3010 사용).
*   **서버 (예상)**:
    *   `fleetId`의 소유권 확인.
    *   현재 위치에서 `targetPlanetId`로 연결된 경로(`Route`)가 있는지 확인.
    *   이동 시작 처리 후 **`FLEET_MOVING`** 브로드캐스트 (또는 주기적 위치 동기화).
*   **클라이언트**: `OnFleetMoving` 수신 시 함대 이동 애니메이션 재생.

#### D. 전투 및 점령
*   **서버 (예상)**: 함대가 적 행성에 도착하면 전투 로직 수행.
    *   전투 결과에 따라 **`COMBAT_ENDED`** 전송.
    *   행성 주인이 바뀌면 **`PLANET_CONQUERED`** 전송.
*   **클라이언트**: 결과에 따라 폭발 이펙트, 소유권 색상 변경, 텍스트 표시.

---

## 3. 데이터 구조 (Data Structures)

클라이언트와 서버가 공유하는 핵심 데이터 구조입니다 (`GameDataStructures.cs`).

*   **`GameStartData`**: 게임 초기화에 필요한 모든 정보 (행성 배열, 경로 배열, 플레이어 목록).
*   **`PlanetData` / `PlanetInfoData`**: 행성 ID, 위치, 자원량, 소유자 정보.
*   **`MapRouteInfoData`**: 행성 간 연결 정보 (From -> To).
*   **`FleetSpawnData`**: 함대 생성 정보 (ID, 타입, 소유자, 위치).
*   **`FleetMoveData`**: 함대 이동 정보 (출발지, 목적지, 진행률).

---

## 4. 요약

현재 클라이언트는 **"멍청한 터미널(Dumb Terminal)"** 구조에 가깝게 설계되어 있습니다.
*   **클라이언트 역할**: 유저 입력을 서버로 전송(`SUBMIT_COMMAND`)하고, 서버가 보내주는 상태(`GAME_SET`, `FLEET_SPAWNED` 등)를 그대로 화면에 그립니다.
*   **서버 역할 (예상)**: 맵 데이터 관리, 자원 계산, 이동 검증, 전투 판정 등 모든 핵심 게임 로직을 수행하고 결과를 브로드캐스트합니다.

이 구조는 보안성이 높고 상태 동기화가 명확하지만, 네트워크 지연(Latency)에 민감할 수 있어 향후 클라이언트 측 예측(Prediction) 로직이 추가될 가능성이 있습니다.
