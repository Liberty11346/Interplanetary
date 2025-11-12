using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using CommonLib;

namespace GameClient
{
    /// <summary>
    /// 룸 관리 및 서버 통신 담당 (BaseManager 의존성 제거)
    /// </summary>
    public class RoomManager : MonoBehaviour
    {
        // --- 싱글톤 ---
        private static RoomManager _instance;
        private static readonly object _lock = new object();

        public static RoomManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = FindObjectOfType<RoomManager>();
                            if (_instance == null)
                            {
                                GameObject go = new GameObject(typeof(RoomManager).Name);
                                _instance = go.AddComponent<RoomManager>();
                                DontDestroyOnLoad(go);
                            }
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
        // --- 룸 관련 이벤트들 ---
        public event Action<RoomInfo> OnRoomJoinSuccess;
        public event Action<string> OnRoomJoinFailure;
        public event Action<List<RoomInfo>> OnRoomListUpdated;
        public event Action OnRoomLeft;

        // --- 현재 상태 ---
        private RoomInfo? currentRoom = null;
        private List<RoomInfo> cachedRoomList = new List<RoomInfo>();
        private bool isInRoom = false;

        // --- 속성들 ---
        public RoomInfo? CurrentRoom => currentRoom;
        public bool IsInRoom => isInRoom;
        public List<RoomInfo> CachedRoomList => new List<RoomInfo>(cachedRoomList);

        // --- 초기화 및 생명주기 ---
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            try
            {
                networkClient = ClientServerHandler.Instance;
                if (networkClient != null)
                {
                    Debug.Log("[RoomManager] 네트워크 클라이언트 연결됨");
                }
                else
                {
                    Debug.LogError("[RoomManager] 네트워크 클라이언트를 찾을 수 없음");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[RoomManager] 네트워크 클라이언트 초기화 실패: {e.Message}");
            }

            RegisterNetworkHandlers();
            isInitialized = true;
            EmitStatusMessage("RoomManager 초기화 완료");
        }

        private void RegisterNetworkHandlers()
        {
            // 룸 관련 브로드캐스트 메시지 처리
            RegisterHandler(ProtocolType.BRODCAST_SYSTEM, HandleRoomBroadcast);
            RegisterHandler(ProtocolType.USER_JOINED, HandleUserJoined);
            RegisterHandler(ProtocolType.USER_LEFT, HandleUserLeft);
            RegisterHandler(ProtocolType.ROOM_INFO_CHANGED, HandleRoomInfoChanged);
            RegisterHandler(ProtocolType.ROOM_CLOSED, HandleRoomClosed);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                Cleanup();
                _instance = null;
            }
        }

        private void Cleanup()
        {
            currentRoom = null;
            isInRoom = false;
            cachedRoomList.Clear();
            EmitStatusMessage("RoomManager 정리 완료");
        }

        public void Reset()
        {
            currentRoom = null;
            isInRoom = false;
            cachedRoomList.Clear();
        }

        // --- 룸 관련 공개 메서드들 ---

