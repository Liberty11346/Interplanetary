using UnityEngine;
using CommonLib;
using System.Collections.Generic;

/// <summary>
/// 게임 UI 총괄 관리자
/// 모든 UI Layer를 관리하고 GameManager와 연결
/// </summary>
public class UIGame : MonoBehaviour
{
    [Header("UI Layers")]
    [SerializeField] private UIFleet_Layer fleetLayer;
    [SerializeField] private UIPlanet_Layer planetLayer;
    [SerializeField] private UIPath_Layer pathLayer;

    [Header("Selection Indicators")]
    [SerializeField] private GameObject fleetSelectionRing;
    [SerializeField] private GameObject planetSelectionRing;

    // 선택 추적
    private int selectedFleetId = -1;
    private int selectedPlanetId = -1;

    private void Awake()
    {
        // Layer 자동 찾기 (할당되지 않은 경우)
        if (fleetLayer == null)
            fleetLayer = GetComponentInChildren<UIFleet_Layer>();
        if (planetLayer == null)
            planetLayer = GetComponentInChildren<UIPlanet_Layer>();
        if (pathLayer == null)
            pathLayer = GetComponentInChildren<UIPath_Layer>();

        // 선택 링 초기화
        if (fleetSelectionRing != null)
            fleetSelectionRing.SetActive(false);
        if (planetSelectionRing != null)
            planetSelectionRing.SetActive(false);
    }

    private void Start()
    {
        // GameManager 이벤트 구독
        if (GameManager.Instance != null)
        {
            fleetLayer.OnClickFleet += OnClickedFleet;
            planetLayer.OnClickPlanet += OnClickedPlanet;

            // 선택 이벤트 구독
            GameManager.Instance.OnFleetSelected += UpdateFleetSelection;
            GameManager.Instance.OnPlanetSelected += UpdatePlanetSelection;
        }
    }

    private void Update()
    {
        // 선택된 함대 추적 (이동 중인 함대를 따라가기 위해)
        if (selectedFleetId >= 0 && fleetSelectionRing != null && fleetSelectionRing.activeSelf)
        {
            UIFleet fleet = fleetLayer?.GetEntity(selectedFleetId);
            if (fleet != null)
            {
                fleetSelectionRing.transform.position = fleet.transform.position;
            }
        }

        // 행성은 고정이므로 추적 불필요
    }

    #region Fleet Management

    private void OnClickedFleet(int fleetId, int ownerId)
    {
        Debug.Log($"Fleet {fleetId} (Owner: {ownerId}) clicked in UIGame");
        GameManager.Instance.SelectFleet(fleetId);
    }

    /// <summary>
    /// 함대 생성 또는 업데이트
    /// </summary>
    public void CreateOrUpdateFleet(int fleetId, GamePlayManager.GameState.FleetInfo fleetInfo)
    {
        if (fleetLayer != null)
        {
            fleetLayer.CreateOrUpdateEntity(fleetId, fleetInfo);
        }
    }

    /// <summary>
    /// 함대 제거
    /// </summary>
    public void RemoveFleet(int fleetId)
    {
        if (fleetLayer != null)
        {
            fleetLayer.RemoveEntity(fleetId);
        }
    }

    /// <summary>
    /// 모든 함대 제거
    /// </summary>
    public void ClearAllFleets()
    {
        if (fleetLayer != null)
        {
            fleetLayer.ClearAllEntities();
        }
    }

    #endregion

    #region Planet Management

    private void OnClickedPlanet(int planetId, int ownerId)
    {
        Debug.Log($"Planet {planetId} (Owner: {ownerId}) clicked in UIGame");
        GameManager.Instance.SelectPlanet(planetId);
    }

    /// <summary>
    /// 행성 생성 또는 업데이트
    /// </summary>
    public void CreateOrUpdatePlanet(int planetId, PlanetData planetData)
    {
        if (planetLayer != null)
        {
            planetLayer.CreateOrUpdateEntity(planetId, planetData);
        }
    }

    /// <summary>
    /// 행성 제거
    /// </summary>
    public void RemovePlanet(int planetId)
    {
        if (planetLayer != null)
        {
            planetLayer.RemoveEntity(planetId);
        }
    }

    /// <summary>
    /// 모든 행성 제거
    /// </summary>
    public void ClearAllPlanets()
    {
        if (planetLayer != null)
        {
            planetLayer.ClearAllEntities();
        }
    }

    #endregion

    #region Path Management

    /// <summary>
    /// 경로 생성 또는 업데이트
    /// </summary>
    public void CreateOrUpdatePath(int pathId, PathData pathData)
    {
        if (pathLayer != null)
        {
            pathLayer.CreateOrUpdateEntity(pathId, pathData);
        }
    }

    /// <summary>
    /// 경로 제거
    /// </summary>
    public void RemovePath(int pathId)
    {
        if (pathLayer != null)
        {
            pathLayer.RemoveEntity(pathId);
        }
    }

    /// <summary>
    /// 모든 경로 제거
    /// </summary>
    public void ClearAllPaths()
    {
        if (pathLayer != null)
        {
            pathLayer.ClearAllEntities();
        }
    }

    #endregion

    #region GameManager Integration

    /// <summary>
    /// GameState 전체 동기화
    /// GameManager에서 매 틱마다 호출
    /// </summary>
    public void SyncGameState(GamePlayManager.GameState gameState)
    {
        if (gameState == null) return;

        // 함대 동기화
        SyncFleets(gameState);

        // 행성 동기화
        SyncPlanets(gameState);
    }

