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
    public int serverPort = 8080;
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

            // 자동으로 룸 입장 시도
            JoinRoom();
        }
        else
        {
            LogDebug("Connection failed!");
            ConnectionChanged?.Invoke(false, "Connection failed");
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
            LogDebug($"Connection error: {ex.Message}");
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

                    int messageSize = BitConverter.ToInt32(sizeBuffer, 0);

                    // 전체 메시지 읽기
                    byte[] messageBuffer = new byte[messageSize];
                    Array.Copy(sizeBuffer, messageBuffer, 4);

                    int totalRead = 4;
                    while (totalRead < messageSize)
                    {
                        bytesRead = _stream.Read(messageBuffer, totalRead, messageSize - totalRead);
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
        }
    }

    private void HandleProtocol(Protocol protocol)
    {
        LogDebug($"Received protocol: {protocol.Type}");


        // 받는다

        switch (protocol.Type)
        {
            case ProtocolType.JOIN_SUCCESS:
                HandleJoinSuccess(protocol);
                break;

            case ProtocolType.JOIN_FAILED:
                HandleJoinFailed(protocol);
                break;

            case ProtocolType.CHAT_BROADCAST:
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

            case ProtocolType.ERROR:
                HandleError(protocol);
                break;

            default:
                LogDebug($"Unhandled protocol type: {protocol.Type}");
                break;
        }
    }

    private void HandleJoinSuccess(Protocol protocol)
    {
        _sessionId = protocol.GetParam<string>("sessionId");
        var roomInfo = protocol.GetStruct<RoomInfo>("roomInfo");

        LogDebug($"Joined room: {roomInfo.RoomId} ({roomInfo.PlayerCount} players)");
        JoinRoomSuccess?.Invoke(roomInfo.RoomId, roomInfo.PlayerCount);
    }

    private void HandleJoinFailed(Protocol protocol)
    {
        string reason = protocol.GetParam<string>("message");
        LogDebug($"Join failed: {reason}");
        JoinRoomFailed?.Invoke(reason);
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
    /// 방 참가 요청 - 클라이언트 → 서버 (프로토콜: JOIN_ROOM = 1001)
    /// 직접 프로토콜 전송 방식 사용
    /// </summary>
    public void JoinRoom()
    {
        var protocol = new Protocol(ProtocolType.JOIN_ROOM);
        SendProtocol(protocol);
    }

    /// <summary>
    /// 방 나가기 요청 - 클라이언트 → 서버 (프로토콜: LEAVE_ROOM = 1002)
    /// 직접 프로토콜 전송 방식 사용
    /// </summary>
    public void LeaveRoom()
    {
        var protocol = new Protocol(ProtocolType.LEAVE_ROOM);
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
    /// 게임 명령 시스템 사용 (ProduceFleetCommand 객체 생성)
    /// GameManager에서 호출하는 함수명과 일치시키기 위한 별칭 함수
    /// </summary>
    public void RequestProduceFleet(int targetPlanetId)
    {
        if (_myPlayerId == -1)
        {
            LogDebug("Player ID not set!");
            return;
        }

        var command = new ProduceFleetCommand
        {
            PlayerId = _myPlayerId,
            TickNumber = _currentTick + 10,
            TargetId = targetPlanetId
        };

        SubmitGameCommand(command);
    }

    /// <summary>
    /// 함대 이동 요청 - 클라이언트 → 서버 (프로토콜: SUBMIT_COMMAND = 3010)
    /// 게임 명령 시스템 사용 (MoveFleetCommand 객체 생성)
    /// GameManager에서 호출하는 함수명과 일치시키기 위한 별칭 함수
    /// </summary>
    public void RequestMoveFleet(int fleetId, int targetPlanetId)
    {
        if (_myPlayerId == -1)
        {
            LogDebug("Player ID not set!");
            return;
        }

        var command = new MoveFleetCommand
        {
            PlayerId = _myPlayerId,
            TickNumber = _currentTick + 5,
            TargetFleet = fleetId,
            TargetPlanetId = targetPlanetId
        };

        SubmitGameCommand(command);
    }

    private void SubmitGameCommand(IGameCommand command)
    {
        var protocol = new Protocol(ProtocolType.SUBMIT_COMMAND)
            .AddParam("commandType", (int)command.Type)
            .AddParam("commandData", JsonConvert.SerializeObject(command));

        SendProtocol(protocol);
        LogDebug($"Submitted command: {command.Type}");
    }

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

            yield return new WaitForSeconds(30f);
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