        /// <summary>
        /// 로비 접속 및 룸 목록 요청
        /// </summary>
        public async Task<bool> RequestJoinLobbyAsync(int page = 0)
        {
            try
            {
                ValidateNetworkConnection();

                var protocol = new Protocol(ProtocolType.REQUEST_JOIN_LOBBY)
                    .AddParam("Page", page);

                NetworkResponse response = await SafeSendAsync(protocol, "로비 접속");

                if (response.isSuccess)
                {
                    // 응답 파라미터: roomCount, page, roomList
                    int roomCount = response.GetParam<int>("roomCount");
                    int responsePage = response.GetParam<int>("page");
                    var roomListData = response.GetParam<object[]>("roomList");

                    cachedRoomList.Clear();

                    if (roomListData != null)
                    {
                        foreach (var roomData in roomListData)
                        {
                            if (roomData is Dictionary<string, object> roomDict)
                            {
                                var roomInfo = ParseRoomInfo(roomDict);
                                cachedRoomList.Add(roomInfo);
                            }
                        }
                    }

                    EmitStatusMessage($"총 {roomCount}개 중 {cachedRoomList.Count}개의 룸 로드됨 (Page: {responsePage})");
                    OnRoomListUpdated?.Invoke(cachedRoomList);
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                EmitError($"로비 접속 오류: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 로비 새로고침 (룸 목록 갱신)
        /// </summary>
        public async Task<bool> RefreshLobbyAsync()
        {
            try
            {
                ValidateNetworkConnection();

                var protocol = new Protocol(ProtocolType.REFRESH_LOBBY);

                NetworkResponse response = await SafeSendAsync(protocol, "로비 새로고침");

                if (response.isSuccess)
                {
                    // 응답 파라미터: roomList
                    var roomListData = response.GetParam<object[]>("roomList");
                    cachedRoomList.Clear();

                    if (roomListData != null)
                    {
                        foreach (var roomData in roomListData)
                        {
                            if (roomData is Dictionary<string, object> roomDict)
                            {
                                var roomInfo = ParseRoomInfo(roomDict);
                                cachedRoomList.Add(roomInfo);
                            }
                        }
                    }

                    EmitStatusMessage($"총 {cachedRoomList.Count}개의 룸 갱신됨");
                    OnRoomListUpdated?.Invoke(cachedRoomList);
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                EmitError($"로비 새로고침 오류: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 룸 생성 요청
        /// </summary>
        public async Task<bool> RequestCreateRoomAsync(string roomName, string mapId, bool isPrivate = false)
        {
            try
            {
                ValidateNetworkConnection();

                var protocol = new Protocol(ProtocolType.REQUEST_CREATE_ROOM)
                    .AddParam("room_name", roomName)
                    .AddParam("mapId", mapId)
                    .AddParam("is_private", isPrivate);

                NetworkResponse response = await SafeSendAsync(protocol, "룸 생성");

                if (response.isSuccess)
                {
                    // 응답 파라미터: roomId, slot
                    string roomId = response.GetParam<string>("roomId");
                    int slot = response.GetParam<int>("slot");

                    EmitStatusMessage($"생성된 룸 ID: {roomId}, 슬롯: {slot}");
                    return true;
                }
                else
                {
                    string failureReason = response.GetParam<string>("message");
                    if (string.IsNullOrEmpty(failureReason))
                        failureReason = response.resultCode.ToString();

                    OnRoomJoinFailure?.Invoke(failureReason);
                    return false;
                }
            }
            catch (Exception e)
            {
                EmitError($"룸 생성 오류: {e.Message}");
                OnRoomJoinFailure?.Invoke($"룸 생성 오류: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 룸 참가 요청
        /// </summary>
        public async Task<bool> RequestJoinRoomAsync(string roomId, int slot = -1)
        {
            try
            {
                ValidateNetworkConnection();

                var protocol = new Protocol(ProtocolType.REQUEST_JOIN_ROOM)
                    .AddParam("roomId", roomId)
                    .AddParam("slot", slot);

                NetworkResponse response = await SafeSendAsync(protocol, "룸 참가");

                if (response.isSuccess)
                {
                    // 응답 파라미터: roomInfo, chatChannelID
                    var roomInfoData = response.GetParam<Dictionary<string, object>>("roomInfo");
                    string chatChannelID = response.GetParam<string>("chatChannelID");

                    if (roomInfoData != null)
                    {
                        currentRoom = ParseRoomInfo(roomInfoData);
                        isInRoom = true;

                        EmitStatusMessage($"룸에 참가함: {currentRoom.Value}, 채팅 채널: {chatChannelID}");
                        OnRoomJoinSuccess?.Invoke(currentRoom.Value);
                        return true;
                    }
                    else
                    {
                        OnRoomJoinFailure?.Invoke("룸 정보를 받지 못했습니다");
                        return false;
                    }
                }
                else
                {
                    string failureReason = response.GetParam<string>("message");
                    if (string.IsNullOrEmpty(failureReason))
                        failureReason = response.resultCode.ToString();

                    OnRoomJoinFailure?.Invoke(failureReason);
                    return false;
                }
            }
            catch (Exception e)
            {
                EmitError($"룸 참가 오류: {e.Message}");
                OnRoomJoinFailure?.Invoke($"룸 참가 오류: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 룸 나가기 요청
        /// </summary>
        public async Task<bool> RequestLeaveRoomAsync()
        {
            try
            {
                if (!isInRoom)
                {
                    EmitStatusMessage("현재 룸에 있지 않음");
                    return true; // 이미 나가진 상태
                }

                ValidateNetworkConnection();

                var protocol = new Protocol(ProtocolType.REQUEST_LEFT_ROOM);

                NetworkResponse response = await SafeSendAsync(protocol, "룸 나가기");

                if (response.isSuccess)
                {
                    // 상태 초기화
                    currentRoom = null;
                    isInRoom = false;

                    OnRoomLeft?.Invoke();
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                EmitError($"룸 나가기 오류: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 게임 준비 상태 변경
        /// </summary>
        public async Task<bool> RequestReadyAsync(bool isReady)
        {
            try
            {
                if (!isInRoom)
                {
                    EmitError("현재 룸에 있지 않음");
                    return false;
                }

                ValidateNetworkConnection();

                var protocol = new Protocol(ProtocolType.REQUEST_READY)
                    .AddParam("isReady", isReady);

                NetworkResponse response = await SafeSendAsync(protocol, $"게임 준비 ({isReady})");

                return response.isSuccess;
            }
            catch (Exception e)
            {
                EmitError($"게임 준비 오류: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 특정 룸 정보 찾기
        /// </summary>
        public RoomInfo? FindRoom(string roomId)
        {
            return cachedRoomList.Find(room => room.RoomId == roomId);
        }

        /// <summary>
        /// 참가 가능한 룸 목록 가져오기
        /// </summary>
        public List<RoomInfo> GetJoinableRooms()
        {
            return cachedRoomList.FindAll(room =>
                room.RoomState == CommonLib.RoomState.Open && room.PlayerCount < room.MaxPlayers);
        }

        // --- 내부 메서드들 ---

        /// <summary>
        /// Dictionary에서 RoomInfo 구조체로 파싱
        /// </summary>
        private RoomInfo ParseRoomInfo(Dictionary<string, object> data)
        {
            return new RoomInfo
            {
                RoomId = GetStringValue(data, "roomId"),
                RoomName = GetStringValue(data, "roomName"),
                PlayerCount = GetIntValue(data, "playerCount"),
                MaxPlayers = GetIntValue(data, "maxPlayers"),
                RoomState = (CommonLib.RoomState)GetIntValue(data, "roomState"),
                MapID = GetIntValue(data, "mapId")
            };
        }

        // --- 네트워크 이벤트 핸들러들 ---

        /// <summary>
        /// 시스템 브로드캐스트 처리
        /// </summary>
        private async Task HandleRoomBroadcast(Protocol protocol)
        {
            string messageType = protocol.GetParam<string>("messageType");
            EmitStatusMessage($"시스템 브로드캐스트: {messageType}");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 룸 정보 변경 처리
        /// </summary>
        private async Task HandleRoomInfoChanged(Protocol protocol)
        {
            var roomInfoData = protocol.GetParam<Dictionary<string, object>>("roomInfo");
            if (roomInfoData != null)
            {
                var updatedRoom = ParseRoomInfo(roomInfoData);

                // 현재 룸 정보 업데이트
                if (isInRoom && currentRoom.HasValue && currentRoom.Value.RoomId == updatedRoom.RoomId)
                {
                    currentRoom = updatedRoom;
                    EmitStatusMessage($"현재 룸 정보 업데이트: {updatedRoom}");
                }

                // 캐시된 룸 목록 업데이트
                int index = cachedRoomList.FindIndex(room => room.RoomId == updatedRoom.RoomId);
                if (index >= 0)
                {
                    cachedRoomList[index] = updatedRoom;
                    OnRoomListUpdated?.Invoke(cachedRoomList);
                }
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// 룸 폐쇄 처리
        /// </summary>
        private async Task HandleRoomClosed(Protocol protocol)
        {
            string closedRoomId = protocol.GetParam<string>("roomId");

            // 현재 있던 룸이 폐쇄된 경우
            if (isInRoom && currentRoom.HasValue && currentRoom.Value.RoomId == closedRoomId)
            {
                EmitStatusMessage($"현재 룸이 폐쇄됨: {closedRoomId}");
                currentRoom = null;
                isInRoom = false;
                OnRoomLeft?.Invoke();
            }

            // 캐시에서 제거
            cachedRoomList.RemoveAll(room => room.RoomId == closedRoomId);
            OnRoomListUpdated?.Invoke(cachedRoomList);

            await Task.CompletedTask;
        }

        /// <summary>
        /// 유저 입장 처리
        /// </summary>
        private async Task HandleUserJoined(Protocol protocol)
        {
            if (isInRoom && currentRoom.HasValue)
            {
                // 서버는 userName이 아닌 userId를 브로드캐스트합니다.
                string userIdStr = protocol.GetParam<string>("userId");
                if (string.IsNullOrEmpty(userIdStr))
                {
                    // 혹시 int로 올 경우 대비
                    int uid = protocol.GetParam<int>("userId");
                    userIdStr = uid.ToString();
                }
                int newPlayerCount = protocol.GetParam<int>("playerCount");

                EmitStatusMessage($"유저 입장: {userIdStr} (총 {newPlayerCount}명)");

                // 현재 룸 정보 업데이트
                var updatedRoom = currentRoom.Value;
                updatedRoom.PlayerCount = newPlayerCount;
                currentRoom = updatedRoom;
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// 유저 퇴장 처리
        /// </summary>
        private async Task HandleUserLeft(Protocol protocol)
        {
            if (isInRoom && currentRoom.HasValue)
            {
                // 서버는 userName이 아닌 userId를 브로드캐스트합니다.
                string userIdStr = protocol.GetParam<string>("userId");
                if (string.IsNullOrEmpty(userIdStr))
                {
                    int uid = protocol.GetParam<int>("userId");
                    userIdStr = uid.ToString();
                }
                int newPlayerCount = protocol.GetParam<int>("playerCount");
                
                EmitStatusMessage($"유저 퇴장: {userIdStr} (총 {newPlayerCount}명)");
                
                // 현재 룸 정보 업데이트
                var updatedRoom = currentRoom.Value;
                updatedRoom.PlayerCount = newPlayerCount;
                currentRoom = updatedRoom;
            }

            await Task.CompletedTask;
        }

        // --- UI 호출용 간단 래퍼 메서드들 ---
        public async void JoinLobby(int page = 0)
        {
            await RequestJoinLobbyAsync(page);
        }

        public async void RefreshRoomList()
        {
            await RefreshLobbyAsync();
        }

        public async void CreateRoom(string roomName, string mapId, bool isPrivate = false)
        {
            await RequestCreateRoomAsync(roomName, mapId, isPrivate);
        }

        public async void JoinRoom(string roomId, int slot = -1)
        {
            await RequestJoinRoomAsync(roomId, slot);
        }

        public async void LeaveRoom()
        {
            await RequestLeaveRoomAsync();
        }

        public async void SetReady(bool isReady)
        {
            await RequestReadyAsync(isReady);
        }

        // --- 공통 유틸리티 (BaseManager 대체) ---
        private void EmitStatusMessage(string message)
        {
            Debug.Log($"[RoomManager] {message}");
            OnStatusMessage?.Invoke(message);
        }

        private void EmitError(string error)
        {
            Debug.LogError($"[RoomManager] {error}");
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
                Debug.Log($"[RoomManager] 핸들러 등록됨: {protocolType}");
            }
            else
            {
                Debug.LogError($"[RoomManager] 네트워크 클라이언트가 없어 핸들러 등록 실패: {protocolType}");
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
                    Debug.Log($"[RoomManager] {operationName} 요청 중...");
                }

                NetworkResponse response = await networkClient.AsyncSend(protocol);

                if (response.isSuccess)
                {
                    if (!string.IsNullOrEmpty(operationName))
                    {
                        Debug.Log($"[RoomManager] {operationName} 성공");
                    }
                }
                else
                {
                    string errorMsg = $"{operationName} 실패: {response.resultCode}";
                    Debug.LogError($"[RoomManager] {errorMsg}");
                    OnError?.Invoke(errorMsg);
                    response.ShowResultCode();
                }

                return response;
            }
            catch (Exception e)
            {
                string errorMsg = $"{operationName} 오류: {e.Message}";
                Debug.LogError($"[RoomManager] {errorMsg}");
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
    }

    /// <summary>
    /// RoomManager 사용 예시 (에러 수정 버전)
    /// </summary>
    public class RoomManagerExample : MonoBehaviour
    {
        private void Start()
        {
            // 싱글톤으로 인스턴스 가져오기
            var roomManager = RoomManager.Instance;

            // 이벤트 등록
            roomManager.OnRoomJoinSuccess += OnRoomJoined;
            roomManager.OnRoomJoinFailure += OnRoomJoinFailed;
            roomManager.OnRoomListUpdated += OnRoomListUpdated;
            roomManager.OnRoomLeft += OnRoomLeft;
            roomManager.OnError += OnError; // BaseManager의 공통 에러 이벤트
            roomManager.OnStatusMessage += OnStatusMessage; // BaseManager의 공통 상태 이벤트
        }

        // UI에서 호출될 메서드들
        public async void JoinLobby(int page = 0)
        {
            await RoomManager.Instance.RequestJoinLobbyAsync(page);
        }

        public async void RefreshRoomList()
        {
            await RoomManager.Instance.RefreshLobbyAsync();
        }

        public async void CreateRoom(string roomName, string mapId, bool isPrivate = false)
        {
            await RoomManager.Instance.RequestCreateRoomAsync(roomName, mapId, isPrivate);
        }

        public async void JoinRoom(string roomId, int slot = -1)
        {
            await RoomManager.Instance.RequestJoinRoomAsync(roomId, slot);
        }

        public async void LeaveRoom()
        {
            await RoomManager.Instance.RequestLeaveRoomAsync();
        }

        public async void SetReady(bool isReady)
        {
            await RoomManager.Instance.RequestReadyAsync(isReady);
        }

        // 이벤트 핸들러들
        private void OnRoomJoined(RoomInfo roomInfo)
        {
            Debug.Log($"룸 참가 성공: {roomInfo}");
            // UI 업데이트
        }

        private void OnRoomJoinFailed(string reason)
        {
            Debug.LogError($"룸 참가 실패: {reason}");
            // 에러 메시지 표시
        }

        private void OnRoomListUpdated(List<RoomInfo> roomList)
        {
            Debug.Log($"룸 목록 업데이트: {roomList.Count}개");
            // 룸 목록 UI 업데이트
        }

        private void OnRoomLeft()
        {
            Debug.Log("룸을 나갔습니다");
            // 로비 UI로 전환
        }

        private void OnError(string error)
        {
            Debug.LogError($"에러: {error}");
            // 에러 팝업 표시
        }

        private void OnStatusMessage(string message)
        {
            Debug.Log($"상태: {message}");
            // 상태 메시지 UI 업데이트
        }
    }
}