    /// <summary>
    /// 함대 동기화
    /// </summary>
    private void SyncFleets(GamePlayManager.GameState gameState)
    {
        if (gameState.players == null || fleetLayer == null) return;

        HashSet<int> currentFleetIds = new HashSet<int>();

        foreach (var player in gameState.players)
        {
            if (player.fleets == null) continue;

            foreach (var fleetInfo in player.fleets)
            {
                int fleetId = (int)fleetInfo.fleetId;
                currentFleetIds.Add(fleetId);

                // 함대 생성 또는 업데이트
                CreateOrUpdateFleet(fleetId, fleetInfo);
            }
        }

        // 더 이상 존재하지 않는 함대 제거
        RemoveDestroyedFleets(currentFleetIds);
    }

    /// <summary>
    /// 행성 동기화
    /// </summary>
    private void SyncPlanets(GamePlayManager.GameState gameState)
    {
        if (gameState.planets == null || planetLayer == null) return;

        foreach (var planet in gameState.planets)
        {
            // PlanetData 구성 (GameState.Planet → PlanetData 변환 필요)
            // 실제 구현 시 GameManager에서 PlanetData를 전달받거나
            // 여기서 변환 로직 추가
        }
    }

    /// <summary>
    /// 파괴된 함대 제거
    /// </summary>
    private void RemoveDestroyedFleets(HashSet<int> currentFleetIds)
    {
        if (fleetLayer == null) return;

        // 현재 활성화된 모든 함대 ID 확인
        var activeFleetIds = new List<int>(fleetLayer.GetActiveEntityIds());
        
        foreach (var fleetId in activeFleetIds)
        {
            if (!currentFleetIds.Contains(fleetId))
            {
                RemoveFleet(fleetId);
            }
        }
    }

    /// <summary>
    /// 게임 초기화 (게임 시작 시)
    /// </summary>
    public void InitializeGame()
    {
        ClearAllFleets();
        ClearAllPlanets();
        ClearAllPaths();
    }

    #endregion

    #region Selection Management

    /// <summary>
    /// 함대 선택 표시 업데이트
    /// </summary>
    public void UpdateFleetSelection(int fleetId)
    {
        selectedFleetId = fleetId;

        if (fleetSelectionRing == null) return;

        if (fleetId >= 0)
        {
            UIFleet fleet = fleetLayer?.GetEntity(fleetId);
            if (fleet != null)
            {
                fleetSelectionRing.SetActive(true);
                fleetSelectionRing.transform.position = fleet.transform.position;
                Debug.Log($"Fleet {fleetId} selected, showing selection ring.");
            }
        }
        else
        {
            fleetSelectionRing.SetActive(false);
            Debug.Log("Fleet selection cleared.");
        }
    }

    /// <summary>
    /// 행성 선택 표시 업데이트
    /// </summary>
    public void UpdatePlanetSelection(int planetId)
    {
        selectedPlanetId = planetId;

        if (planetSelectionRing == null) return;

        if (planetId >= 0)
        {
            UIPlanet planet = planetLayer?.GetEntity(planetId);
            if (planet != null)
            {
                planetSelectionRing.SetActive(true);
                planetSelectionRing.transform.position = planet.transform.position;
                Debug.Log($"Planet {planetId} selected, showing selection ring.");
            }
        }
        else
        {
            planetSelectionRing.SetActive(false);
            Debug.Log("Planet selection cleared.");
        }
    }

    #endregion

    #region Conquest Progress

    /// <summary>
    /// 함대 정보 가져오기
    /// </summary>
    public GamePlayManager.GameState.FleetInfo GetFleetInfo(int fleetId)
    {
        if (fleetLayer != null)
        {
            var fleet = fleetLayer.GetEntity(fleetId);
            if (fleet != null)
            {
                return fleet.fleetData;
            }
        }
        return null;
    }

    /// <summary>
    /// 행성 점령 진행도 업데이트
    /// </summary>
    public void UpdateConquestProgress(int planetId, float normalizedProgress)
    {
        UIPlanet planet = planetLayer?.GetEntity(planetId);
        if (planet != null)
        {
            // UIPlanet에 진행도 업데이트
            planet.UpdateConquestProgress(normalizedProgress);

            // 로그
            if (normalizedProgress >= 1.0f)
            {
                Debug.Log($"[UIGame] Planet {planetId} conquest complete!");
            }
            else if (normalizedProgress > 0f)
            {
                Debug.Log($"[UIGame] Planet {planetId} conquest: {normalizedProgress * 100f:F1}%");
            }
        }
    }

    #endregion

    #region Debug

    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    public void PrintDebugInfo()
    {
        Debug.Log($"=== UIGame Debug Info ===");
        Debug.Log($"Active Fleets: {fleetLayer?.ActiveEntityCount ?? 0}");
        Debug.Log($"Active Planets: {planetLayer?.ActiveEntityCount ?? 0}");
        Debug.Log($"Active Paths: {pathLayer?.ActiveEntityCount ?? 0}");
        Debug.Log($"========================");
    }

    #endregion

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFleetSelected -= UpdateFleetSelection;
            GameManager.Instance.OnPlanetSelected -= UpdatePlanetSelection;
        }
    }
}
