# 게임 서버 네트워크 프로토콜

## 기본 정보
- 통신 방식: **TCP** (UDP 는 현재 사용 안 함)
- 인코딩: Binary Header (18 bytes) + JSON Body (UTF-8)
- 버전: 1.0.0

---

## 프로토콜 목록

### 클라이언트 -> 서버 (C→S)

| ID | 이름 | 설명 |
|----|------|------|
| 10000 | REQUEST_LOGIN | 로그인 요청 |
| 10001 | REQUEST_LOGOUT | 로그아웃 요청 |
| 10002 | CHAT_MESSAGE | 메시지 전송 |
| 10003 | HEARTBEAT | 하트비트 (연결 유지 확인) |
| 10004 | REQUEST_TABLEDATA | 테이블 데이터 요청 |
| 10005 | REQUEST_REGISTER | 회원가입 요청 |
| 10006 | REQUEST_REGISTER_AUTO | 자동 회원가입 요청 (게스트) |
| 10010 | REQUEST_JOIN_LOBBY | 로비 접속 요청 |
| 10011 | REFRESH_LOBBY | 로비 새로고침 요청 |
| 10012 | REQUEST_CREATE_ROOM | 방 생성 요청 |
| 10013 | REQUEST_JOIN_ROOM | 방 입장 요청 |
| 10014 | REQUEST_READY | 게임 레디 |
| 10015 | REQUEST_LEFT_ROOM | 방 퇴장 요청 |
| **30100** | **SUBMIT_COMMAND** | **명령 제출 (ProduceFleet, MoveFleet)** |

### 서버 -> 클라이언트 (S→C)

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

---

## 프로토콜 상세

### REQUEST_LOGIN
- **ID**: 10000
- **파라미터**:
  - `username`: string
  - `password`: string
- **응답 파라미터**:
  - `sessionId`: string

### REQUEST_LOGOUT
- **ID**: 10001
- **파라미터**: 없음
- **응답 파라미터**: 없음

### CHAT_MESSAGE
- **ID**: 10002
- **파라미터**:
  - `message`: string
- **응답**: `BRODCAST_CHAT_MESSAGE(20002)` 브로드캐스트

### HEARTBEAT
- **ID**: 10003
- **파라미터**:
  - `timestamp`: long
- **응답**: `HEARTBEAT_ACK(20003)`

### REQUEST_TABLEDATA
- **ID**: 10004
- **파라미터**:
  - `table_name`: string
- **응답 파라미터**:
  - `tableRowCount`: int
  - `tableRows`: string[]
  - `tableTypes`: type[]
  - `tableData`: object[][]

### REQUEST_REGISTER
- **ID**: 10005
- **파라미터**:
  - `username`: string
  - `password`: string
- **응답 파라미터**:
  - `message`: string

### REQUEST_REGISTER_AUTO
- **ID**: 10006
- **파라미터**: 없음
- **응답 파라미터**:
  - `username`: string
  - `password`: string
  - `message`: string

### REQUEST_JOIN_LOBBY
- **ID**: 10010
- **파라미터**:
  - `Page`: int (기본값 0 입력 시 전체 전송)
- **응답 파라미터**:
  - `roomCount`: int
  - `page`: int
  - `roomList`: RoomInfo[]

### REFRESH_LOBBY
- **ID**: 10011
- **파라미터**: 없음

### REQUEST_CREATE_ROOM
- **ID**: 10012
- **파라미터**:
  - `roomName`: string
  - `mapId`: int
  - `isPrivate`: bool
- **응답 파라미터**:
  - `roomId`: string
  - `slot`: int
  - `roomList`: RoomInfo[]

### REQUEST_JOIN_ROOM
- **ID**: 10013
- **파라미터**:
  - `userId`: int
  - `roomId`: string
  - `slot`: int
- **응답 파라미터**:
  - `roominfo`: RoomInfo
  - `chatChannelId`: string

### REQUEST_READY
- **ID**: 10014
- **파라미터**:
  - `isReady`: bool
- **응답**: 성공/실패 `RESPONSE(20000)`

### REQUEST_LEFT_ROOM
- **ID**: 10015
- **파라미터**: 없음
- **응답**: 성공/실패 `RESPONSE(20000)`

### SUBMIT_COMMAND (명령 제출)
- **ID**: 30100
- **파라미터**:
  - `commandType`: GameCommandType (ProduceFleet, MoveFleet)
  - `tick`: long (클라이언트 현재 틱)
  - **ProduceFleet**: `target`: int (함대 타입)
  - **MoveFleet**: `target_fleet`: long, `target_planet`: int
- **응답**: `RESPONSE(20000)` (성공/실패)

---

## 데이터 구조

### UserData
- `userId`: int
- `username`: string

### RoomInfo
- `RoomId`: string
- `playerCount`: int
- `MaxPlayers`: int
- `roomName`: string
- `roomState`: RoomState
- `mapId`: int

### GameState
- `state`: int (0=준비, 1=진행중, 2=종료 — 주석 기준)
- `players`: Player[]
- `planets`: Planet[]

### Player
- `id`: int
- `fleets`: FleetInfo[]

### FleetInfo
- `position`: Vector2
- `state`: int (0=Idle, 1=Battle, 2=Move)
- `HP`: float
- `target`: Vector2

### Planet
- `owner`: int
- `conquestProgress`: float

---

## 열거형 정의

### FleetType
- Scout
- Fighter
- Cruiser
- BattleShip

### RoomState
- open
- full
- ingame
- disabled
- closed
- error

### GameCommandType
- ProduceFleet
- MoveFleet

---

## 상태 코드

| 코드 | 설명 |
|------|------|
| 0 | 성공 |
| 1 | 일반 오류 |
| 2 | 인증 실패 |
| 3 | 권한 부족 |
| 4 | 리소스 없음 |
| 5 | 서버 오류 |

---

## 헤더 포맷 (18 bytes)

| 필드 | 크기 | 타입 | 설명 |
|------|------|------|------|
| TotalSize | 4 bytes | int | 헤더 포함 전체 패킷 크기 |
| Type | 4 bytes | int | 프로토콜 ID |
| Timestamp | 8 bytes | long | 생성 시간 |
| ParamCount | 2 bytes | short | 파라미터 개수 |

**바디**: UTF-8 인코딩 JSON 문자열.
