using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using CommonLib;

namespace GameClient
{
    /// <summary>
    /// 네트워크 프로토콜 테스트 컴포넌트 - Unity Inspector에서 실행 가능
    /// </summary>
    [System.Serializable]
    public class ProtocolTestData
    {
        [Header("프로토콜 정보")]
        public string protocolName;
        public int protocolType;
        
        [Header("파라미터")]
        public bool hasParameters = false;
        public List<ParameterData> parameters = new List<ParameterData>();
        
        [Header("실행")]
        [Space(10)]
        public bool executeOnStart = false;
    }

    [System.Serializable]
    public class ParameterData
    {
        public string key;
        public ParameterType type;
        
        [Header("값 (타입에 맞는 것만 사용)")]
        public string stringValue;
        public int intValue;
        public bool boolValue;
        public long longValue;
        public float floatValue;
    }

    public enum ParameterType
    {
        String,
        Int,
        Bool,
        Long,
        Float
    }

    public class NetworkProtocolTester : MonoBehaviour
    {
        [Header("=== 네트워크 프로토콜 테스터 ===")]
        [Space(10)]
        
        [Header("연결 설정")]
        [SerializeField] private string serverAddress = "localhost";
        [SerializeField] private int serverPort = 8080;
        [SerializeField] private bool autoConnect = true;
        
        [Header("로그 설정")]
        [SerializeField] private bool showDetailedLogs = true;
        [SerializeField] private bool showResponseLogs = true;
        
        [Space(20)]
        [Header("=== 빠른 테스트 버튼들 ===")]
        [Space(10)]
        
        [Header("연결 테스트")]
        [SerializeField] private bool connectToServer = false;
        [SerializeField] private bool disconnectFromServer = false;
        [SerializeField] private bool testHeartbeat = false;
        
        [Header("인증 테스트")]
        [SerializeField] private string loginId = "testUser";
        [SerializeField] private string loginPassword = "testPass";
        [SerializeField] private bool executeLogin = false;
        [SerializeField] private bool executeLogout = false;
        
        [Header("로비 테스트")]
        [SerializeField] private int lobbyPage = 0;
        [SerializeField] private bool executeJoinLobby = false;
        [SerializeField] private bool executeRefreshLobby = false;
        
        [Header("룸 테스트")]
        [SerializeField] private string roomName = "테스트 룸";
        [SerializeField] private string mapId = "map_001";
        [SerializeField] private bool isPrivateRoom = false;
        [SerializeField] private bool executeCreateRoom = false;
        
        [SerializeField] private string targetRoomId = "room_123";
        [SerializeField] private int targetSlot = -1;
        [SerializeField] private bool executeJoinRoom = false;
        [SerializeField] private bool executeLeaveRoom = false;
        
        [SerializeField] private bool readyState = true;
        [SerializeField] private bool executeReady = false;
        
        [Header("채팅 테스트")]
        [SerializeField] private string chatMessage = "안녕하세요!";
        [SerializeField] private string chatChannelId = "lobby";
        [SerializeField] private int chatMessageType = 0;
        [SerializeField] private bool executeChatMessage = false;
        [SerializeField] private bool executeJoinChatChannel = false;
        [SerializeField] private bool executeLeaveChatChannel = false;
        
        [Header("데이터 테스트")]
        [SerializeField] private string tableName = "testTable";
        [SerializeField] private bool executeRequestTableData = false;
        
        [Space(20)]
        [Header("=== 커스텀 프로토콜 테스트 ===")]
        [Space(10)]
        [SerializeField] private List<ProtocolTestData> customProtocols = new List<ProtocolTestData>();
        
        [Header("커스텀 실행")]
        [SerializeField] private int customProtocolIndex = 0;
        [SerializeField] private bool executeCustomProtocol = false;
        
        [Space(20)]
        [Header("=== 상태 정보 ===")]
        [Space(10)]
        [SerializeField] private bool isConnected = false;
        [SerializeField] private string connectionStatus = "미연결";
        [SerializeField] private string lastResponse = "";
        [SerializeField] private string lastError = "";

        // 네트워크 클라이언트
        private ClientServerHandler networkClient;
        
