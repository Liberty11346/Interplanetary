using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CommonLib;

public class GameUIManager : MonoBehaviour
{
    [Header("Connection UI")]
    public Button connectButton;
    public Button disconnectButton;
    public TextMeshProUGUI connectionStatusText;

    [Header("Chat UI")]
    public TMP_InputField chatInputField;
    public Button sendChatButton;
    public ScrollRect chatScrollRect;
    public TextMeshProUGUI chatContentText;

    [Header("Game UI")]
    public TextMeshProUGUI resourcesText;
    public TextMeshProUGUI gameStatusText;
    public Button produceFleetButton;
    public Button leaveRoomButton;

    [Header("Fleet UI")]
    public Transform fleetListParent;
    public GameObject fleetItemPrefab;

    [Header("Planet UI")]

    private UnityGameClient _gameClient;

    private void Start()
    {
        _gameClient = FindFirstObjectByType<UnityGameClient>();

        if (_gameClient != null)
        {
            // 이벤트 구독
            _gameClient.ConnectionChanged += OnConnectionChanged;
            _gameClient.ChatReceived += OnChatReceived;
            _gameClient.ResourcesUpdated += OnResourcesUpdated;
            _gameClient.FleetSpawned += OnFleetSpawned;
            _gameClient.GameStarted += OnGameStarted;
            _gameClient.JoinRoomSuccess += OnJoinRoomSuccess;
            _gameClient.UserJoined += OnUserJoined;
            _gameClient.UserLeft += OnUserLeft;
            _gameClient.ErrorOccurred += OnError;
        }

        // UI 이벤트 설정
        if (connectButton != null)
            connectButton.onClick.AddListener(() => StartCoroutine(_gameClient.ConnectToServer()));

        if (disconnectButton != null)
            disconnectButton.onClick.AddListener(() => _gameClient.Disconnect());

        if (sendChatButton != null)
            sendChatButton.onClick.AddListener(SendChat);

        if (produceFleetButton != null)
            produceFleetButton.onClick.AddListener(() => {
                if (GameManager.Instance != null)
                {
                    // 버튼에서는 함대 타입만 전달 (임시로 0)
                    GameManager.Instance.CommandFleetSpawn(0);
                }
            });

        if (leaveRoomButton != null)
            leaveRoomButton.onClick.AddListener(() => _gameClient.LeaveRoom());

        // Enter 키로 채팅 전송
        if (chatInputField != null)
            chatInputField.onSubmit.AddListener((text) => SendChat());

        // 초기 UI 상태 설정
        UpdateConnectionUI(false);
    }

    private void OnDestroy()
    {
        if (_gameClient != null)
        {
            _gameClient.ConnectionChanged -= OnConnectionChanged;
            _gameClient.ChatReceived -= OnChatReceived;
            _gameClient.ResourcesUpdated -= OnResourcesUpdated;
            _gameClient.FleetSpawned -= OnFleetSpawned;
            _gameClient.GameStarted -= OnGameStarted;
            _gameClient.JoinRoomSuccess -= OnJoinRoomSuccess;
            _gameClient.UserJoined -= OnUserJoined;
            _gameClient.UserLeft -= OnUserLeft;
            _gameClient.ErrorOccurred -= OnError;
        }
    }

    private void Update()
    {
        // 키보드 입력 처리
        HandleKeyboardInput();
    }

    private void HandleKeyboardInput()
    {
        // 스페이스바로 함대 생산
        if (Input.GetKeyDown(KeyCode.Space) && _gameClient != null && _gameClient.IsConnected)
        {
            if (GameManager.Instance != null)
            {
                // 스페이스바 입력도 GameManager 경유, 함대 타입만 전달 (임시로 0)
                GameManager.Instance.CommandFleetSpawn(0);
            }
        }
    }

    private void OnConnectionChanged(bool connected, string message)
    {
        UpdateConnectionUI(connected);

        if (connectionStatusText != null)
        {
            connectionStatusText.text = connected ? "Connected" : "Disconnected";
            connectionStatusText.color = connected ? Color.green : Color.red;
        }

        AddChatMessage("System", message);
    }

