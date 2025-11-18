using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using CommonLib;
using CommonLib.Commands;
using Newtonsoft.Json;
using ProtocolType = CommonLib.ProtocolType;
using System.Data;

/// <summary>
/// 네트워크 메시지 처리 클라이언트
/// </summary>
public class UnityGameClient : MonoBehaviour
{
    [Header("Connection Settings")]
    public string serverAddress = "localhost";
    public int serverPort = 7777;
    public bool autoConnect = true;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private TcpClient _tcpClient;
    private NetworkStream _stream;
    private bool _isConnected = false;
    private bool _isReceiving = false;
    private string _sessionId = "";
    private int _myPlayerId = -1;
    private long _currentTick = 0;
    private string _lastConnectError = "";


    // C# Eventsㅜ 
    public event Action<bool, string> ConnectionChanged;
    public event Action<string, int> JoinRoomSuccess;
    public event Action<string> JoinRoomFailed;
    public event Action<ChatMessage> ChatReceived;
    public event Action<string, int> UserJoined;
    public event Action<string, int> UserLeft;
    public event Action<string> ErrorOccurred;

    // 명령 실패 이벤트 (잘못된 경로, 자원 부족 등)
    public event Action<CommandFailureData> CommandFailed;

    // 게임 이벤트
    public event Action<GameStartData> GameStarted;
    public event Action<ResourceUpdate> ResourcesUpdated;
    public event Action<FleetSpawnData> FleetSpawned;
    public event Action<FleetMoveData> FleetMoving;
    public event Action<CombatResult> CombatEnded;
    public event Action<PlanetConquerData> PlanetConquered;
    public event Action<GameEndData> GameEnded;

    // 프로토콜 처리를 위한 큐
    private Queue<Protocol> _incomingProtocols = new Queue<Protocol>();
    private readonly object _protocolLock = new object();

    // 하트비트
    private Coroutine _heartbeatCoroutine;

    // Properties
    public bool IsConnected => _isConnected;
    public string SessionId => _sessionId;
    public int MyPlayerId => _myPlayerId;

    private void Start()
    {
        if (autoConnect)
        {
            StartCoroutine(ConnectToServer());
        }
    }

    private void Update()
    {
        // 메인 스레드에서 프로토콜 처리
        ProcessIncomingProtocols();
    }