        // 매니저들
        private RoomManager roomManager;
        
        void Start()
        {
            InitializeComponents();
            
            if (autoConnect)
            {
                ConnectToServer();
            }
            
            // 시작 시 자동 실행 프로토콜들 처리
            ExecuteAutoStartProtocols();
        }

        void Update()
        {
            // Inspector 값 변경 감지 및 실행
            HandleInspectorCommands();
            
            // 상태 업데이트
            UpdateStatus();
        }

        /// <summary>
        /// 컴포넌트 초기화
        /// </summary>
        private void InitializeComponents()
        {
            try
            {
                networkClient = ClientServerHandler.Instance;
                roomManager = RoomManager.Instance;
                
                LogMessage("네트워크 테스터 초기화 완료");
            }
            catch (Exception e)
            {
                LogError($"초기화 실패: {e.Message}");
            }
        }

        /// <summary>
        /// Inspector 명령어 처리
        /// </summary>
        private void HandleInspectorCommands()
        {
            // 연결 명령어
            if (connectToServer)
            {
                connectToServer = false;
                ConnectToServer();
            }
            
            if (disconnectFromServer)
            {
                disconnectFromServer = false;
                DisconnectFromServer();
            }
            
            if (testHeartbeat)
            {
                testHeartbeat = false;
                _ = TestHeartbeat();
            }
            
            // 인증 명령어
            if (executeLogin)
            {
                executeLogin = false;
                _ = TestLogin(loginId, loginPassword);
            }
            
            if (executeLogout)
            {
                executeLogout = false;
                _ = TestLogout();
            }
            
            // 로비 명령어
            if (executeJoinLobby)
            {
                executeJoinLobby = false;
                _ = TestJoinLobby(lobbyPage);
            }
            
            if (executeRefreshLobby)
            {
                executeRefreshLobby = false;
                _ = TestRefreshLobby();
            }
            
            // 룸 명령어
            if (executeCreateRoom)
            {
                executeCreateRoom = false;
                _ = TestCreateRoom(roomName, mapId, isPrivateRoom);
            }
            
            if (executeJoinRoom)
            {
                executeJoinRoom = false;
                _ = TestJoinRoom(targetRoomId, targetSlot);
            }
            
            if (executeLeaveRoom)
            {
                executeLeaveRoom = false;
                _ = TestLeaveRoom();
            }
            
            if (executeReady)
            {
                executeReady = false;
                _ = TestReady(readyState);
            }
            
            // 채팅 명령어
            if (executeChatMessage)
            {
                executeChatMessage = false;
                _ = TestChatMessage(chatMessage, chatMessageType, chatChannelId);
            }
            
            if (executeJoinChatChannel)
            {
                executeJoinChatChannel = false;
                _ = TestJoinChatChannel(chatChannelId);
            }
            
            if (executeLeaveChatChannel)
            {
                executeLeaveChatChannel = false;
                _ = TestLeaveChatChannel();
            }
            
            // 데이터 명령어
            if (executeRequestTableData)
            {
                executeRequestTableData = false;
                _ = TestRequestTableData(tableName);
            }
            
            // 커스텀 프로토콜
            if (executeCustomProtocol)
            {
                executeCustomProtocol = false;
                _ = ExecuteCustomProtocol(customProtocolIndex);
            }
        }

        /// <summary>
        /// 자동 시작 프로토콜들 실행
        /// </summary>
        private void ExecuteAutoStartProtocols()
        {
            foreach (var protocol in customProtocols)
            {
                if (protocol.executeOnStart)
                {
                    _ = ExecuteProtocol(protocol);
                }
            }
        }

        /// <summary>
        /// 상태 업데이트
        /// </summary>
        private void UpdateStatus()
        {
            if (networkClient != null)
            {
                isConnected = networkClient.IsConnected;
                connectionStatus = isConnected ? "연결됨" : "미연결";
            }
            else
            {
                isConnected = false;
                connectionStatus = "클라이언트 없음";
            }
        }

        // === 네트워크 기본 기능 테스트 ===