    private void OnJoinRoomSuccess(string roomId, int playerCount)
    {
        AddChatMessage("System", $"Joined room {roomId} ({playerCount} players)");

        if (gameStatusText != null)
        {
            gameStatusText.text = $"Room: {roomId} ({playerCount}/2 players)";
            gameStatusText.color = Color.yellow;
        }
    }

    private void OnUserJoined(string userId, int playerCount)
    {
        AddChatMessage("System", $"{userId} joined the room ({playerCount} players)");

        if (gameStatusText != null)
        {
            gameStatusText.text = $"Room ({playerCount}/2 players)";
        }
    }

    private void OnUserLeft(string userId, int playerCount)
    {
        AddChatMessage("System", $"{userId} left the room ({playerCount} players)");

        if (gameStatusText != null)
        {
            gameStatusText.text = $"Room ({playerCount}/2 players)";
        }
    }

    private void OnChatReceived(ChatMessage chatMessage)
    {
        var timestamp = System.DateTimeOffset.FromUnixTimeMilliseconds(chatMessage.Timestamp).LocalDateTime;
        AddChatMessage(chatMessage.SenderId, chatMessage.Message, timestamp);
    }

    private void OnResourcesUpdated(ResourceUpdate update)
    {
        if (_gameClient != null && update.PlayerId == _gameClient.MyPlayerId && resourcesText != null)
        {
            resourcesText.text = $"Minerals: {update.Minerals:F1} | Gas: {update.Gas:F1} | Supply: {update.CurrentSupply}/{update.MaxSupply}";
        }
    }

    private void OnFleetSpawned(FleetSpawnData fleetData)
    {
        // 함대 UI 아이템 생성
        if (fleetListParent != null && fleetItemPrefab != null)
        {
            GameObject fleetItem = Instantiate(fleetItemPrefab, fleetListParent);
            FleetUIButton fleetUIButton = fleetItem.GetComponent<FleetUIButton>();
            if (fleetUIButton != null)
            {
                fleetUIButton.Initialize(fleetData);
            }
        }

        string ownerText = (_gameClient != null && fleetData.OwnerId == _gameClient.MyPlayerId) ? "Your" : "Enemy";
        AddChatMessage("System", $"{ownerText} fleet #{fleetData.FleetId} spawned at planet {fleetData.PlanetId}");
    }

    private void OnGameStarted(GameStartData gameData)
    {
        if (gameStatusText != null)
        {
            gameStatusText.text = $"Game Started! (ID: {gameData.GameId})";
            gameStatusText.color = Color.green;
        }

        AddChatMessage("System", $"Game started! Your Player ID: {_gameClient.MyPlayerId}");
    }

    private void OnError(string errorMessage)
    {
        AddChatMessage("Error", errorMessage);
    }

    private void UpdateConnectionUI(bool connected)
    {
        if (connectButton != null)
            connectButton.interactable = !connected;

        if (disconnectButton != null)
            disconnectButton.interactable = connected;

        if (sendChatButton != null)
            sendChatButton.interactable = connected;

        if (produceFleetButton != null)
            produceFleetButton.interactable = connected;

        if (leaveRoomButton != null)
            leaveRoomButton.interactable = connected;
    }



    private void AddChatMessage(string sender, string message, System.DateTime? timestamp = null)
    {
        if (chatContentText == null) return;

        var time = timestamp ?? System.DateTime.Now;
        string _chatContent = $"[{time:HH:mm:ss}] {sender}: {message}\n";
        chatContentText.text = _chatContent;

        // 스크롤을 맨 아래로
        if (chatScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void SendChat()
    {
        if (chatInputField == null || _gameClient == null) return;

        string message = chatInputField.text.Trim();
        if (!string.IsNullOrEmpty(message) && _gameClient.IsConnected)
        {
            _gameClient.SendChatMessage(message);
            chatInputField.text = "";
            chatInputField.ActivateInputField();
        }
    }

    // 공개 메서드들


    public void SetSelectedFleet(int fleetId)
    {
        // 선택된 함대 정보 업데이트 (필요시 UI 요소 추가)
        Debug.Log($"Fleet {fleetId} selected in UI");
    }
}
