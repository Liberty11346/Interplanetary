using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using CommonLib;
using CommonLib.TableData;
using System.Linq;
using UnityEngine.InputSystem;

/// <summary>
/// 게임 진행 및 서버 통신 담당 (RoomManager 기반 추출)
/// </summary>
public class GamePlayManager
{
    // --- 싱글톤 ---
    private static GamePlayManager _instance;
    private static readonly object _lock = new object();

    public static GamePlayManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new GamePlayManager();
                    }
                }
            }
            return _instance;
        }
    }

    // --- 네트워크 및 공통 이벤트 ---
    private ClientServerHandler networkClient;
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;
    public event Action<string> OnError;
    public event Action<string> OnStatusMessage;

    // --- 게임 플레이 관련 이벤트들 ---
    public System.Action<GameStartData> GameStarted;
    public System.Action<GameState, long> GameStateReceived;  // ⭐ 새 이벤트: GameState 수신
    public System.Action<ResourceUpdate> ResourcesUpdated;
    public System.Action<FleetSpawnData> FleetSpawned;
    public System.Action<FleetMoveData> FleetMoving;
    public System.Action<CombatResult> CombatEnded;
    public System.Action<PlanetConquerData> PlanetConquered;
    public System.Action<GameEndData> GameEnded;
    public System.Action<ChatMessage> ChatMessageReceived;

    // --- 현재 상태 ---
    private UserInfo? currentUser = null;
    private GameState _previousGameState = null;
    private long _currentTick = 0;
    private string _sessionId = "";
    private int _myPlayerId = -1;

    // 게임 시작 데이터 (승리 조건 판정에 필요)
    private GameStartData? _gameStartData = null;

    public void SetGameStartData(GameStartData data)
    {
        _gameStartData = data;
    }

    // --- 초기화 및 생명주기 ---
    public async Task Initialize(UserInfo user, string sessionId = "")
    {
        currentUser = user;
        _sessionId = sessionId;

        if (!string.IsNullOrEmpty(sessionId))
        {
            Debug.Log($"[GamePlayManager] SessionId 설정: {sessionId}");
        }

        networkClient = ClientServerHandler.Instance;
        if (networkClient == null)
            return;

        if (!isInitialized)
        {
            RegisterNetworkHandlers();
            isInitialized = true;
            EmitStatusMessage("GamePlayManager 초기화 완료");
        }

        await Task.CompletedTask;
    }

    private void RegisterNetworkHandlers()
    {
        // ✅ 이벤트 구독 방식으로 변경 (ClientServerHandler의 기본 핸들러 덮어쓰기 방지)
        networkClient.OnChatMessageEvent += HandleChatBroadcast;

        // 게임 전용 핸들러는 직접 등록 (충돌 없음)
        RegisterHandler(ProtocolType.GAME_STATE, HandleReceiveGameState);
        RegisterHandler(ProtocolType.GAME_STARTED, HandleGameStarted);
        RegisterHandler(ProtocolType.GAME_ENDED, HandleGameEnded);
    }

    public void Cleanup()
    {
        // 이벤트 구독 해제
        if (networkClient != null)
        {
            networkClient.OnChatMessageEvent -= HandleChatBroadcast;
        }

        // 상태 초기화
        _previousGameState = null;
        _gameStartData = null;
        _currentTick = 0;
        currentUser = null;
        EmitStatusMessage("GamePlayManager 정리 완료");
    }

    public void Reset()
    {
        // 데이터 리셋 (Cleanup과 동일하지만 메시지 없음)
        _previousGameState = null;
        _gameStartData = null;
        _currentTick = 0;
    }

    // --- 공통 유틸리티 ---
    private void EmitStatusMessage(string message)
    {
        Debug.Log($"[GamePlayManager] {message}");
        OnStatusMessage?.Invoke(message);
    }

    private void EmitError(string error)
    {
        Debug.LogError($"[GamePlayManager] {error}");
        OnError?.Invoke(error);
    }

    private bool IsNetworkReady()
    {
        return networkClient != null && networkClient.IsConnected;
    }

    private void ValidateNetworkConnection()
    {
        if (!IsNetworkReady())
        {
            throw new InvalidOperationException("서버에 연결되지 않았습니다");
        }
    }

    private void RegisterHandler(int protocolType, ProtocolHandler.ProtocolHandlerDelegate handler)
    {
        if (networkClient != null)
        {
            networkClient.RegisterHandler(protocolType, handler);
            Debug.Log($"[GamePlayManager] 핸들러 등록됨: {protocolType}");
        }
        else
        {
            Debug.LogError($"[GamePlayManager] 네트워크 클라이언트가 없어 핸들러 등록 실패: {protocolType}");
        }
    }

    private async Task<NetworkResponse> SafeSendAsync(Protocol protocol, string operationName = "")
    {
        try
        {
            if (networkClient == null)
            {
                throw new InvalidOperationException("네트워크 클라이언트가 초기화되지 않음");
            }

            if (!string.IsNullOrEmpty(operationName))
            {
                Debug.Log($"[GamePlayManager] {operationName} 요청 중...");
            }

            NetworkResponse response = await networkClient.AsyncSend(protocol);

            if (response.isSuccess)
            {
                if (!string.IsNullOrEmpty(operationName))
                {
                    Debug.Log($"[GamePlayManager] {operationName} 성공");
                }
            }
            else
            {
                string errorMsg = $"{operationName} 실패: {response.resultCode}";
                Debug.LogError($"[GamePlayManager] {errorMsg}");
                OnError?.Invoke(errorMsg);
                response.ShowResultCode();
            }

            return response;
        }
        catch (Exception e)
        {
            string errorMsg = $"{operationName} 오류: {e.Message}";
            Debug.LogError($"[GamePlayManager] {errorMsg}");
            OnError?.Invoke(errorMsg);
            throw;
        }
    }

    // 안전한 값 파싱 유틸리티
    private int GetIntValue(Dictionary<string, object> data, string key, int defaultValue = 0)
    {
        if (data != null && data.TryGetValue(key, out object value))
        {
            if (value is int intValue) return intValue;
            if (int.TryParse(value.ToString(), out int parsedValue)) return parsedValue;
        }
        return defaultValue;
    }

    private string GetStringValue(Dictionary<string, object> data, string key, string defaultValue = "")
    {
        if (data != null && data.TryGetValue(key, out object value))
        {
            return value?.ToString() ?? defaultValue;
        }
        return defaultValue;
    }

    private bool GetBoolValue(Dictionary<string, object> data, string key, bool defaultValue = false)
    {
        if (data != null && data.TryGetValue(key, out object value))
        {
            if (value is bool boolValue) return boolValue;
            if (bool.TryParse(value.ToString(), out bool parsedValue)) return parsedValue;
        }
        return defaultValue;
    }

    private async Task HandleReceiveGameState(Protocol protocol)
    {
        // record 타입으로 역직렬화
        var gameState = protocol.GetObject<GameState>("gameState");
        var serverTick = protocol.GetParam<long>("serverTick");

        if (gameState == null)
        {
            EmitError("게임 상태 데이터가 null입니다");
            return;
        }

        _currentTick = serverTick;

        // ⭐ GameState 수신 이벤트 발생 (GameManager가 상태 동기화에 사용)
        GameStateReceived?.Invoke(gameState, serverTick);

        // 이전 상태와 비교하여 변화 감지 (개별 이벤트 발생)
        if (_previousGameState != null)
        {
            DetectAndFireEvents(_previousGameState, gameState);
        }

        // 현재 상태 저장
        _previousGameState = DeepCopyGameState(gameState);

        await Task.CompletedTask;
    }

    /// <summary>
    /// 이전 상태와 현재 상태를 비교하여 변화를 감지하고 이벤트 발생
    /// </summary>
    private void DetectAndFireEvents(GameState previous, GameState current)
    {
        // 1. 게임 종료 체크 (가이드 명세: state 필드로 승자 판정)
        if ((previous.state == 0 || previous.state == 2) &&
            (current.state == 1 || current.state == -1 || current.state == 3))
        {
            // 게임 종료 상태로 전환됨
            int winnerId = DetermineWinnerFromState(current.state);
            GameEnded?.Invoke(new GameEndData
            {
                WinnerId = winnerId,
                Reason = GetEndReason(current.state),
                GameDuration = _currentTick * 50 // 틱당 50ms
            });
            EmitStatusMessage($"게임 종료! 승자: Player {winnerId}, 사유: {GetEndReason(current.state)}");
        }

        // 2. 행성 소유권 변경 감지
        DetectPlanetOwnershipChanges(previous, current);

        // 3. 플레이어 자원 변경 감지
        DetectResourceChanges(previous, current);

        // 4. 함대 상태 변경 감지
        DetectFleetChanges(previous, current);
    }

    /// <summary>
    /// 가이드 명세에 따른 승자 판정
    /// state == 1: Player 0 승리
    /// state == -1: Player 1 승리
    /// state == 3: 무승부 또는 기타
    /// </summary>
    private int DetermineWinnerFromState(int state)
    {
        if (state == 1) return 0;       // Player 0 승리
        if (state == -1) return 1;      // Player 1 승리
        return -1;                      // 무승부
    }

    /// <summary>
    /// 종료 사유 반환
    /// </summary>
    private string GetEndReason(int state)
    {
        if (state == 1) return "Player 0 승리";
        if (state == -1) return "Player 1 승리";
        if (state == 3) return "게임 종료";
        return "알 수 없는 사유";
    }

    /// <summary>
    /// 행성 소유권 변경 감지
    /// </summary>
    private void DetectPlanetOwnershipChanges(GameState previous, GameState current)
    {
        if (previous.planets == null || current.planets == null)
            return;

        int minLength = Math.Min(previous.planets.Length, current.planets.Length);

        for (int i = 0; i < minLength; i++)
        {
            var prevPlanet = previous.planets[i];
            var currPlanet = current.planets[i];

            if (prevPlanet.owner != currPlanet.owner)
            {
                // 행성 소유권 변경됨
                PlanetConquered?.Invoke(new PlanetConquerData
                {
                    PlanetId = i,
                    NewOwnerId = currPlanet.owner,
                    PreviousOwnerId = prevPlanet.owner
                });

                Debug.Log($"[GamePlayManager] 행성 {i} 점령: Player {prevPlanet.owner} → Player {currPlanet.owner}");

                // 홈월드 점령 여부 확인
                CheckHomeworldConquest(i, currPlanet.owner);
            }
        }
    }

    /// <summary>
    /// 홈월드 점령 로그 출력 (승리 조건은 서버의 GameState.state에서 판정)
    /// </summary>
    private void CheckHomeworldConquest(int planetIndex, int newOwnerId)
    {
        if (_gameStartData == null || !_gameStartData.HasValue)
            return;

        // 점령된 행성이 홈월드인지 확인
        var planets = _gameStartData.Value.Planets;
        if (planetIndex < 0 || planetIndex >= planets.Length)
            return;

        int conqueredPlanetId = planets[planetIndex].PlanetId;
        var mapInfo = _gameStartData.Value.MapInfo;

        // Player 1의 홈월드가 점령되었는지 확인
        if (conqueredPlanetId == mapInfo.Player1_HomeID && newOwnerId == 1)
        {
            EmitStatusMessage($"🎉 Player 2가 Player 1의 홈월드(ID: {conqueredPlanetId})를 점령했습니다!");
        }
        // Player 2의 홈월드가 점령되었는지 확인
        else if (conqueredPlanetId == mapInfo.Player2_HomeID && newOwnerId == 0)
        {
            EmitStatusMessage($"🎉 Player 1이 Player 2의 홈월드(ID: {conqueredPlanetId})를 점령했습니다!");
        }

        // 실제 승리 조건은 서버가 GameState.state 필드로 판정하여 전송
    }

    /// <summary>
    /// 플레이어 자원 변경 감지
    /// </summary>
    private void DetectResourceChanges(GameState previous, GameState current)
    {
        if (previous.players == null || current.players == null)
            return;

        int minLength = Math.Min(previous.players.Length, current.players.Length);

        for (int i = 0; i < minLength; i++)
        {
            var prevPlayer = previous.players[i];
            var currPlayer = current.players[i];

            // 자원이 변경되었는지 확인
            if (prevPlayer.Mineral != currPlayer.Mineral ||
                prevPlayer.Gas != currPlayer.Gas ||
                prevPlayer.Supply != currPlayer.Supply)
            {
                ResourcesUpdated?.Invoke(new ResourceUpdate
                {
                    PlayerId = currPlayer.id,
                    Minerals = currPlayer.Mineral,
                    Gas = currPlayer.Gas,
                    CurrentSupply = currPlayer.Supply,
                    MaxSupply = currPlayer.Supply // MaxSupply는 별도 필드가 필요할 수 있음
                });
            }
        }
    }

    /// <summary>
    /// 함대 상태 변경 감지
    /// </summary>
    private void DetectFleetChanges(GameState previous, GameState current)
    {
        if (previous.players == null || current.players == null)
            return;

        int minLength = Math.Min(previous.players.Length, current.players.Length);

        for (int playerId = 0; playerId < minLength; playerId++)
        {
            var prevPlayer = previous.players[playerId];
            var currPlayer = current.players[playerId];

            if (prevPlayer.fleets == null || currPlayer.fleets == null)
                continue;

            // 새로운 함대 생성 감지 (함대 수 증가)
            if (currPlayer.fleets.Length > prevPlayer.fleets.Length)
            {
                for (int i = prevPlayer.fleets.Length; i < currPlayer.fleets.Length; i++)
                {
                    var newFleet = currPlayer.fleets[i];
                    FleetSpawned?.Invoke(new FleetSpawnData
                    {
                        FleetId = i,
                        FleetType = 0, // FleetType은 별도로 관리 필요
                        OwnerId = currPlayer.id,
                        PlanetId = -1 // 위치로부터 행성 ID 추론 필요
                    });

                    Debug.Log($"[GamePlayManager] 함대 생성: Player {currPlayer.id}, Fleet {i}");
                }
            }

            // 함대 이동 감지
            int fleetCount = Math.Min(prevPlayer.fleets.Length, currPlayer.fleets.Length);
            for (int fleetId = 0; fleetId < fleetCount; fleetId++)
            {
                var prevFleet = prevPlayer.fleets[fleetId];
                var currFleet = currPlayer.fleets[fleetId];

                // 이동 중인 함대 (state == 2)
                if (prevFleet.state != 2 && currFleet.state == 2)
                {
                    FleetMoving?.Invoke(new FleetMoveData
                    {
                        FleetId = fleetId,
                        FromPlanetId = -1, // 위치로부터 행성 ID 추론 필요
                        ToPlanetId = -1,   // target 위치로부터 행성 ID 추론 필요
                        Progress = 0f
                    });

                    Debug.Log($"[GamePlayManager] 함대 이동 시작: Player {currPlayer.id}, Fleet {fleetId}");
                }

                // 전투 중인 함대 (state == 1)
                if (prevFleet.state != 1 && currFleet.state == 1)
                {
                    Debug.Log($"[GamePlayManager] 전투 시작: Player {currPlayer.id}, Fleet {fleetId}");
                    // CombatEnded는 전투가 끝날 때 발생하므로 여기서는 로그만
                }

                // HP 변화로 전투 종료 감지 (간접적)
                if (prevFleet.state == 1 && currFleet.state == 0 && prevFleet.HP != currFleet.HP)
                {
                    // 전투 종료 (승리 또는 패배)
                    CombatEnded?.Invoke(new CombatResult
                    {
                        AttackerId = currPlayer.id,
                        DefenderId = -1, // 상대 플레이어 ID 추론 필요
                        PlanetId = -1,   // 전투 위치 추론 필요
                        AttackerWon = currFleet.HP > 0,
                        AttackerLosses = (int)Math.Max(0, prevFleet.HP - currFleet.HP),
                        DefenderLosses = 0
                    });

                    Debug.Log($"[GamePlayManager] 전투 종료: Player {currPlayer.id}, Fleet {fleetId}");
                }
            }
        }
    }


    /// <summary>
    /// GameState 깊은 복사
    /// </summary>
    private GameState DeepCopyGameState(GameState original)
    {
        if (original == null)
            return null;

        var copy = new GameState
        {
            state = original.state,
            players = original.players != null ? new GameState.Player[original.players.Length] : null,
            planets = original.planets != null ? new GameState.Planet[original.planets.Length] : null
        };

        // Players 복사
        if (original.players != null)
        {
            for (int i = 0; i < original.players.Length; i++)
            {
                var origPlayer = original.players[i];
                copy.players[i] = new GameState.Player
                {
                    id = origPlayer.id,
                    Gas = origPlayer.Gas,
                    Mineral = origPlayer.Mineral,
                    Supply = origPlayer.Supply,
                    fleets = origPlayer.fleets != null ? new GameState.FleetInfo[origPlayer.fleets.Length] : null
                };

                // Fleets 복사
                if (origPlayer.fleets != null)
                {
                    for (int j = 0; j < origPlayer.fleets.Length; j++)
                    {
                        var origFleet = origPlayer.fleets[j];
                        copy.players[i].fleets[j] = new GameState.FleetInfo
                        {
                            fleetId = origFleet.fleetId,
                            fleetType = origFleet.fleetType,
                            ownerId = origFleet.ownerId,
                            position = origFleet.position,
                            state = origFleet.state,
                            HP = origFleet.HP,
                            maxHP = origFleet.maxHP,
                            target = origFleet.target
                        };
                    }
                }
            }
        }

        // Planets 복사
        if (original.planets != null)
        {
            for (int i = 0; i < original.planets.Length; i++)
            {
                copy.planets[i] = new GameState.Planet
                {
                    planetId = original.planets[i].planetId,
                    owner = original.planets[i].owner,
                    conquestProgress = original.planets[i].conquestProgress
                };
            }
        }

        return copy;
    }

    // --- 네트워크 이벤트 핸들러들 ---

    /// <summary>
    /// 게임 시작 브로드캐스트 처리 (GAME_STARTED)
    /// 가이드 명세: GAME_SET 직후 수신, 실제 게임 시작 신호
    /// </summary>
    private async Task HandleGameStarted(Protocol _)
    {
        EmitStatusMessage("게임이 시작되었습니다!");

        // 게임 시작 이벤트 발생 (GameManager가 구독하여 처리)
        if (_gameStartData.HasValue)
        {
            GameStarted?.Invoke(_gameStartData.Value);
            Debug.Log("[GamePlayManager] GAME_STARTED - 게임 시작 이벤트 발생");
        }
        else
        {
            Debug.LogWarning("[GamePlayManager] GAME_STARTED 수신했지만 GameStartData가 없습니다");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 게임 종료 브로드캐스트 처리
    /// </summary>
    private async Task HandleGameEnded(Protocol protocol)
    {
        try
        {
            int winnerId = protocol.GetParam<int>("winnerId");
            string reason = protocol.GetParam<string>("reason") ?? "게임 종료";
            long gameDuration = protocol.GetParam<long>("gameDuration");

            EmitStatusMessage($"게임 종료! 승자: Player {winnerId}, 사유: {reason}");

            // 게임 종료 이벤트 발생
            GameEnded?.Invoke(new GameEndData
            {
                WinnerId = winnerId,
                Reason = reason,
                GameDuration = gameDuration
            });
        }
        catch (Exception ex)
        {
            EmitError($"게임 종료 처리 오류: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 채팅 메시지 브로드캐스트 처리
    /// </summary>
    private void HandleChatBroadcast(Protocol protocol)
    {
        try
        {
            var chatMessage = protocol.GetStruct<ChatMessage>("message");
            if (chatMessage.MessageType == 1) // 1 = INGAME
            {
                EmitStatusMessage($"[인게임 채팅] {chatMessage}");
                ChatMessageReceived?.Invoke(chatMessage);
            }
        }
        catch (Exception ex)
        {
            EmitError($"채팅 메시지 처리 오류: {ex.Message}");
        }
    }

    // --- 게임 명령 전송 메서드 ---

    /// <summary>
    /// 함대 생산 요청
    /// 가이드 명세: target = fleetTypeId만 전송 (서버가 플레이어의 모성에서 자동 생산)
    /// </summary>
    public async Task<bool> RequestProduceFleet(int fleetType)
    {
        ValidateNetworkConnection();

        var protocol = new Protocol(ProtocolType.SUBMIT_COMMAND)
            .AddParam("tick", 0L)  // 서버가 자동 계산
            .AddParam("target", fleetType);

        var response = await SafeSendAsync(protocol, $"함대 생산 요청 (타입: {fleetType})");
        return response.isSuccess;
    }

    /// <summary>
    /// 함대 이동 요청
    /// 가이드 명세: tick, target_fleet, target_planet만 전송
    /// </summary>
    public async Task<bool> RequestMoveFleet(int fleetId, int targetPlanetId)
    {
        ValidateNetworkConnection();

        var protocol = new Protocol(ProtocolType.SUBMIT_COMMAND)
            .AddParam("tick", 0L)  // 서버가 자동 계산
            .AddParam("target_fleet", fleetId)
            .AddParam("target_planet", targetPlanetId);

        var response = await SafeSendAsync(protocol, $"함대 이동 요청 (함대: {fleetId} → 행성: {targetPlanetId})");
        return response.isSuccess;
    }

    /// <summary>
    /// 채팅 메시지 전송
    /// </summary>
    public async Task<bool> SendChatMessage(string message)
    {
        ValidateNetworkConnection();

        var protocol = new Protocol(ProtocolType.CHAT_MESSAGE)
            .AddParam("message", message);

        var response = await SafeSendAsync(protocol, "채팅 메시지 전송");
        return response.isSuccess;
    }

    /// <summary>
    /// 방 생성 요청
    /// </summary>
    public async Task<bool> CreateRoom(string roomName, int mapId, bool isPrivate = false)
    {
        ValidateNetworkConnection();

        var protocol = new Protocol(ProtocolType.REQUEST_CREATE_ROOM)
            .AddParam("roomName", roomName)
            .AddParam("mapId", mapId)
            .AddParam("isPrivate", isPrivate);

        var response = await SafeSendAsync(protocol, $"방 생성 ({roomName}, 맵 ID: {mapId})");
        return response.isSuccess;
    }

    /// <summary>
    /// 방 입장 요청
    /// </summary>
    public async Task<bool> JoinRoom(string roomId, int slot, int userId = 0)
    {
         return await RoomManager.Instance.RequestJoinRoomAsync(roomId, slot);
    }

    /// <summary>
    /// 방 나가기 요청
    /// </summary>
    public async Task<bool> LeaveRoom()
    {
        ValidateNetworkConnection();

        var protocol = new Protocol(ProtocolType.REQUEST_LEFT_ROOM);

        var response = await SafeSendAsync(protocol, "방 나가기");
        return response.isSuccess;
    }

    /// <summary>
    /// 준비 상태 변경 요청
    /// </summary>
    public async Task<bool> RequestReady(bool isReady)
    {
        ValidateNetworkConnection();

        var protocol = new Protocol(ProtocolType.REQUEST_READY)
            .AddParam("isReady", isReady);

        var response = await SafeSendAsync(protocol, $"준비 상태 변경 ({isReady})");
        return response.isSuccess;
    }

    /// <summary>
    /// 게임 클라이언트 준비 완료 알림
    /// </summary>
    public async Task<bool> RequestGameClientReady()
    {
        ValidateNetworkConnection();

        var protocol = new Protocol(ProtocolType.REQUEST_GAME_CL_READY);

        var response = await SafeSendAsync(protocol, "게임 클라이언트 준비 완료");
        return response.isSuccess;
    }

    // --- 공개 속성 ---

    /// <summary>
    /// 현재 게임 틱
    /// </summary>
    public long CurrentTick => _currentTick;

    /// <summary>
    /// 세션 ID
    /// </summary>
    public string SessionId => _sessionId;

    /// <summary>
    /// 내 플레이어 ID
    /// </summary>
    public int MyPlayerId => _myPlayerId;

    /// <summary>
    /// 게임 시작 데이터 (읽기 전용)
    /// </summary>
    public GameStartData? GameStartData => _gameStartData;

    /// <summary>
    /// 현재 게임 상태 (읽기 전용)
    /// </summary>
    public GameState PreviousGameState => _previousGameState;

    /// <summary>
    /// 현재 사용자 정보
    /// </summary>
    public UserInfo? CurrentUser => currentUser;

    // --- UI 호출용 간단 래퍼 메서드들 ---

    /// <summary>
    /// 함대 생산 (UI 호출용)
    /// </summary>
    public async void ProduceFleet(int fleetType)
    {
        await RequestProduceFleet(fleetType);
    }

    /// <summary>
    /// 함대 이동 (UI 호출용)
    /// </summary>
    public async void MoveFleet(int fleetId, int targetPlanetId)
    {
        await RequestMoveFleet(fleetId, targetPlanetId);
    }

    /// <summary>
    /// 채팅 메시지 전송 (UI 호출용)
    /// </summary>
    public async void SendChat(string message)
    {
        await SendChatMessage(message);
    }

    // --- 게임 상태 정의 ---


    /// <summary>
    /// 게임 상태 클래스. 매 틱마다 클라 전송.
    /// </summary>
    public class GameState
    {
        public class Player
        {
            public int id = -1;
            public FleetInfo[] fleets;
            public int Gas;
            public int Mineral;
            public int Supply;
        }

        public class FleetInfo
        {
            public long fleetId;        // 함대 고유 ID (추적용)
            public int fleetType;       // 함대 타입 (시각화용)
            public int ownerId;         // 소유자 ID
            public CommonLib.Vector2 position;
            public int state = 0;   // 0 = Idle, 1 = battle. 2 = move
            public float HP = 0;
            public float maxHP = 0;     // 최대 HP (HP바 표시용)
            public CommonLib.Vector2 target;    // idle일때는 무시
        }

        public class Planet
        {
            public int planetId;        // 행성 ID (추적용)
            public int owner = -1;
            public float conquestProgress = 0;
        }

        public int state = 0; // 0 = 준비. 1 = 진행중, 2 = 종료

        public Player[] players;
        public Planet[] planets;
    }
}