        /// <summary>
        /// 서버 연결
        /// </summary>
        private void ConnectToServer()
        {
            try
            {
                if (networkClient != null)
                {
                    // 실제 연결 로직은 ClientServerHandler에서 처리
                    LogMessage($"서버 연결 시도: {serverAddress}:{serverPort}");
                }
                else
                {
                    LogError("네트워크 클라이언트가 없습니다");
                }
            }
            catch (Exception e)
            {
                LogError($"서버 연결 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 서버 연결 해제
        /// </summary>
        private void DisconnectFromServer()
        {
            try
            {
                if (networkClient != null)
                {
                    // 실제 연결 해제 로직
                    LogMessage("서버 연결 해제");
                }
            }
            catch (Exception e)
            {
                LogError($"연결 해제 실패: {e.Message}");
            }
        }

        // === 프로토콜 테스트 메서드들 ===

        /// <summary>
        /// 하트비트 테스트
        /// </summary>
        private async Task TestHeartbeat()
        {
            try
            {
                LogMessage("하트비트 테스트 시작");
                
                var protocol = new Protocol(ProtocolType.HEARTBEAT)
                    .AddParam("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

                NetworkResponse response = await networkClient.AsyncSend(protocol);
                HandleResponse("HEARTBEAT", response);
            }
            catch (Exception e)
            {
                LogError($"하트비트 테스트 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 로그인 테스트
        /// </summary>
        private async Task TestLogin(string id, string password)
        {
            try
            {
                LogMessage($"로그인 테스트: {id}");
                
                var protocol = new Protocol(ProtocolType.REQUEST_LOGIN)
                    .AddParam("id", id)
                    .AddParam("password", password);

                NetworkResponse response = await networkClient.AsyncSend(protocol);
                HandleResponse("LOGIN", response);
            }
            catch (Exception e)
            {
                LogError($"로그인 테스트 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 로그아웃 테스트
        /// </summary>
        private async Task TestLogout()
        {
            try
            {
                LogMessage("로그아웃 테스트");
                
                var protocol = new Protocol(ProtocolType.REQUEST_LOGOUT);

                NetworkResponse response = await networkClient.AsyncSend(protocol);
                HandleResponse("LOGOUT", response);
            }
            catch (Exception e)
            {
                LogError($"로그아웃 테스트 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 로비 접속 테스트
        /// </summary>
        private async Task TestJoinLobby(int page)
        {
            try
            {
                LogMessage($"로비 접속 테스트 (Page: {page})");
                
                var protocol = new Protocol(ProtocolType.REQUEST_JOIN_LOBBY)
                    .AddParam("Page", page);

                NetworkResponse response = await networkClient.AsyncSend(protocol);
                HandleResponse("JOIN_LOBBY", response);
            }
            catch (Exception e)
            {
                LogError($"로비 접속 테스트 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 로비 새로고침 테스트
        /// </summary>
        private async Task TestRefreshLobby()
        {
            try
            {
                LogMessage("로비 새로고침 테스트");
                
                var protocol = new Protocol(ProtocolType.REFRESH_LOBBY);

                NetworkResponse response = await networkClient.AsyncSend(protocol);
                HandleResponse("REFRESH_LOBBY", response);
            }
            catch (Exception e)
            {
                LogError($"로비 새로고침 테스트 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 룸 생성 테스트
        /// </summary>
        private async Task TestCreateRoom(string roomName, string mapId, bool isPrivate)
        {
            try
            {
                LogMessage($"룸 생성 테스트: {roomName}");
                
                var protocol = new Protocol(ProtocolType.REQUEST_CREATE_ROOM)
                    .AddParam("room_name", roomName)
                    .AddParam("mapId", mapId)
                    .AddParam("is_private", isPrivate);

                NetworkResponse response = await networkClient.AsyncSend(protocol);
                HandleResponse("CREATE_ROOM", response);
            }
            catch (Exception e)
            {
                LogError($"룸 생성 테스트 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 룸 참가 테스트
        /// </summary>
        private async Task TestJoinRoom(string roomId, int slot)
        {
            try
            {
                LogMessage($"룸 참가 테스트: {roomId}");
                
                var protocol = new Protocol(ProtocolType.REQUEST_JOIN_ROOM)
                    .AddParam("roomId", roomId)
                    .AddParam("slot", slot);

                NetworkResponse response = await networkClient.AsyncSend(protocol);
                HandleResponse("JOIN_ROOM", response);
            }
            catch (Exception e)
            {
                LogError($"룸 참가 테스트 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 룸 나가기 테스트
        /// </summary>
        private async Task TestLeaveRoom()
        {
            try
            {
                LogMessage("룸 나가기 테스트");
                
                var protocol = new Protocol(ProtocolType.REQUEST_LEFT_ROOM);

                NetworkResponse response = await networkClient.AsyncSend(protocol);
                HandleResponse("LEAVE_ROOM", response);
            }
            catch (Exception e)
            {
                LogError($"룸 나가기 테스트 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 게임 준비 테스트
        /// </summary>
        private async Task TestReady(bool isReady)
        {
            try
            {
                LogMessage($"게임 준비 테스트: {isReady}");
                
                var protocol = new Protocol(ProtocolType.REQUEST_READY)
                    .AddParam("isReady", isReady);

                NetworkResponse response = await networkClient.AsyncSend(protocol);
                HandleResponse("READY", response);
            }
            catch (Exception e)
            {
                LogError($"게임 준비 테스트 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 채팅 메시지 테스트
        /// </summary>
        private async Task TestChatMessage(string message, int messageType, string channelId)
        {
            try
            {
                LogMessage($"채팅 메시지 테스트: {message}");
                
                var chatMessage = new ChatMessage
                {
                    Message = message,
                    MessageType = messageType,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                
                var protocol = new Protocol(ProtocolType.CHAT_MESSAGE)
                    .AddParam("type", messageType)
                    .AddParam("channelId", channelId)
                    .AddParam("chatMessage", chatMessage);

                NetworkResponse response = await networkClient.AsyncSend(protocol);
                HandleResponse("CHAT_MESSAGE", response);
            }
            catch (Exception e)
            {
                LogError($"채팅 메시지 테스트 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 채팅 채널 참가 테스트
        /// </summary>
        private async Task TestJoinChatChannel(string channelId)
        {
            try
            {
                LogMessage($"채팅 채널 참가 테스트: {channelId}");
                
                var protocol = new Protocol(ProtocolType.CHAT_CHANNEL_JOIN)
                    .AddParam("channelId", channelId);

                NetworkResponse response = await networkClient.AsyncSend(protocol);
                HandleResponse("JOIN_CHAT_CHANNEL", response);
            }
            catch (Exception e)
            {
                LogError($"채팅 채널 참가 테스트 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 채팅 채널 나가기 테스트
        /// </summary>
        private async Task TestLeaveChatChannel()
        {
            try
            {
                LogMessage("채팅 채널 나가기 테스트");
                
                var protocol = new Protocol(ProtocolType.CHAT_CHANNEL_LEFT)
                    .AddParam("channelId", chatChannelId);

                NetworkResponse response = await networkClient.AsyncSend(protocol);
                HandleResponse("LEAVE_CHAT_CHANNEL", response);
            }
            catch (Exception e)
            {
                LogError($"채팅 채널 나가기 테스트 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 테이블 데이터 요청 테스트
        /// </summary>
        private async Task TestRequestTableData(string tableName)
        {
            try
            {
                LogMessage($"테이블 데이터 요청 테스트: {tableName}");
                
                var protocol = new Protocol(ProtocolType.REQUEST_TABLEDATA)
                    .AddParam("table_name", tableName);

                NetworkResponse response = await networkClient.AsyncSend(protocol);
                HandleResponse("REQUEST_TABLEDATA", response);
            }
            catch (Exception e)
            {
                LogError($"테이블 데이터 요청 테스트 실패: {e.Message}");
            }
        }

        // === 커스텀 프로토콜 실행 ===

        /// <summary>
        /// 커스텀 프로토콜 실행
        /// </summary>
        private async Task ExecuteCustomProtocol(int index)
        {
            if (index < 0 || index >= customProtocols.Count)
            {
                LogError($"잘못된 커스텀 프로토콜 인덱스: {index}");
                return;
            }

            await ExecuteProtocol(customProtocols[index]);
        }

        /// <summary>
        /// 프로토콜 실행
        /// </summary>
        private async Task ExecuteProtocol(ProtocolTestData protocolData)
        {
            try
            {
                LogMessage($"커스텀 프로토콜 실행: {protocolData.protocolName}");
                
                var protocol = new Protocol(protocolData.protocolType);
                
                // 파라미터 추가
                if (protocolData.hasParameters)
                {
                    foreach (var param in protocolData.parameters)
                    {
                        AddParameterToProtocol(protocol, param);
                    }
                }

                NetworkResponse response = await networkClient.AsyncSend(protocol);
                HandleResponse(protocolData.protocolName, response);
            }
            catch (Exception e)
            {
                LogError($"커스텀 프로토콜 실행 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 프로토콜에 파라미터 추가
        /// </summary>
        private void AddParameterToProtocol(Protocol protocol, ParameterData param)
        {
            switch (param.type)
            {
                case ParameterType.String:
                    protocol.AddParam(param.key, param.stringValue);
                    break;
                case ParameterType.Int:
                    protocol.AddParam(param.key, param.intValue);
                    break;
                case ParameterType.Bool:
                    protocol.AddParam(param.key, param.boolValue);
                    break;
                case ParameterType.Long:
                    protocol.AddParam(param.key, param.longValue);
                    break;
                case ParameterType.Float:
                    protocol.AddParam(param.key, param.floatValue);
                    break;
            }
        }

        // === 응답 처리 및 로깅 ===

        /// <summary>
        /// 응답 처리
        /// </summary>
        private void HandleResponse(string protocolName, NetworkResponse response)
        {
            if (showResponseLogs)
            {
                if (response.isSuccess)
                {
                    LogMessage($"[{protocolName}] 성공 - 코드: {response.resultCode}");
                    lastResponse = $"{protocolName}: 성공 ({response.resultCode})";
                }
                else
                {
                    LogError($"[{protocolName}] 실패 - 코드: {response.resultCode}");
                    lastError = $"{protocolName}: 실패 ({response.resultCode})";
                }
            }
        }

        /// <summary>
        /// 메시지 로깅
        /// </summary>
        private void LogMessage(string message)
        {
            if (showDetailedLogs)
            {
                Debug.Log($"[NetworkTester] {message}");
            }
        }

        /// <summary>
        /// 에러 로깅
        /// </summary>
        private void LogError(string error)
        {
            Debug.LogError($"[NetworkTester] {error}");
            lastError = error;
        }

        // === Inspector 편의 기능 ===

        /// <summary>
        /// 기본 커스텀 프로토콜 설정
        /// </summary>
        [ContextMenu("기본 커스텀 프로토콜 추가")]
        private void AddDefaultCustomProtocols()
        {
            customProtocols.Clear();
            
            // 하트비트 예시
            customProtocols.Add(new ProtocolTestData
            {
                protocolName = "HEARTBEAT_CUSTOM",
                protocolType = ProtocolType.HEARTBEAT,
                hasParameters = true,
                parameters = new List<ParameterData>
                {
                    new ParameterData { key = "timestamp", type = ParameterType.Long, longValue = 0 }
                }
            });
            
            // 로그인 예시
            customProtocols.Add(new ProtocolTestData
            {
                protocolName = "LOGIN_CUSTOM",
                protocolType = ProtocolType.REQUEST_LOGIN,
                hasParameters = true,
                parameters = new List<ParameterData>
                {
                    new ParameterData { key = "id", type = ParameterType.String, stringValue = "testUser" },
                    new ParameterData { key = "password", type = ParameterType.String, stringValue = "testPass" }
                }
            });
            
            LogMessage("기본 커스텀 프로토콜이 추가되었습니다");
        }

        /// <summary>
        /// 모든 설정 초기화
        /// </summary>
        [ContextMenu("설정 초기화")]
        private void ResetAllSettings()
        {
            loginId = "testUser";
            loginPassword = "testPass";
            roomName = "테스트 룸";
            mapId = "map_001";
            chatMessage = "안녕하세요!";
            customProtocols.Clear();
            
            LogMessage("모든 설정이 초기화되었습니다");
        }
    }
}