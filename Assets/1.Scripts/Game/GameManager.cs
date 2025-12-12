using UnityEngine;
using CommonLib;
using Vector2 = UnityEngine.Vector2;
using CommonVector2 = CommonLib.Vector2;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using System.Threading.Tasks;


/// <summary>
/// 게임 매니저 - 게임 상태 관리 및 이벤트 핸들링을 담당
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    private static readonly object _lock = new object();

    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null && Application.isPlaying)
                    {
                        var go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                        DontDestroyOnLoad(go);
                    }
                }
            }
            return _instance;
        }
    }

    [Header("Game Settings")]
    public int debugMapId = 1; // 디버그용 맵 ID (F5로 로드)

    [Header("Managers")]
    public GamePlayManager gamePlayManager;    // 게임 플레이 및 서버 통신 담당
    public UIGame uiGame;  // UI 시각화 담당
    public UI_HUD uiHUD;   // UI HUD 담당 (신규)

    [Header("Game State")]
    private bool isGameStarted = false;
    private int myPlayerId = -1;
    public int MyPlayerId => myPlayerId;
    public int MyHomePlanetId { get; private set; } = -1;
    private float gameTime = 0f;
    public bool IsGameStarted => isGameStarted;

    [Header("Interpolation")]
    private float interpolationTime = 0f;
    private const float TICK_INTERVAL = 0.05f; // 50ms = 0.05초

    [Header("Selection State")]
    private int selectedPlanetId = -1;
    private int selectedFleetId = -1;

    public event Action<int> OnPlanetSelected;
    public event Action<int> OnFleetSelected;

    private long _currentTick = 0;

    // === 정적 데이터 (GameSet에서 1회 수신) ===
    private Dictionary<int, PlanetStaticData> _planetStaticData = new Dictionary<int, PlanetStaticData>();
    public GameStartData _gameStartData;

    // === 동적 데이터 (GameState에서 매 틱 수신) ===
    private GamePlayManager.GameState _currentState;
    private GamePlayManager.GameState _previousState;

    // === 엔티티 추적 ===
    private HashSet<long> _spawnedFleetIds = new HashSet<long>();
    private Dictionary<int, int> _planetOwners = new Dictionary<int, int>(); // planetId -> ownerId
    private Dictionary<int, PlanetData> _planetDatas = new Dictionary<int, PlanetData>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeGame();
    }

    private void Update()
    {
        if (isGameStarted)
        {
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

        if (uiGame == null)
            uiGame = FindFirstObjectByType<UIGame>();

        if (uiHUD == null)
            uiHUD = FindFirstObjectByType<UI_HUD>();

        Debug.Log($"[GameManager] gamePlayManager: {(gamePlayManager != null ? "찾음" : "없음")}, uiHUD: {(uiHUD != null ? "찾음" : "없음")}, uiGame: {(uiGame != null ? "찾음" : "없음")}");

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

            // 채팅 및 시스템 이벤트 (UI_HUD 연동)
            gamePlayManager.ChatReceived += OnChatReceived;
            gamePlayManager.ConnectionChanged += OnConnectionChanged;
            gamePlayManager.UserJoined += OnUserJoined;
            gamePlayManager.UserLeft += OnUserLeft;

            // 에러 처리
            gamePlayManager.OnError += OnErrorOccurred;
        }

        Debug.Log("GameManager initialized - State-Sync 모델 활성화");

        // ⭐ 가이드 명세: 모든 초기화 완료 후 REQUEST_GAME_CL_READY 전송
        // GAME_SET → 맵 로딩 → 초기화 완료 → REQUEST_GAME_CL_READY → GAME_STARTED 대기
        _ = SendGameClientReady();
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
        if (uiGame != null)
        {
            // UIGame으로 행성 생성
            if (gameData.Planets != null)
            {
                _planetDatas.Clear();
                foreach (var planet in gameData.Planets)
                {
                    uiGame.CreateOrUpdatePlanet(planet.PlanetId, planet);
                    _planetDatas.Add(planet.PlanetId, planet);
                }
            }

            // 행성 간 연결선(경로) 그리기
            if (gameData.Routes != null && gameData.Routes.Length > 0)
            {
                foreach (var route in gameData.Routes)
                {
                    if (!_planetDatas.TryGetValue(route.planetToId, out PlanetData toPlanet) ||
                        !_planetDatas.TryGetValue(route.planetFromId, out PlanetData fromPlanet))
                    {
                        Debug.LogWarning($"Route {route.id} references unknown planets: From {route.planetFromId}, To {route.planetToId}");
                        continue;
                    }

                    PathData pathData = new PathData
                    {
                        PathId = route.id,
                        FromPlanetId = route.planetFromId,
                        ToPlanetId = route.planetToId,
                        FromPosition = ConvertCVectorToRect(fromPlanet.Position),
                        ToPosition = ConvertCVectorToRect(toPlanet.Position),
                        LineColor = UnityEngine.Color.white
                    };
                    uiGame.CreateOrUpdatePath(route.id, pathData);
                }
                Debug.Log($"행성 간 연결선 {gameData.Routes.Length}개 그려짐");
            }

            Debug.Log("Game visualization initialized");
        }
    }


    private UnityEngine.Vector2 ConvertCVectorToRect(CommonLib.Vector2 vector2)
    {
        const float uiScale = 0.5f; // 맵 스케일에 맞게 조정 필요
        return new UnityEngine.Vector2(vector2.X * uiScale, vector2.Y * uiScale);
    }

    /// <summary>
    /// 게임 종료 이벤트 처리
    /// </summary>
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
        }

        // UI 업데이트
        if (uiHUD != null)
        {
            uiHUD.AddChatMessage("Error", errorMessage); // 채팅창에 에러 표시
        }
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

    private void OnChatReceived(ChatMessage chatMessage)
    {
        if (uiHUD != null)
        {
            var timestamp = System.DateTimeOffset.FromUnixTimeMilliseconds(chatMessage.Timestamp).LocalDateTime;
            uiHUD.AddChatMessage(chatMessage.SenderId, chatMessage.Message, timestamp);
        }
    }

    private void OnConnectionChanged(bool connected, string message)
    {
        if (uiHUD != null)
        {
            uiHUD.UpdateConnectionStatus(connected);
            uiHUD.AddChatMessage("System", message);
        }
    }

    private void OnUserJoined(string userId, int playerCount)
    {
        if (uiHUD != null)
        {
            // 방 정보 표시는 제거됨
            uiHUD.AddChatMessage("System", $"{userId} joined the room ({playerCount} players)");
        }
    }

    private void OnUserLeft(string userId, int playerCount)
    {
        if (uiHUD != null)
        {
            uiHUD.AddChatMessage("System", $"{userId} left the room ({playerCount} players)");
        }
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

        // 보간 타이머 리셋 (새로운 스냅샷 수신)
        interpolationTime = 0f;

        // 순서 중요: 생산 대기열 → 함대 → 행성 → 자원
        SyncProductionQueue(tick);
        SyncFleets(tick);
        SyncPlanets(tick);

        // 자원 및 게임 상태 체크는 UI_HUD.UpdateGameState에서 처리됨

        // UI HUD 업데이트
        if (uiHUD != null)
        {
            uiHUD.UpdateGameState(_currentState, gameTime);
        }
    }

    /// <summary>
    /// 생산 대기열 동기화: 생산 시작, 진행, 완료 감지
    /// </summary>
    private void SyncProductionQueue(long tick)
    {
        if (_currentState?.players == null) return;

        foreach (var player in _currentState.players)
        {
            if (player.id != myPlayerId) continue; // 내 플레이어만 체크

            var currentQueue = player.productionQueue;
            var previousQueue = _previousState?.players != null ?
                GetPlayerFromState(_previousState, player.id)?.productionQueue : null;

            // === 생산 시작 감지 ===
            if (currentQueue != null && currentQueue.Length > 0)
            {
                foreach (var queueItem in currentQueue)
                {
                    // 이전에 없던 생산 항목이 추가됨
                    bool isNewProduction = previousQueue == null ||
                        !ContainsProduction(previousQueue, queueItem.fleetId);

                    if (isNewProduction)
                    {
                        Debug.Log($"[GameManager] Production started: FleetType {queueItem.fleetType}, " +
                                  $"Total time: {queueItem.totalTicks * 0.05f:F1}s");
                    }

                    // 생산 진행 중 로그 (선택적, 너무 많이 출력되지 않도록 10% 단위로)
                    if (previousQueue != null)
                    {
                        var prevItem = FindProduction(previousQueue, queueItem.fleetId);
                        if (prevItem != null)
                        {
                            float prevProgress = prevItem.progress;
                            float currProgress = queueItem.progress;

                            // 10% 단위로 로그 출력
                            int prevPercentile = (int)(prevProgress * 10);
                            int currPercentile = (int)(currProgress * 10);

                            if (currPercentile > prevPercentile)
                            {
                                Debug.Log($"[GameManager] Production progress: FleetType {queueItem.fleetType}, " +
                                          $"{currProgress * 100:F0}% ({queueItem.remainingTicks * 0.05f:F1}s remaining)");
                            }
                        }
                    }
                }
            }

            // === 생산 완료 감지 ===
            if (previousQueue != null && previousQueue.Length > 0)
            {
                foreach (var prevItem in previousQueue)
                {
                    // 이전에 있던 생산 항목이 현재 대기열에 없음 = 완료됨
                    bool isCompleted = currentQueue == null ||
                        !ContainsProduction(currentQueue, prevItem.fleetId);

                    if (isCompleted)
                    {
                        Debug.Log($"[GameManager] ✅ Production completed: FleetType {prevItem.fleetType}");
                    }
                }
            }

            break; // 내 플레이어만 처리하므로 종료
        }
    }

    /// <summary>
    /// 생산 대기열에서 특정 fleetId를 가진 항목이 있는지 확인
    /// </summary>
    private bool ContainsProduction(GamePlayManager.GameState.ProductionQueueInfo[] queue, long fleetId)
    {
        if (queue == null) return false;

        foreach (var item in queue)
        {
            if (item.fleetId == fleetId) return true;
        }
        return false;
    }

    /// <summary>
    /// 생산 대기열에서 특정 fleetId를 가진 항목 찾기
    /// </summary>
    private GamePlayManager.GameState.ProductionQueueInfo? FindProduction(
        GamePlayManager.GameState.ProductionQueueInfo[] queue, long fleetId)
    {
        if (queue == null) return null;

        foreach (var item in queue)
        {
            if (item.fleetId == fleetId) return item;
        }
        return null;
    }

    /// <summary>
    /// GameState에서 특정 플레이어 찾기
    /// </summary>
    private GamePlayManager.GameState.Player GetPlayerFromState(GamePlayManager.GameState state, int playerId)
    {
        if (state?.players == null) return null;

        foreach (var player in state.players)
        {
            if (player.id == playerId) return player;
        }
        return null;
    }

    /// <summary>
    /// 함대 동기화: 생성, 이동, 파괴 감지
    /// </summary>
    private void SyncFleets(long tick)
    {
        if (_currentState?.players == null) return;

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
                    if (uiGame != null)
                    {
                        uiGame.CreateOrUpdateFleet((int)fleetInfo.fleetId, fleetInfo);
                    }

                    _spawnedFleetIds.Add(fleetInfo.fleetId);
                    Debug.Log($"[GameManager] Fleet {fleetInfo.fleetId} (Type: {fleetInfo.fleetType}) spawned for Player {player.id}");
                }
                // === 기존 함대 업데이트 ===
                else
                {
                    if (uiGame != null)
                    {
                        // 위치 및 상태 업데이트
                        uiGame.CreateOrUpdateFleet((int)fleetInfo.fleetId, fleetInfo);
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

            // ✅ 함대 파괴 애니메이션 실행
            if (uiGame != null)
            {
                uiGame.RemoveFleet((int)fleetId);
            }

            _spawnedFleetIds.Remove(fleetId);
        }
    }

    /// <summary>
    /// 행성 동기화: 소유권, 점령 진행도
    /// </summary>
    private void SyncPlanets(long tick)
    {
        if (_currentState?.planets == null) return;

        for (int i = 0; i < _currentState.planets.Length; i++)
        {
            var planet = _currentState.planets[i];
            int planetId = planet.planetId;

            // 이전 소유자 가져오기
            int prevOwner = _planetOwners.ContainsKey(planetId) ? _planetOwners[planetId] : -1;

            // === 소유권 변경 감지 ===
            if (prevOwner != planet.owner)
            {
                if (uiGame != null)
                {
                    // 행성 소유권 변경은 PlanetData 업데이트로 처리
                    // uiGame.CreateOrUpdatePlanet에서 자동 처리됨
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
            if (planet.conquestProgress > 0 && uiGame != null)
            {
                UpdatePlanetConquestProgress(planetId, planet.conquestProgress);
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

    /// <summary>
    /// 행성 점령 진행도 UI 업데이트 (가이드 권장사항)
    /// </summary>
    private void UpdatePlanetConquestProgress(int planetId, float progress)
    {
        // UIGame으로 점령 진행도 업데이트
        uiGame.UpdateConquestProgress(planetId, progress / 100f);
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
    /// 초기 게임 상태 설정 - 씬 로드 후 호출됨
    /// </summary>
    private async void SetupInitialGameState()
    {
        // 게임 시작 시 초기 상태 설정
        Debug.Log("[GameManager] Setting up initial game state...");

        // UI 초기화
        if (uiHUD != null)
        {
            uiHUD.Initialize(myPlayerId, $"Player {myPlayerId}");
        }

        Debug.Log("[GameManager] Initial game state setup completed");

        // REQUEST_GAME_CL_READY는 InitializeGame에서 전송됨
    }

    /// <summary>
    /// 게임 클라이언트 준비 완료 알림 전송
    /// </summary>
    private async Task SendGameClientReady()
    {
        if (gamePlayManager != null)
        {
            bool success = await gamePlayManager.RequestGameClientReady();

            if (success)
            {
                Debug.Log("[GameManager] ✅ REQUEST_GAME_CL_READY 전송 완료 - GAME_STARTED 대기 중...");
            }
            else
            {
                Debug.LogError("[GameManager] ❌ REQUEST_GAME_CL_READY 전송 실패!");
            }
        }
        else
        {
            Debug.LogError("[GameManager] GamePlayManager가 없어 REQUEST_GAME_CL_READY를 전송할 수 없습니다!");
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
        if (uiHUD != null)
        {
            uiHUD.ShowGameResult(isWinner, resultMessage);
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

        // UI HUD에 선택 정보 전달
        if (uiHUD != null)
        {
            uiHUD.OnFleetSelected(fleetId);
        }

        Debug.Log($"Fleet {fleetId} selected. Ready for move command.");
    }



    /// <summary>
    /// 함대 생산 요청
    /// </summary>
    public void RequestProduction(int fleetTypeId)
    {
        Debug.Log($"[GameManager] Requesting production: FleetType {fleetTypeId} at Planet {MyHomePlanetId}");

        // GamePlayManager를 통해 서버로 생산 요청 전송

        gamePlayManager.RequestProduceFleet(fleetTypeId);
    }

    /// <summary>
    /// 선택 해제
    /// </summary>
    public void ClearSelection()
    {
        selectedFleetId = -1;
        selectedPlanetId = -1;
        OnFleetSelected?.Invoke(-1);
        OnPlanetSelected?.Invoke(-1);

        if (uiHUD != null)
        {
            uiHUD.OnFleetSelected(-1);
        }
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
    /// 가이드 명세: 서버가 플레이어의 모성에서 자동 생산 (행성 ID 불필요)
    /// </summary>
    public void CommandFleetSpawn(int fleetType)
    {
        if (gamePlayManager != null && isGameStarted)
        {
            gamePlayManager.RequestProduceFleet(fleetType);
            Debug.Log($"Requesting fleet spawn of type {fleetType}");

            // 실제 함대 생성 및 검증은 서버에서 처리
            // 서버는 자원 확인, 생산 가능 여부 등을 검증하고
            // 승인/거절 결과를 FleetSpawned 이벤트나 Error 이벤트로 응답
        }
    }

    #endregion

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
    public async Task LoadMapAsync(int mapId)
    {
        if (gamePlayManager == null)
        {
            Debug.LogError($"❌ [LoadMap] GamePlayManager가 null입니다. 맵을 로드할 수 없습니다.");
            return;
        }

        Debug.Log($"[LoadMap] 서버에서 맵 {mapId} 요청 중...");

        try
        {
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
        catch (Exception ex)
        {
            Debug.LogError($"❌ [LoadMap] 예외 발생: {ex.Message}");
        }
    }

    /// <summary>
    /// async void wrapper for Unity event system
    /// </summary>
    public async void LoadMap(int mapId)
    {
        await LoadMapAsync(mapId);
    }

    /// <summary>
    /// 컴포넌트 제거 시 이벤트 구독 해제 및 리소스 정리
    /// </summary>
    private void OnDestroy()
    {
        // Singleton 정리
        if (_instance == this)
        {
            _instance = null;
        }

        // 이벤트 구독 해제
        if (gamePlayManager != null)
        {
            gamePlayManager.GameStateReceived -= OnGameStateReceived;
            gamePlayManager.GameStarted -= OnGameStarted;
            gamePlayManager.GameEnded -= OnGameEnded;
            gamePlayManager.FleetSpawned -= OnFleetSpawned;
            gamePlayManager.OnError -= OnErrorOccurred;

            // 추가된 이벤트 구독 해제
            gamePlayManager.ChatReceived -= OnChatReceived;
            gamePlayManager.ConnectionChanged -= OnConnectionChanged;
            gamePlayManager.UserJoined -= OnUserJoined;
            gamePlayManager.UserLeft -= OnUserLeft;
        }

        // 컬렉션 정리
        _spawnedFleetIds?.Clear();
        _planetOwners?.Clear();
        _planetStaticData?.Clear();

        // 상태 초기화
        _currentState = null;
        _previousState = null;
        _gameStartData = default;

        Debug.Log("[GameManager] Resources cleaned up");
    }
}