    private void OnDestroy()
    {
        Disconnect();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            StopHeartbeat();
        }
        else
        {
            if (_isConnected)
            {
                StartHeartbeat();
            }
        }
    }

    public IEnumerator ConnectToServer()
    {
        if (_isConnected)
        {
            LogDebug("Already connected to server");
            yield break;
        }

        LogDebug($"Connecting to {serverAddress}:{serverPort}...");

        var connectTask = ConnectAsync();
        yield return new WaitUntil(() => connectTask.IsCompleted);

        if (connectTask.Result)
        {
            LogDebug("Connected successfully!");
            ConnectionChanged?.Invoke(true, "Connected");

            StartCoroutine(ReceiveLoop());
            StartHeartbeat();
            // 자동 룸 입장은 비활성화합니다. 필요 시 JoinRoom(roomId, slot) 호출하세요.
        }
        else
        {
            LogDebug($"Connection failed! Error: {_lastConnectError}");
            ConnectionChanged?.Invoke(false, $"Connection failed: {_lastConnectError}");
        }
    }

    private async Task<bool> ConnectAsync()
    {
        try
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(serverAddress, serverPort);
            _stream = _tcpClient.GetStream();
            _isConnected = true;
            return true;
        }
        catch (Exception ex)
        {
            _lastConnectError = ex.Message;
            LogDebug($"Connection error: {_lastConnectError}");
            return false;
        }
    }

    public void Disconnect()
    {
        _isConnected = false;
        _isReceiving = false;

        StopHeartbeat();

        try
        {
            _stream?.Close();
            _tcpClient?.Close();
        }
        catch (Exception ex)
        {
            LogDebug($"Disconnect error: {ex.Message}");
        }

        ConnectionChanged?.Invoke(false, "Disconnected");
        LogDebug("Disconnected from server");
    }

    // ===== Debug Handlers =====
    [ContextMenu("Debug/Connect to My Server")]
    public void DebugConnectToMyServer()
    {
        DebugConnect("127.0.0.1", 7777);
    }

    public void DebugConnect(string address = "127.0.0.1", int port = 7777)
    {
        serverAddress = address;
        serverPort = port;
        autoConnect = false;

        LogDebug($"[Debug] Connecting to {serverAddress}:{serverPort}...");
        StartCoroutine(ConnectToServer());
    }

    [ContextMenu("Debug/Disconnect")]
    public void DebugDisconnect()
    {
        LogDebug("[Debug] Force disconnect.");
        Disconnect();
    }

    private IEnumerator ReceiveLoop()
    {
        _isReceiving = true;

        while (_isConnected && _isReceiving)
        {
            if (_stream != null && _stream.DataAvailable)
            {
                try
                {
                    // 프로토콜 크기 읽기
                    byte[] sizeBuffer = new byte[4];
                    int bytesRead = _stream.Read(sizeBuffer, 0, 4);
                    if (bytesRead != 4) break;

                    int payloadSize = BitConverter.ToInt32(sizeBuffer, 0);

                    // 전체 메시지 읽기: [4바이트 길이] + [payload]
                    byte[] messageBuffer = new byte[payloadSize + 4];
                    Array.Copy(sizeBuffer, 0, messageBuffer, 0, 4);

                    int totalRead = 0;
                    while (totalRead < payloadSize)
                    {
                        bytesRead = _stream.Read(messageBuffer, 4 + totalRead, payloadSize - totalRead);
                        if (bytesRead == 0) break;
                        totalRead += bytesRead;
                    }

                    // 프로토콜 역직렬화
                    Protocol protocol = Protocol.Deserialize(messageBuffer);

                    // 메인 스레드에서 처리하기 위해 큐에 추가
                    lock (_protocolLock)
                    {
                        _incomingProtocols.Enqueue(protocol);
                    }
                }
                catch (Exception ex)
                {
                    LogDebug($"Receive error: {ex.Message}");
                    break;
                }
            }

            yield return null;
        }

        LogDebug("Receive loop ended");
    }

    private void ProcessIncomingProtocols()
    {
        lock (_protocolLock)
        {
            while (_incomingProtocols.Count > 0)
            {
                Protocol protocol = _incomingProtocols.Dequeue();
                HandleProtocol(protocol);
            }
        }
    }

    public void SendProtocol(Protocol protocol)
    {
        if (!_isConnected || _stream == null) return;

        // 연결 상태 및 스트림 쓰기 가능 여부 점검
        try
        {
            if (_tcpClient == null || _tcpClient.Client == null)
            {
                return;
            }
            if (!_stream.CanWrite)
            {
                LogDebug("Stream not writable. Disconnecting.");
                Disconnect();
                return;
            }
        }
        catch (Exception checkEx)
        {
            LogDebug($"Send pre-check error: {checkEx.Message}");
            Disconnect();
            return;
        }

        // 보낸다



        try
        {
            byte[] data = protocol.Serialize();
            _stream.Write(data, 0, data.Length);
            LogDebug($"Sent protocol: {protocol.Type}");
        }
        catch (Exception ex)
        {
            LogDebug($"Send error: {ex.Message}");
            ErrorOccurred?.Invoke($"Send error: {ex.Message}");
            // 연결 오류 발생 시 안전 종료
            Disconnect();
        }
    }

    private void HandleProtocol(Protocol protocol)
    {
        LogDebug($"Received protocol: {protocol.Type}");


        // 받는다

        switch (protocol.Type)
        {
            case ProtocolType.RESPONSE:
                HandleResponse(protocol);
                break;

            case ProtocolType.BRODCAST_CHAT_MESSAGE:
                HandleChatBroadcast(protocol);
                break;

            case ProtocolType.USER_JOINED:
                HandleUserJoined(protocol);
                break;

            case ProtocolType.USER_LEFT:
                HandleUserLeft(protocol);
                break;

            case ProtocolType.GAME_STARTED:
                HandleGameStarted(protocol);
                break;

            case ProtocolType.RESOURCES_UPDATED:
                HandleResourcesUpdated(protocol);
                break;

            case ProtocolType.FLEET_SPAWNED:
                HandleFleetSpawned(protocol);
                break;

            case ProtocolType.FLEET_MOVING:
                HandleFleetMoving(protocol);
                break;

            case ProtocolType.COMBAT_ENDED:
                HandleCombatEnded(protocol);
                break;

            case ProtocolType.PLANET_CONQUERED:
                HandlePlanetConquered(protocol);
                break;

            case ProtocolType.GAME_ENDED:
                HandleGameEnded(protocol);
                break;

            //case ProtocolType.ERROR:
            //HandleError(protocol);
            //break;

            default:
                LogDebug($"Unhandled protocol type: {protocol.Type}");
                break;
        }
    }

    private void HandleResponse(Protocol protocol)
    {
        // 공통 응답: protoId로 라우팅, status로 성공/실패 확인
        int protoId = protocol.GetParam<int>("protoId");
        byte status = protocol.GetParam<byte>("status");
        bool isSuccess = status == (byte)StateCode.SUCCESS;

        switch (protoId)
        {
            case ProtocolType.REQUEST_CREATE_ROOM:
                if (isSuccess)
                {
                    string createdRoomId = protocol.GetParam<string>("roomId");
                    int slot = protocol.GetParam<int>("slot");
                    LogDebug($"Room created: {createdRoomId}, slot={slot}. Auto-joining...");
                    // 생성 직후 자동 입장
                    JoinRoom(createdRoomId, slot);
                }
                else
                {
                    string reason = protocol.GetParam<string>("message");
                    LogDebug($"Create room failed: {reason}");
                    ErrorOccurred?.Invoke($"Create room failed: {reason}");
                }
                break;
            case ProtocolType.REQUEST_JOIN_ROOM:
                if (isSuccess)
                {
                    var roomInfo = protocol.GetStruct<RoomInfo>("roominfo");
                    LogDebug($"Joined room: {roomInfo.RoomId} ({roomInfo.PlayerCount} players)");
                    JoinRoomSuccess?.Invoke(roomInfo.RoomId, roomInfo.PlayerCount);
                }
                else
                {
                    string reason = protocol.GetParam<string>("message");
                    LogDebug($"Join failed: {reason}");
                    JoinRoomFailed?.Invoke(reason);
                }
                break;

            default:
                // 필요 시 다른 응답 타입도 여기서 라우팅 가능
                break;
        }
    }

    private void HandleChatBroadcast(Protocol protocol)
    {
        var chatMessage = protocol.GetStruct<ChatMessage>("chatMessage");
        LogDebug($"Chat from {chatMessage.SenderId}: {chatMessage.Message}");

        ChatReceived?.Invoke(chatMessage);
    }

    private void HandleUserJoined(Protocol protocol)
    {
        string userId = protocol.GetParam<string>("userId");
        int playerCount = protocol.GetParam<int>("playerCount");

        LogDebug($"User joined: {userId} ({playerCount} players)");
        UserJoined?.Invoke(userId, playerCount);
    }

    private void HandleUserLeft(Protocol protocol)
    {
        string userId = protocol.GetParam<string>("userId");
        int playerCount = protocol.GetParam<int>("playerCount");

        LogDebug($"User left: {userId} ({playerCount} players)");
        UserLeft?.Invoke(userId, playerCount);
    }

    private void HandleGameStarted(Protocol protocol)
    {
        var gameStartData = new GameStartData
        {
            GameId = protocol.GetParam<int>("gameId"),
            MapId = protocol.GetParam<int>("mapId"),
            PlayersJson = protocol.GetParam<string>("players")
        };

        // 내 플레이어 ID 찾기
        try
        {
            var players = JsonConvert.DeserializeObject<PlayerData[]>(gameStartData.PlayersJson);
            foreach (var player in players)
            {
                if (player.SessionId == _sessionId)
                {
                    _myPlayerId = player.PlayerId;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            LogDebug($"Failed to parse players data: {ex.Message}");
        }

        LogDebug($"Game started! GameId: {gameStartData.GameId}, MyPlayerId: {_myPlayerId}");

        GameStarted?.Invoke(gameStartData);
    }

    private void HandleResourcesUpdated(Protocol protocol)
    {
        var resourceUpdate = new ResourceUpdate
        {
            PlayerId = protocol.GetParam<int>("playerId"),
            Minerals = protocol.GetParam<float>("minerals"),
            Gas = protocol.GetParam<float>("gas"),
            CurrentSupply = protocol.GetParam<int>("currentSupply"),
            MaxSupply = protocol.GetParam<int>("maxSupply")
        };

        ResourcesUpdated?.Invoke(resourceUpdate);
    }

    private void HandleFleetSpawned(Protocol protocol)
    {
        var fleetData = new FleetSpawnData
        {
            FleetId = protocol.GetParam<int>("fleetId"),
            FleetType = protocol.GetParam<int>("fleetType"),
            OwnerId = protocol.GetParam<int>("ownerId"),
            PlanetId = protocol.GetParam<int>("planetId")
        };

        FleetSpawned?.Invoke(fleetData);
    }

    private void HandleFleetMoving(Protocol protocol)
    {
        var moveData = new FleetMoveData
        {
            FleetId = protocol.GetParam<int>("fleetId"),
            FromPlanetId = protocol.GetParam<int>("fromPlanetId"),
            ToPlanetId = protocol.GetParam<int>("toPlanetId"),
            Progress = protocol.GetParam<float>("progress")
        };

        FleetMoving?.Invoke(moveData);
    }

    private void HandleCombatEnded(Protocol protocol)
    {
        var combatResult = new CombatResult
        {
            AttackerId = protocol.GetParam<int>("attackerId"),
            DefenderId = protocol.GetParam<int>("defenderId"),
            PlanetId = protocol.GetParam<int>("planetId"),
            AttackerWon = protocol.GetParam<bool>("attackerWon"),
            AttackerLosses = protocol.GetParam<int>("attackerLosses"),
            DefenderLosses = protocol.GetParam<int>("defenderLosses")
        };

        CombatEnded?.Invoke(combatResult);
    }

    private void HandlePlanetConquered(Protocol protocol)
    {
        var conquerData = new PlanetConquerData
        {
            PlanetId = protocol.GetParam<int>("planetId"),
            NewOwnerId = protocol.GetParam<int>("newOwnerId"),
            PreviousOwnerId = protocol.GetParam<int>("previousOwnerId")
        };

        PlanetConquered?.Invoke(conquerData);
    }

    private void HandleGameEnded(Protocol protocol)
    {
        var gameEndData = new GameEndData
        {
            WinnerId = protocol.GetParam<int>("winnerId"),
            Reason = protocol.GetParam<string>("reason"),
            GameDuration = protocol.GetParam<long>("gameDuration")
        };

        GameEnded?.Invoke(gameEndData);
    }

    private void HandleError(Protocol protocol)
    {
        string errorMsg = protocol.GetParam<string>("message");
        LogDebug($"Server error: {errorMsg}");
        ErrorOccurred?.Invoke(errorMsg);
    }


    /*
    private void HandleCommandFailure(Protocol protocol)
    {
        var failureData = new CommandFailureData
        {
            CommandId = protocol.GetParam<int>("commandId"),
            PlayerId = protocol.GetParam<int>("playerId"),
            CommandType = (CommandType)protocol.GetParam<int>("commandType"),
            Reason = (CommandFailureData.FailureReason)protocol.GetParam<int>("reason"),
            Message = protocol.GetParam<string>("message")
        };

        LogDebug($"Command failed: {failureData.CommandType} - {failureData.Reason}: {failureData.Message}");
        CommandFailed?.Invoke(failureData);
    }
    */

    // 공개 메서드들 - 클라이언트 → 서버 통신

    /// <summary>
    /// 방 참가 요청 - 클라이언트 → 서버 (프로토콜: REQUEST_JOIN_ROOM = 10013)
    /// roomId와 slot을 지정해 서버 룸에 입장합니다.
    /// </summary>
    public void JoinRoom(string roomId, int slot, int userId = 0)
    {
        var protocol = new Protocol(ProtocolType.REQUEST_JOIN_ROOM)
            .AddParam("userId", userId)
            .AddParam("roomId", roomId)
            .AddParam("slot", slot);
        SendProtocol(protocol);
    }

    /// <summary>
    /// 매개변수 없는 JoinRoom은 가이드 로그만 출력합니다.
    /// 실제 사용 시 JoinRoom(roomId, slot) 호출하세요.
    /// </summary>
    public void JoinRoom()
    {
        LogDebug("Use JoinRoom(roomId, slot) to join a specific room.");
    }

    /// <summary>
    /// 방 나가기 요청 - 클라이언트 → 서버 (프로토콜: LEAVE_ROOM = 1002)
    /// 직접 프로토콜 전송 방식 사용
    /// </summary>
    public void LeaveRoom()
    {
        // 필요 시 REQUEST_LEFT_ROOM 구현 (요청만 남겨둠)
        // var protocol = new Protocol(ProtocolType.REQUEST_LEFT_ROOM);
        // SendProtocol(protocol);
    }

    /// <summary>
    /// 방 생성 요청 - 클라이언트 → 서버 (프로토콜: REQUEST_CREATE_ROOM = 10012)
    /// 서버 규격에 맞춘 파라미터 이름과 타입 사용
    /// </summary>
    public void CreateRoom(string roomName, int mapId, bool isPrivate = false)
    {
        var protocol = new Protocol(ProtocolType.REQUEST_CREATE_ROOM)
            .AddParam("roomName", roomName)
            .AddParam("mapId", mapId)
            .AddParam("isPrivate", isPrivate);
        SendProtocol(protocol);
    }

    /// <summary>
    /// 준비 상태 변경 요청 - 클라이언트 → 서버 (프로토콜: REQUEST_READY = 10014)
    /// </summary>
    public void RequestReady(bool isReady)
    {
        var protocol = new Protocol(ProtocolType.REQUEST_READY)
            .AddParam("isReady", isReady);
        SendProtocol(protocol);
    }

    /// <summary>
    /// 채팅 메시지 전송 - 클라이언트 → 서버 (프로토콜: CHAT_MESSAGE = 1003)
    /// 직접 프로토콜 전송 방식 사용
    /// </summary>
    public void SendChatMessage(string message)
    {
        var protocol = new Protocol(ProtocolType.CHAT_MESSAGE)
            .AddParam("message", message);
        SendProtocol(protocol);
    }

    /// <summary>
    /// 함대 생산 요청 - 클라이언트 → 서버 (프로토콜: SUBMIT_COMMAND = 3010)
    /// 서버 기준으로 target은 "행성 ID"로 해석되므로 planetId를 전달해야 함.
    /// 함대 타입은 별도 파라미터로 분리하여 함께 전송(서버는 현재 미사용 가능).
    /// </summary>
    public void RequestProduceFleet(int planetId, int fleetType)
    {
        long tick = _currentTick + 10; // 간단히 클라이언트 기준 예약 틱
        var protocol = new Protocol(ProtocolType.SUBMIT_COMMAND)
            .AddParam("commandType", (int)CommonLib.Commands.GameCommandType.ProduceFleet)
            .AddParam("commandData", JsonConvert.SerializeObject(new
            {
                tick = tick,
                target = planetId,
                fleetType = fleetType
            }))
            .AddParam("tick", tick)
            .AddParam("target", planetId)
            .AddParam("fleetType", fleetType);
        SendProtocol(protocol);
        LogDebug($"Submitted ProduceFleet: planet={planetId}, fleetType={fleetType}, tick={tick}");
    }

    /// <summary>
    /// 함대 이동 요청 - 클라이언트 → 서버 (프로토콜: SUBMIT_COMMAND = 3010)
    /// 게임 명령 시스템 사용 (MoveFleetCommand 객체 생성)
    /// GameManager에서 호출하는 함수명과 일치시키기 위한 별칭 함수
    /// </summary>
    public void RequestMoveFleet(int fleetId, int targetPlanetId)
    {
        long tick = _currentTick + 5; // 간단히 클라이언트 기준 예약 틱
        var protocol = new Protocol(ProtocolType.SUBMIT_COMMAND)
            .AddParam("commandType", (int)CommonLib.Commands.GameCommandType.MoveFleet)
            .AddParam("commandData", JsonConvert.SerializeObject(new
            {
                tick = tick,
                target_fleet = fleetId,
                target_planet = targetPlanetId
            }))
            .AddParam("tick", tick)
            .AddParam("target_fleet", fleetId)
            .AddParam("target_planet", targetPlanetId);
        SendProtocol(protocol);
        LogDebug($"Submitted MoveFleet: fleet={fleetId} -> planet={targetPlanetId}, tick={tick}");
    }

    // 사용하지 않음: 서버가 Protocol 내 개별 파라미터를 직접 읽으므로 각 요청에서 바로 직렬화해 전송합니다.
    // private void SubmitGameCommand(IGameCommand command) { }

    private void StartHeartbeat()
    {
        if (_heartbeatCoroutine != null)
        {
            StopCoroutine(_heartbeatCoroutine);
        }
        _heartbeatCoroutine = StartCoroutine(HeartbeatLoop());
    }

    private void StopHeartbeat()
    {
        if (_heartbeatCoroutine != null)
        {
            StopCoroutine(_heartbeatCoroutine);
            _heartbeatCoroutine = null;
        }
    }

    private IEnumerator HeartbeatLoop()
    {
        while (_isConnected)
        {
            var heartbeat = new Protocol(ProtocolType.HEARTBEAT);
            SendProtocol(heartbeat);

            // 서버 타임아웃(30초)보다 짧게 보냅니다.
            yield return new WaitForSeconds(10f);
        }
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[UnityGameClient] {message}");
        }
    }
}
