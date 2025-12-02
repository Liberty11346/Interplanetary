using UnityEngine;
using CommonLib;
using Vector2 = UnityEngine.Vector2;
using CommonVector2 = CommonLib.Vector2;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;


/// <summary>
/// 
/// 
/// 게임 매니저 - 게임 상태 관리 및 이벤트 핸들링을 담당
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public bool autoStartGame = true;
    public float gameTickRate = 1f; // 초당 틱 수
    public int debugMapId = 1; // 디버그용 맵 ID (F5로 로드)
    public int selectedMapId = 1; // 로드할 맵 번호

    [Header("Managers")]
    private GamePlayManager gamePlayManager;    // 게임 플레이 및 서버 통신 담당
    public GameUIManager uiManager;             // UI 관리 담당
    public VisualizationManager visualizationManager;  // 시각화 담당

    [Header("Game State")]
    public bool isGameStarted = false;
    public int myPlayerId = -1;
    public int MyHomePlanetId { get; private set; } = -1;
    public float gameTime = 0f;

    [Header("Selection State")]
    public int selectedPlanetId = -1;
    public int selectedFleetId = -1;

    public event Action<int> OnPlanetSelected;
    public event Action<int> OnFleetSelected;

    private long _currentTick = 0;
    private float _tickTimer = 0f;

    // 게임 데이터 캐싱
    private Dictionary<int, PlanetData> _planetDataCache = new Dictionary<int, PlanetData>();
    private Dictionary<int, FleetData> _fleetDataCache = new Dictionary<int, FleetData>();
    private Dictionary<int, ResourceData> _playerResourceCache = new Dictionary<int, ResourceData>();
    private Dictionary<int, int> _homePlanetByPlayer = new Dictionary<int, int>(); // 플레이어별 모성 ID 저장

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

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
        // GamePlayManager 싱글톤 참조
        gamePlayManager = GamePlayManager.Instance;

        // 컴포넌트 자동 찾기
        if (uiManager == null)
            uiManager = FindFirstObjectByType<GameUIManager>();

        if (visualizationManager == null)
            visualizationManager = FindFirstObjectByType<VisualizationManager>();

        Debug.Log($"[GameManager] gamePlayManager: {(gamePlayManager != null ? "찾음" : "없음")}, uiManager: {(uiManager != null ? "찾음" : "없음")}, visualizationManager: {(visualizationManager != null ? "찾음" : "없음")}");

        // GamePlayManager 이벤트 구독
        if (gamePlayManager != null)
        {
            // 게임 상태 이벤트
            gamePlayManager.GameStarted += OnGameStarted;
            gamePlayManager.GameEnded += OnGameEnded;
            gamePlayManager.OnError += OnErrorOccurred;

            // 게임 데이터 이벤트
            gamePlayManager.ResourcesUpdated += OnResourcesUpdated;
            gamePlayManager.FleetSpawned += OnFleetSpawned;
            gamePlayManager.FleetMoving += OnFleetMoving;
            gamePlayManager.ChatMessageReceived += OnChatReceived;
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
            if (gamePlayManager != null)
            {
                gamePlayManager.SendChat("Debug message from GameManager");
            }
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            PrintGameState();
        }

        // F5: debugMapId에 설정된 맵을 로드 (Inspector에서 변경 가능)
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Debug.Log($"F5: 서버에서 맵 {debugMapId} 요청");
            LoadMap(debugMapId);
        }
    }

    #region 이벤트 핸들러 - 서버로부터 받은 이벤트 처리

    /// <summary>
    /// 게임 시작 이벤트 처리 - 초기 게임 상태 설정
    /// </summary>
    private void OnGameStarted(GameStartData gameData)
    {
        isGameStarted = true;
        mapLoadRequested = false; // 다음 게임을 위해 초기화
        myPlayerId = gamePlayManager != null ? gamePlayManager.MyPlayerId : -1;
        if (myPlayerId == -1 && !string.IsNullOrEmpty(gameData.PlayersJson))
        {
            try
            {
                var players = JsonConvert.DeserializeObject<PlayerData[]>(gameData.PlayersJson);
                if (players != null && players.Length > 0)
                {
                    myPlayerId = players[0].PlayerId;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to parse PlayersJson for myPlayerId: {ex.Message}");
            }
        }
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
        _homePlanetByPlayer.Clear();
        MyHomePlanetId = -1; // 리셋

        // 게임 데이터가 있으면 캐싱
        if (gameData.Planets != null)
        {
            foreach (var planet in gameData.Planets)
            {
                _planetDataCache[planet.PlanetId] = planet;

                // IsHomePlanet 플래그로 모성 저장
                //if (planet.IsHomePlanet)
                {
                    //_homePlanetByPlayer[planet.OwnerId] = planet.PlanetId;
                    
                    // 내 모성이면 MyHomePlanetId도 설정
                    //if (planet.OwnerId == myPlayerId)
                    {
                        //MyHomePlanetId = planet.PlanetId;
                        //Debug.Log($"My home planet is set to: {MyHomePlanetId}");
                    }
                }
            }
        }

        Debug.Log($"Game data initialized with {_planetDataCache.Count} planets and {_homePlanetByPlayer.Count} home planets");
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
            if (gameData.Planets != null)
            {
                foreach (var planet in gameData.Planets)
                {
                    visualizationManager.CreatePlanetWithData(planet.PlanetId, 
                        new UnityEngine.Vector3(planet.Position.X, planet.Position.Y, 0), 
                        planet);
                }
            }

            // 행성 간 연결선(경로) 그리기
            if (gameData.Routes != null && gameData.Routes.Length > 0)
            {
                visualizationManager.CreatePlanetEdges(gameData.Routes);
                Debug.Log($"행성 간 연결선 {gameData.Routes.Length}개 그려짐");
            }

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

        // 시각화 요청 - 함대 생성 (통일성: planetId 포함)
        if (visualizationManager != null)
        {
            visualizationManager.CreateFleet(fleetData.FleetId, fleetData.FleetType, fleetData.OwnerId, fleetData.PlanetId);
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

        // 모든 초기화가 완료되었으므로 서버에 준비 완료 신호 전송
        if (gamePlayManager != null)
        {
            gamePlayManager.RequestGameClientReady();
        }
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
        if (gamePlayManager != null)
        {
            gamePlayManager.SendChatMessage($"GG! {resultMessage}");
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
        Debug.Log($"GamePlayManager: {(gamePlayManager != null ? "Available" : "Not Available")}");
        Debug.Log($"Planets: {_planetDataCache.Count}, Fleets: {_fleetDataCache.Count}");
        Debug.Log($"==================");
    }

    #region 공개 메서드 - 외부에서 호출 가능한 인터페이스

    public void SelectPlanet(int planetId)
    {
        // 함대가 먼저 선택되었는지 확인
        if (selectedFleetId != -1)
        {
            // 함대가 선택된 상태 -> 이동 명령 실행
            Debug.Log($"Fleet {selectedFleetId} is selected. Issuing move command to planet {planetId}.");

            // 이동 명령 요청
            CommandFleetMovement(selectedFleetId, planetId);

            // 명령 후 선택 상태 초기화
            selectedFleetId = -1;
            selectedPlanetId = -1;
            OnFleetSelected?.Invoke(selectedFleetId);
            OnPlanetSelected?.Invoke(selectedPlanetId);
        }
        else
        {
            // 함대가 선택되지 않은 상태 -> 단순 행성 선택
            selectedPlanetId = planetId;
            OnPlanetSelected?.Invoke(selectedPlanetId);
        }
    }

    public void SelectFleet(int fleetId)
    {
        // 함대 선택은 항상 이동 명령의 시작점.
        // 이전에 선택된 행성이 있다면 무시하고, 함대만 선택된 상태로 만든다.
        selectedFleetId = fleetId;
        selectedPlanetId = -1; // 행성 선택 초기화

        OnFleetSelected?.Invoke(selectedFleetId);
        OnPlanetSelected?.Invoke(selectedPlanetId); // 행성 선택 해제 알림
        Debug.Log($"Fleet {fleetId} selected. Ready for move command.");
    }



    /// <summary>
    /// 게임 시작 요청
    /// </summary>
    public void StartGame()
    {
        // 임시
        if (gamePlayManager != null)
        {
            Debug.Log("Requesting game start...");
        }
        else
        {
            Debug.LogWarning("Cannot start game: GamePlayManager not available");
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

            if (gamePlayManager != null)
            {
                gamePlayManager.LeaveRoom();
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
        // 연결은 ClientServerHandler에서 관리
        Debug.Log("Reconnection is handled by ClientServerHandler");
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
    /// 특정 플레이어의 모성 ID를 반환합니다.
    /// </summary>
    public int GetHomePlanetId(int playerId)
    {
        if (_homePlanetByPlayer.TryGetValue(playerId, out int homePlanetId))
        {
            return homePlanetId;
        }
        return -1; // 찾지 못하면 -1 반환
    }

    /// <summary>
    /// 에디터 디버그용: 행성 정보를 캐시에 등록하고, 첫 행성이면 모성으로 설정
    /// </summary>
    public void EditorRegisterPlanet(int planetId, int ownerId, UnityEngine.Vector2 position)
    {
        // 캐시 업데이트
        _planetDataCache[planetId] = new PlanetData
        {
            PlanetId = planetId,
            OwnerId = ownerId,
            Position = new CommonLib.Vector2(position.x, position.y),
            Minerals = 0,
            Gas = 0
        };

        // 내 플레이어의 첫 행성이면 모성 등록
        if (ownerId == myPlayerId && MyHomePlanetId == -1)
        {
            MyHomePlanetId = planetId;
            Debug.Log($"My home planet registered (editor): {MyHomePlanetId}");
        }
    }

    /// <summary>
    /// 함대 이동 요청 - UI나 입력에서 호출
    /// </summary>
    public void CommandFleetMovement(int fleetId, int targetPlanetId)
    {
        if (gamePlayManager != null && isGameStarted)
        {
            // 서버에 함대 이동 요청
            gamePlayManager.RequestMoveFleet(fleetId, targetPlanetId);
            Debug.Log($"Requesting fleet {fleetId} to move to planet {targetPlanetId}");
        }
    }

    /// <summary>
    /// 함대 생성 요청 - UI 버튼에서 함대 타입만 전달
    /// 행성 ID는 플레이어의 모성(MyHomePlanetId)으로 고정
    /// </summary>
    public void CommandFleetSpawn(int fleetType)
    {
        if (gamePlayManager != null && isGameStarted)
        {
            int planetId = MyHomePlanetId;
            if (planetId == -1)
            {
                Debug.LogWarning("Cannot spawn fleet: MyHomePlanetId is invalid (-1)");
                return;
            }

            gamePlayManager.RequestProduceFleet(planetId, fleetType);
            Debug.Log($"Requesting fleet spawn at home planet {planetId} of type {fleetType}");

            // 실제 함대 생성 및 검증은 서버에서 처리
            // 서버는 자원 확인, 생산 가능 여부 등을 검증하고
            // 승인/거절 결과를 FleetSpawned 이벤트나 Error 이벤트로 응답
        }
    }

    #endregion

    /// <summary>
    /// F5: 서버에게 맵(게임 시작)을 요청하는 흐름
    /// - 방이 없으면 생성하고 자동 입장
    /// - 방에 입장되면 READY(true)로 서버에 시작 요청
    /// - 서버가 GAME_STARTED 브로드캐스트를 보내면 기존 OnGameStarted로 처리됨
    /// </summary>
    private void RequestMapFromServer()
    {
        // 기존 동작을 유지: 기본 맵 id=1로 로드합니다.
        LoadMap(1);
    }

    /// <summary>
    /// 로컬 맵 로드 - 서버에서 맵 데이터를 받아옴
    /// </summary>
    private bool mapLoadRequested = false;
    
    public void LoadMapLocal(int mapId)
    {
        // 한 번만 요청하도록 가드
        if (mapLoadRequested)
        {
            Debug.Log($"[GameManager] LoadMapLocal: map load already requested, skipping");
            return;
        }

        Debug.Log($"[GameManager] LoadMapLocal: requesting map {mapId} from server");

        if (gamePlayManager == null)
        {
            Debug.LogError("[GameManager] LoadMapLocal: GamePlayManager not available. Cannot load map.");
            return;
        }

        mapLoadRequested = true;
        // LoadMap 메서드 호출 - 서버에서 맵을 받아옴
        LoadMap(mapId);
    }


    /// <summary>
    /// 메인 게임씬에서 사용될 맵 로드 메서드
    /// - 서버에 연결되어 있으면 룸 생성/입장 흐름을 이용해 맵(GameStartData)을 요청합니다.
    /// - 서버가 GAME_STARTED 브로드캐스트를 보내면 OnGameStarted로 처리됩니다.
    /// </summary>
    public async void LoadMap(int mapId)
    {
        if (gamePlayManager == null)
        {
            Debug.LogError($"❌ [LoadMap] GamePlayManager가 null입니다. 맵을 로드할 수 없습니다.");
            return;
        }

        Debug.Log($"[LoadMap] 서버에서 맵 {mapId} 요청 중...");

        // 임시/디버그 목적의 방 이름 (짧은 GUID로 충돌 가능성 낮춤)
        string roomName = $"MapLoadRoom_{mapId}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
        bool isPrivate = true;

        // 방 생성
        bool created = await gamePlayManager.CreateRoom(roomName, mapId, isPrivate);
        if (created)
        {
            Debug.Log($"✅ 룸 생성 성공: {roomName} → READY 요청");
            await gamePlayManager.RequestReady(true);
        }
        else
        {
            Debug.LogError($"❌ [LoadMap] 방 생성 실패");
        }
    }

    /// <summary>
    /// 컴포넌트 제거 시 이벤트 구독 해제
    /// </summary>
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (gamePlayManager != null)
        {
            gamePlayManager.GameStarted -= OnGameStarted;
            gamePlayManager.GameEnded -= OnGameEnded;
            gamePlayManager.OnError -= OnErrorOccurred;

            gamePlayManager.ResourcesUpdated -= OnResourcesUpdated;
            gamePlayManager.FleetSpawned -= OnFleetSpawned;
            gamePlayManager.FleetMoving -= OnFleetMoving;
            gamePlayManager.ChatMessageReceived -= OnChatReceived;
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
