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

    // === 정적 데이터 (GameSet에서 1회 수신) ===
    private Dictionary<int, PlanetStaticData> _planetStaticData = new Dictionary<int, PlanetStaticData>();
    public GameStartData _gameStartData;

    // === 동적 데이터 (GameState에서 매 틱 수신) ===
    private GamePlayManager.GameState _currentState;
    private GamePlayManager.GameState _previousState;

    // === 엔티티 추적 ===
    private HashSet<long> _spawnedFleetIds = new HashSet<long>();
    private Dictionary<int, int> _planetOwners = new Dictionary<int, int>(); // planetId -> ownerId

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
            // ⭐ State-Sync: GameState 수신 이벤트 (핵심)
            gamePlayManager.GameStateReceived += OnGameStateReceived;

            // 게임 시작/종료 이벤트
            gamePlayManager.GameStarted += OnGameStarted;
            gamePlayManager.GameEnded += OnGameEnded;

            // 중요 이벤트 (효과, 사운드용)
            gamePlayManager.FleetSpawned += OnFleetSpawned;

            // 에러 처리
            gamePlayManager.OnError += OnErrorOccurred;
        }

        Debug.Log("GameManager initialized - State-Sync 모델 활성화");
        gamePlayManager.GameStarted?.Invoke(gamePlayManager.GameStartData.Value);
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
    /// 게임 데이터 초기화 - 정적 맵 데이터만 초기화 (GameSet)
    /// </summary>
    private void InitializeGameData(GameStartData gameData)
    {
        // 정적 데이터 초기화
        _planetStaticData.Clear();
        _spawnedFleetIds.Clear();
        _planetOwners.Clear();
        _currentState = null;
        _previousState = null;
        MyHomePlanetId = -1;

        // 행성 정적 데이터 저장 (위치는 변하지 않음)
        if (gameData.Planets != null)
        {
            foreach (var planet in gameData.Planets)
            {
                _planetStaticData[planet.PlanetId] = new PlanetStaticData
                {
                    PlanetId = planet.PlanetId,
                    Position = new Vector2(planet.Position.X, planet.Position.Y),
                    MaxMinerals = (int)planet.Minerals,
                    MaxGas = (int)planet.Gas
                };

                // 내 모성 찾기 (첫 번째로 소유한 행성)
                if (planet.OwnerId == myPlayerId && MyHomePlanetId == -1)
                {
                    MyHomePlanetId = planet.PlanetId;
                    Debug.Log($"My home planet: {MyHomePlanetId}");
                }
            }
        }

        Debug.Log($"Static map data initialized with {_planetStaticData.Count} planets");
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
    /// ⭐ State-Sync 모델에서는 OnGameStateReceived에서 처리하므로 사용 안 함
    /// </summary>
    private void OnResourcesUpdated(ResourceUpdate resourceData)
    {
        // State-Sync 모델에서는 사용하지 않음 (레거시 코드)
        Debug.Log($"[Legacy] Resources updated for player {resourceData.PlayerId}: Minerals={resourceData.Minerals}, Gas={resourceData.Gas}");
    }

    /// <summary>
    /// 함대 생성 이벤트 처리 - 효과 및 사운드용
    /// ⭐ State-Sync 모델에서는 함대 생성/파괴는 OnGameStateReceived에서 처리
    /// 이 이벤트는 생성 효과/사운드만 담당
    /// </summary>
    private void OnFleetSpawned(FleetSpawnData fleetData)
    {
        // 생성 효과/사운드 재생
        Debug.Log($"[Effect] Fleet {fleetData.FleetId} spawned at planet {fleetData.PlanetId} by player {fleetData.OwnerId}");

        // TODO: PlayFleetSpawnEffect(fleetData.PlanetId);
    }

    /// <summary>
    /// 함대 이동 이벤트 처리 - 레거시 이벤트 (State-Sync에서는 사용 안 함)
    /// </summary>
    private void OnFleetMoving(FleetMoveData moveData)
    {
        // State-Sync 모델에서는 사용하지 않음
        Debug.Log($"[Legacy] Fleet {moveData.FleetId} moving from planet {moveData.FromPlanetId} to {moveData.ToPlanetId}");
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

    #region State-Sync 핸들러

    /// <summary>
    /// ⭐ GameState 수신 핸들러 (매 틱마다 호출 - 50ms)
    /// </summary>
    private void OnGameStateReceived(GamePlayManager.GameState newState, long tick)
    {
        if (newState == null) return;

        _previousState = _currentState;
        _currentState = newState;
        _currentTick = tick;

        // 순서 중요: 함대 → 행성 → 자원
        SyncFleets(tick);
        SyncPlanets(tick);
        SyncResources(tick);
        CheckGameState(tick);
    }

    /// <summary>
    /// 함대 동기화: 생성, 이동, 파괴 감지
    /// </summary>
    private void SyncFleets(long tick)
    {
        var currentFleetIds = new HashSet<long>();

        foreach (var player in _currentState.players)
        {
            if (player.fleets == null) continue;

            foreach (var fleetInfo in player.fleets)
            {
                currentFleetIds.Add(fleetInfo.fleetId);

                // === 새로 생성된 함대 ===
                if (!_spawnedFleetIds.Contains(fleetInfo.fleetId))
                {
                    int spawnPlanetId = FindClosestPlanet(new Vector2(fleetInfo.position.X, fleetInfo.position.Y));

                    if (visualizationManager != null)
                    {
                        visualizationManager.CreateFleet(
                            (int)fleetInfo.fleetId,
                            fleetInfo.fleetType,
                            fleetInfo.ownerId,
                            spawnPlanetId
                        );
                    }

                    _spawnedFleetIds.Add(fleetInfo.fleetId);
                    Debug.Log($"Fleet {fleetInfo.fleetId} spawned at planet {spawnPlanetId}");
                }
                // === 기존 함대 업데이트 ===
                else
                {
                    if (visualizationManager != null)
                    {
                        // 위치 업데이트 - Fleet GameObject의 Transform 직접 업데이트
                        UnityEngine.Vector3 targetPos = new UnityEngine.Vector3(fleetInfo.position.X, fleetInfo.position.Y, 0);

                        // ⭐ TODO: VisualizationManager에 SetFleetPosition 또는 GetFleet 메서드 추가 필요
                        // 현재는 VisualizationManager에 해당 API가 없으므로 로그만 출력
                        // visualizationManager.UpdateFleetPosition((int)fleetInfo.fleetId, targetPos);

                        // HP 바 업데이트
                        // ⭐ TODO: VisualizationManager에 UpdateFleetHealth 메서드 추가 필요
                        // float hpRatio = fleetInfo.maxHP > 0 ? fleetInfo.HP / fleetInfo.maxHP : 1f;
                        // visualizationManager.UpdateFleetHealth((int)fleetInfo.fleetId, hpRatio);
                    }

                    // 상태 변화 감지
                    var prevFleet = FindPreviousFleet(fleetInfo.fleetId);
                    if (prevFleet != null)
                    {
                        // Idle → Battle
                        if (prevFleet.state == 0 && fleetInfo.state == 1)
                        {
                            PlayBattleStartEffect(new Vector2(fleetInfo.position.X, fleetInfo.position.Y));
                            Debug.Log($"Fleet {fleetInfo.fleetId} entered combat!");
                        }
                        // Battle → Idle
                        else if (prevFleet.state == 1 && fleetInfo.state == 0)
                        {
                            PlayBattleEndEffect(new Vector2(fleetInfo.position.X, fleetInfo.position.Y));
                        }
                    }
                }
            }
        }

        // === 파괴된 함대 ===
        var destroyedFleetIds = new List<long>();
        foreach (var fleetId in _spawnedFleetIds)
        {
            if (!currentFleetIds.Contains(fleetId))
            {
                destroyedFleetIds.Add(fleetId);
            }
        }

        foreach (var fleetId in destroyedFleetIds)
        {
            var prevFleet = FindPreviousFleet(fleetId);
            if (prevFleet != null)
            {
                PlayDestroyEffect(new Vector2(prevFleet.position.X, prevFleet.position.Y));
                Debug.Log($"Fleet {fleetId} destroyed at ({prevFleet.position.X}, {prevFleet.position.Y})");
            }

            // ⭐ TODO: VisualizationManager에 DestroyFleet 메서드 추가 필요
            // if (visualizationManager != null)
            // {
            //     visualizationManager.DestroyFleet((int)fleetId);
            // }

            _spawnedFleetIds.Remove(fleetId);
        }
    }

    /// <summary>
    /// 행성 동기화: 소유권, 점령 진행도
    /// </summary>
    private void SyncPlanets(long tick)
    {
        if (_currentState.planets == null) return;

        for (int i = 0; i < _currentState.planets.Length; i++)
        {
            var planet = _currentState.planets[i];
            int planetId = planet.planetId;

            // 이전 소유자 가져오기
            int prevOwner = _planetOwners.ContainsKey(planetId) ? _planetOwners[planetId] : -1;

            // === 소유권 변경 감지 ===
            if (prevOwner != planet.owner)
            {
                if (visualizationManager != null)
                {
                    visualizationManager.UpdatePlanetOwnership(planetId, planet.owner);
                }

                if (prevOwner != -1) // 초기화가 아닌 실제 점령
                {
                    PlayConquerEffect(planetId);
                    ShowNotification($"Planet {planetId} conquered by Player {planet.owner}!");

                    // 카메라 효과
                    if (planet.owner == myPlayerId)
                    {
                        CameraFocusPlanet(planetId, 1.5f);
                    }
                }

                _planetOwners[planetId] = planet.owner;
            }

            // === 점령 진행도 업데이트 ===
            // ⭐ TODO: VisualizationManager에 UpdateConquestProgress 메서드 추가 필요
            // if (planet.conquestProgress > 0 && visualizationManager != null)
            // {
            //     visualizationManager.UpdateConquestProgress(planetId, planet.conquestProgress);
            // }
        }
    }

    /// <summary>
    /// 자원 UI 업데이트
    /// </summary>
    private void SyncResources(long tick)
    {
        if (_currentState.players == null) return;

        foreach (var player in _currentState.players)
        {
            if (player.id == myPlayerId && uiManager != null && uiManager.resourcesText != null)
            {
                // GameUIManager의 resourcesText 직접 업데이트
                uiManager.resourcesText.text = $"Minerals: {player.Mineral:F1} | Gas: {player.Gas:F1} | Supply: {player.Supply}";
            }
        }
    }

    /// <summary>
    /// 게임 종료 확인
    /// </summary>
    private void CheckGameState(long tick)
    {
        if (_currentState.state == 2) // GAMESTATE_ENDED
        {
            var alivePlayers = new List<GamePlayManager.GameState.Player>();
            foreach (var player in _currentState.players)
            {
                if (player.fleets != null && player.fleets.Length > 0)
                {
                    alivePlayers.Add(player);
                }
            }

            if (alivePlayers.Count == 1)
            {
                int winnerId = alivePlayers[0].id;
                bool isWinner = winnerId == myPlayerId;

                string message = isWinner ? "Victory!" : "Defeat!";
                Debug.Log($"Game ended: {message}");

                if (uiManager != null)
                {
                    // uiManager.ShowGameResult(isWinner, winnerId);
                }
            }
            else if (alivePlayers.Count == 0)
            {
                Debug.Log("Game ended: Draw (no survivors)");
            }
        }
    }

    #endregion

    #region 헬퍼 메서드

    private GamePlayManager.GameState.FleetInfo FindPreviousFleet(long fleetId)
    {
        if (_previousState == null || _previousState.players == null) return null;

        foreach (var player in _previousState.players)
        {
            if (player.fleets == null) continue;

            foreach (var fleet in player.fleets)
            {
                if (fleet.fleetId == fleetId) return fleet;
            }
        }
        return null;
    }

    private int FindClosestPlanet(Vector2 position)
    {
        int closestId = -1;
        float minDist = float.MaxValue;

        foreach (var kvp in _planetStaticData)
        {
            float dist = Vector2.Distance(position, kvp.Value.Position);

            if (dist < minDist)
            {
                minDist = dist;
                closestId = kvp.Key;
            }
        }

        return closestId;
    }

    // 효과 메서드들 (구현 필요)
    private void PlayBattleStartEffect(Vector2 pos) { /* TODO */ }
    private void PlayBattleEndEffect(Vector2 pos) { /* TODO */ }
    private void PlayDestroyEffect(Vector2 pos) { /* TODO */ }
    private void PlayConquerEffect(int planetId) { /* TODO */ }
    private void CameraFocusPlanet(int planetId, float duration) { /* TODO */ }
    private void ShowNotification(string message) { Debug.Log($"[Notification] {message}"); }

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
        Debug.Log($"Static Planets: {_planetStaticData.Count}");
        Debug.Log($"Spawned Fleets: {_spawnedFleetIds.Count}");
        Debug.Log($"Planet Owners: {_planetOwners.Count}");
        if (_currentState != null)
        {
            int totalFleets = 0;
            if (_currentState.players != null)
            {
                foreach (var player in _currentState.players)
                {
                    if (player.fleets != null)
                        totalFleets += player.fleets.Length;
                }
            }
            Debug.Log($"Current State - Players: {_currentState.players?.Length ?? 0}, Total Fleets: {totalFleets}");
        }
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
        // State-Sync 모델에서는 모성 정보를 별도로 관리하지 않음
        // 내 플레이어의 모성만 MyHomePlanetId에 저장됨
        if (playerId == myPlayerId)
        {
            return MyHomePlanetId;
        }
        return -1; // 다른 플레이어는 지원 안 함
    }

    /// <summary>
    /// 에디터 디버그용: 행성 정보를 등록하고, 첫 행성이면 모성으로 설정
    /// </summary>
    public void EditorRegisterPlanet(int planetId, int ownerId, UnityEngine.Vector2 position)
    {
        // State-Sync 모델에서는 _planetStaticData 사용
        if (!_planetStaticData.ContainsKey(planetId))
        {
            _planetStaticData[planetId] = new PlanetStaticData
            {
                PlanetId = planetId,
                Position = position,
                MaxMinerals = 0,
                MaxGas = 0
            };
        }

        // 소유권 초기화
        _planetOwners[planetId] = ownerId;

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
            gamePlayManager.GameStateReceived -= OnGameStateReceived;
            gamePlayManager.GameStarted -= OnGameStarted;
            gamePlayManager.GameEnded -= OnGameEnded;
            gamePlayManager.FleetSpawned -= OnFleetSpawned;
            gamePlayManager.OnError -= OnErrorOccurred;
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
