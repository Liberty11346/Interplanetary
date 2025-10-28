using UnityEngine;
using CommonLib;
using System.Collections.Generic;


/// <summary>
/// 
/// 
/// 게임 매니저 - 게임 상태 관리 및 이벤트 핸들링을 담당
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public bool autoStartGame = true;
    public float gameTickRate = 1f; // 초당 틱 수

    [Header("Managers")]
    public UnityGameClient gameClient;      // 서버 통신 담당
    public GameUIManager uiManager;         // UI 관리 담당
    public UnifiedVisualizationManager visualizationManager;  // 시각화 담당

    [Header("Game State")]
    public bool isGameStarted = false;
    public int myPlayerId = -1;
    public float gameTime = 0f;

    private long _currentTick = 0;
    private float _tickTimer = 0f;

    // 게임 데이터 캐싱
    private Dictionary<int, PlanetData> _planetDataCache = new Dictionary<int, PlanetData>();
    private Dictionary<int, FleetData> _fleetDataCache = new Dictionary<int, FleetData>();
    private Dictionary<int, ResourceData> _playerResourceCache = new Dictionary<int, ResourceData>();

    private void Start()
    {
        InitializeGame();
    }

    private void Update()
    {
        if (isGameStarted)
        {
            UpdateGameTick();
            gameTime += Time.deltaTime;
        }

        HandleDebugInput();
    }

    /// <summary>
    /// 게임 초기화 - 컴포넌트 참조 및 이벤트 구독 설정
    /// </summary>
    private void InitializeGame()
    {
        // 컴포넌트 자동 찾기
        if (gameClient == null)
            gameClient = FindFirstObjectByType<UnityGameClient>();

        if (uiManager == null)
            uiManager = FindFirstObjectByType<GameUIManager>();

        if (visualizationManager == null)
            visualizationManager = FindFirstObjectByType<UnifiedVisualizationManager>();

        // 게임 클라이언트 이벤트 구독
        if (gameClient != null)
        {
            // 게임 상태 이벤트
            gameClient.GameStarted += OnGameStarted;
            gameClient.GameEnded += OnGameEnded;
            gameClient.ConnectionChanged += OnConnectionChanged;
            gameClient.ErrorOccurred += OnErrorOccurred;

            // 게임 데이터 이벤트
            gameClient.ResourcesUpdated += OnResourcesUpdated;
            gameClient.FleetSpawned += OnFleetSpawned;
            gameClient.FleetMoving += OnFleetMoving;
            gameClient.ChatReceived += OnChatReceived;
        }

        Debug.Log("GameManager initialized - 이벤트 핸들러 설정 완료");
    }

    /// <summary>
    /// 게임 틱 업데이트 - 일정 간격으로 게임 로직 처리
    /// </summary>
    private void UpdateGameTick()
    {
        _tickTimer += Time.deltaTime;

        if (_tickTimer >= 1f / gameTickRate)
        {
            _tickTimer = 0f;
            _currentTick++;

            // 틱 기반 게임 로직 처리
            ProcessGameTick(_currentTick);
        }
    }

    /// <summary>
    /// 게임 틱 처리 - 주기적인 게임 로직 실행
    /// </summary>
    private void ProcessGameTick(long tick)
    {
        // 게임 틱마다 실행되는 로직
        // 예: 자원 생산, 함대 이동 진행 등

        if (tick % 10 == 0) // 10틱마다 (10초마다)
        {
            Debug.Log($"Game Tick: {tick}, Game Time: {gameTime:F1}s");

            // 필요시 주기적인 데이터 동기화 요청
            // SyncGameState();
        }
    }

    /// <summary>
    /// 디버그 입력 처리
    /// </summary>
    private void HandleDebugInput()
    {
        // 디버그 키 입력 처리
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ToggleDebugInfo();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (gameClient != null && gameClient.IsConnected)
            {
                gameClient.SendChatMessage("Debug message from GameManager");
            }
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            PrintGameState();
        }
    }

    #region 이벤트 핸들러 - 서버로부터 받은 이벤트 처리

    /// <summary>
    /// 게임 시작 이벤트 처리 - 초기 게임 상태 설정
    /// </summary>
    private void OnGameStarted(GameStartData gameData)
    {
        isGameStarted = true;
        myPlayerId = gameClient != null ? gameClient.MyPlayerId : -1;
        gameTime = 0f;
        _currentTick = 0;
        _tickTimer = 0f;

        Debug.Log($"Game started! My Player ID: {myPlayerId}");

        // 게임 데이터 초기화
        InitializeGameData(gameData);

        // 시각화 초기화 (행성 및 함대 생성)
        InitializeVisualization(gameData);

        // 게임 시작 시 초기 설정
        SetupInitialGameState();
    }

    /// <summary>
    /// 게임 데이터 초기화 - 게임 시작 시 받은 데이터로 캐시 초기화
    /// </summary>
    private void InitializeGameData(GameStartData gameData)
    {
        // 캐시 초기화
        _planetDataCache.Clear();
        _fleetDataCache.Clear();
        _playerResourceCache.Clear();

        // 게임 데이터가 있으면 캐싱
        if (gameData.Planets != null)
        {
            foreach (var planet in gameData.Planets)
            {
                _planetDataCache[planet.PlanetId] = planet;
            }
        }

        Debug.Log($"Game data initialized with {_planetDataCache.Count} planets");
    }

    /// <summary>
    /// 시각화 초기화 - 게임 시작 시 시각적 요소 초기화
    /// </summary>
    private void InitializeVisualization(GameStartData gameData)
    {
        if (visualizationManager != null)
        {
            // 테스트용 행성은 이미 UnifiedVisualizationManager의 Start에서 생성됨
            // 실제 게임에서는 여기서 행성 데이터를 기반으로 시각화 요청

            // 예: 실제 게임 데이터가 있는 경우 행성 생성
            /*
            if (gameData != null && gameData.Planets != null)
            {
                foreach (var planet in gameData.Planets)
                {
                    visualizationManager.CreatePlanet(planet.PlanetId, planet.Position);
                    visualizationManager.UpdatePlanetOwnership(planet.PlanetId, planet.OwnerId);
                }
            }
            */

            Debug.Log("Game visualization initialized");
        }
    }

    /// <summary>
    /// 게임 종료 이벤트 처리
    /// </summary>
    private void OnGameEnded(GameEndData gameEndData)
    {
        isGameStarted = false;

        Debug.Log($"Game ended! Winner: {gameEndData.WinnerId}, Duration: {gameEndData.GameDuration}ms");

        // 게임 종료 처리
        HandleGameEnd(gameEndData);
    }

    /// <summary>
    /// 연결 상태 변경 이벤트 처리
    /// </summary>
    private void OnConnectionChanged(bool connected, string message)
    {
        if (!connected && isGameStarted)
        {
            // 연결이 끊어지면 게임 일시정지
            isGameStarted = false;
            Debug.LogWarning("Connection lost during game!");

            // UI 업데이트
            if (uiManager != null)
            {
                // uiManager.ShowConnectionLostMessage(message);
            }
        }
    }

    /// <summary>
    /// 에러 발생 이벤트 처리
    /// </summary>
    private void OnErrorOccurred(string errorMessage)
    {
        Debug.LogError($"Game Error: {errorMessage}");

        // 심각한 에러인 경우 게임 중단
        if (errorMessage.Contains("Fatal") || errorMessage.Contains("Critical"))
        {
            isGameStarted = false;

            // UI 업데이트
            if (uiManager != null)
            {
                // uiManager.ShowErrorMessage(errorMessage);
            }
        }
    }

    /// <summary>
    /// 자원 업데이트 이벤트 처리 - 자원 데이터 캐싱 및 시각화 요청
    /// </summary>
    private void OnResourcesUpdated(ResourceUpdate resourceData)
    {
        // 자원 데이터 캐싱
        if (!_playerResourceCache.ContainsKey(resourceData.PlayerId))
        {
            _playerResourceCache[resourceData.PlayerId] = new ResourceData();
        }

        // 자원 데이터 업데이트
        var playerResources = _playerResourceCache[resourceData.PlayerId];
        playerResources.Minerals = (int)resourceData.Minerals;
        playerResources.Gas = (int)resourceData.Gas;
        playerResources.CurrentSupply = resourceData.CurrentSupply;
        playerResources.MaxSupply = resourceData.MaxSupply;

        // UI 업데이트 (내 자원인 경우)
        if (resourceData.PlayerId == myPlayerId && uiManager != null)
        {
            // uiManager.UpdateResourceDisplay(playerResources);
        }

        // 시각화 업데이트 - 행성 소유권 변경
        if (visualizationManager != null)
        {
            // ResourceUpdate에는 PlanetId와 OwnerId가 없으므로 플레이어 ID만 전달
            // 실제 게임에서는 플레이어 ID와 행성 ID 매핑이 필요함
            visualizationManager.UpdatePlanetOwnership(resourceData.PlayerId, resourceData.PlayerId);
        }

        Debug.Log($"Resources updated for player {resourceData.PlayerId}: Minerals={resourceData.Minerals}, Gas={resourceData.Gas}");
    }

    /// <summary>
    /// 함대 생성 이벤트 처리 - 함대 데이터 캐싱 및 시각화 요청
    /// </summary>
    private void OnFleetSpawned(FleetSpawnData fleetData)
    {
        // 함대 데이터 캐싱
        _fleetDataCache[fleetData.FleetId] = new FleetData
        {
            FleetId = fleetData.FleetId,
            OwnerId = fleetData.OwnerId,
            FleetType = fleetData.FleetType,
            CurrentPlanetId = fleetData.PlanetId
        };

        // 시각화 요청 - 함대 생성
        if (visualizationManager != null)
        {
            visualizationManager.CreateFleet(fleetData.FleetId, fleetData.PlanetId, fleetData.FleetType, fleetData.OwnerId);
        }

        Debug.Log($"Fleet {fleetData.FleetId} spawned at planet {fleetData.PlanetId} by player {fleetData.OwnerId}");
    }

    /// <summary>
    /// 함대 이동 이벤트 처리 - 함대 데이터 업데이트 및 시각화 요청
    /// </summary>
    private void OnFleetMoving(FleetMoveData moveData)
    {
        // 함대 데이터 업데이트
        if (_fleetDataCache.TryGetValue(moveData.FleetId, out FleetData fleetData))
        {
            fleetData.CurrentPlanetId = moveData.ToPlanetId;
        }

        // 시각화 요청 - 함대 이동 애니메이션
        if (visualizationManager != null)
        {
            visualizationManager.AnimateFleetMovement(moveData.FleetId, moveData.FromPlanetId, moveData.ToPlanetId);
        }

        Debug.Log($"Fleet {moveData.FleetId} moving from planet {moveData.FromPlanetId} to {moveData.ToPlanetId}");
    }

    /// <summary>
    /// 채팅 메시지 수신 이벤트 처리
    /// </summary>
    private void OnChatReceived(ChatMessage chatMessage)
    {
        // 채팅 메시지 처리 및 UI 업데이트
        if (uiManager != null)
        {
            // uiManager.AddChatMessage(chatMessage);
        }

        Debug.Log($"Chat from {chatMessage.SenderId}: {chatMessage.Message}");
    }

    #endregion

    /// <summary>
    /// 초기 게임 상태 설정
    /// </summary>
    private void SetupInitialGameState()
    {
        // 게임 시작 시 초기 상태 설정
        Debug.Log("Setting up initial game state...");

        // UI 초기화
        if (uiManager != null)
        {
            // uiManager.SetupGameUI(myPlayerId);
        }

        Debug.Log("Initial game state setup completed");
    }

    /// <summary>
    /// 게임 종료 처리
    /// </summary>
    private void HandleGameEnd(GameEndData gameEndData)
    {
        // 게임 종료 처리
        bool isWinner = gameEndData.WinnerId == myPlayerId;

        string resultMessage = isWinner ? "Victory!" : "Defeat!";
        Debug.Log($"Game Result: {resultMessage}");

        // UI에 결과 표시
        if (uiManager != null)
        {
            // uiManager.ShowGameResult(isWinner, gameEndData);
        }

        // 종료 메시지 전송
        if (gameClient != null && gameClient.IsConnected)
        {
            gameClient.SendChatMessage($"GG! {resultMessage}");
        }
    }

    /// <summary>
    /// 디버그 정보 토글
    /// </summary>
    private void ToggleDebugInfo()
    {
        // 디버그 정보 표시 토글
        Debug.Log("Debug info toggled");

        // UI 디버그 정보 토글
        if (uiManager != null)
        {
            // uiManager.ToggleDebugPanel();
        }
    }

    /// <summary>
    /// 현재 게임 상태 출력
    /// </summary>
    private void PrintGameState()
    {
        Debug.Log($"=== Game State ===");
        Debug.Log($"Game Started: {isGameStarted}");
        Debug.Log($"My Player ID: {myPlayerId}");
        Debug.Log($"Current Tick: {_currentTick}");
        Debug.Log($"Game Time: {gameTime:F1}s");
        Debug.Log($"Connected: {(gameClient != null ? gameClient.IsConnected : false)}");
        Debug.Log($"Planets: {_planetDataCache.Count}, Fleets: {_fleetDataCache.Count}");
        Debug.Log($"==================");
    }

    #region 공개 메서드 - 외부에서 호출 가능한 인터페이스

    /// <summary>
    /// 게임 시작 요청
    /// </summary>
    public void StartGame()
    {
        // 임시
        if (gameClient != null && gameClient.IsConnected)
        {
            Debug.Log("Requesting game start...");
        }
        else
        {
            Debug.LogWarning("Cannot start game: not connected to server");
        }
    }

    /// <summary>
    /// 게임 종료 요청
    /// </summary>
    public void EndGame()
    {
        if (isGameStarted)
        {
            isGameStarted = false;
            Debug.Log("Game ended by user");

            if (gameClient != null)
            {
                gameClient.LeaveRoom();
            }
        }
    }

    /// <summary>
    /// 게임 재시작 요청 
    /// </summary>
    public void RestartGame()
    {
        EndGame();

        // 잠시 후 다시 연결 시도
        Invoke(nameof(ReconnectToServer), 2f);
    }

    /// <summary>
    /// 서버 재연결 시도
    /// </summary>
    private void ReconnectToServer()
    {
        if (gameClient != null)
        {
            StartCoroutine(gameClient.ConnectToServer());
        }
    }

    /// <summary>
    /// 현재 게임 틱 반환
    /// </summary>
    public long GetCurrentTick()
    {
        return _currentTick;
    }

    /// <summary>
    /// 현재 게임 시간 반환
    /// </summary>
    public float GetGameTime()
    {
        return gameTime;
    }

    /// <summary>
    /// 현재 플레이어 턴 여부 확인
    /// </summary>
    public bool IsMyTurn()
    {
        // 턴 기반 게임인 경우 사용
        return true; // 실시간 게임이므로 항상 true
    }

    /// <summary>
    /// 함대 이동 요청 - UI나 입력에서 호출
    /// </summary>
    public void CommandFleetMovement(int fleetId, int targetPlanetId)
    {
        if (gameClient != null && gameClient.IsConnected && isGameStarted)
        {
            // 서버에 함대 이동 요청
            gameClient.RequestMoveFleet(fleetId, targetPlanetId);
            Debug.Log($"Requesting fleet {fleetId} to move to planet {targetPlanetId}");
        }
    }

    /// <summary>
    /// 함대 생성 요청 - UI나 입력에서 호출
    /// </summary>
    public void CommandFleetSpawn(int planetId, int fleetType)
    {
        if (gameClient != null && gameClient.IsConnected && isGameStarted)
        {
            // 서버에 함대 생성 요청
            gameClient.RequestProduceFleet(planetId);
            Debug.Log($"Requesting fleet spawn of type {fleetType} at planet {planetId}");

            // 실제 함대 생성 및 검증은 서버에서 처리
            // 서버는 자원 확인, 생산 가능 여부 등을 검증하고
            // 승인/거절 결과를 FleetSpawned 이벤트나 Error 이벤트로 응답
        }
    }

    #endregion

    /// <summary>
    /// 컴포넌트 제거 시 이벤트 구독 해제
    /// </summary>
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (gameClient != null)
        {
            gameClient.GameStarted -= OnGameStarted;
            gameClient.GameEnded -= OnGameEnded;
            gameClient.ConnectionChanged -= OnConnectionChanged;
            gameClient.ErrorOccurred -= OnErrorOccurred;




            gameClient.ResourcesUpdated -= OnResourcesUpdated;
            gameClient.FleetSpawned -= OnFleetSpawned;
            gameClient.FleetMoving -= OnFleetMoving;
            gameClient.ChatReceived -= OnChatReceived;
        }
    }
}

/// <summary>
/// 내부 데이터 캐싱을 위한 클래스들
/// </summary>
public class ResourceData
{
    public int Minerals;
    public int Gas;
    public int CurrentSupply;
    public int MaxSupply;
}

public class FleetData
{
    public int FleetId;
    public int OwnerId;
    public int FleetType;
    public int CurrentPlanetId;
}
