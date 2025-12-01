using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonLib;
using Vector3 = UnityEngine.Vector3;

/// <summary>
/// 통합 시각화 매니저 - 게임 내 모든 시각적 요소 관리
/// GameManager로부터 데이터를 받아 시각적 표현만 담당
/// </summary>
public class VisualizationManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject planetPrefab;  // 행성 프리팹
    public GameObject fleetPrefab;     // 함대 프리팹

    [Header("UI Containers")]
    public Transform planetContainer;        // 행성 오브젝트 컨테이너
    public Transform fleetContainer;         // 함대 오브젝트 컨테이너
    public Transform edgeContainer;          // 간선 오브젝트 컨테이너

    [Header("Prefabs")]
    public GameObject linePrefab; // 간선 프리팹 (LineRenderer 컴포넌트 포함)

    [Header("Selection")]
    public GameObject fleetSelectionRing;    // 함대 선택 표시 링
    public GameObject planetSelectionRing;   // 행성 선택 표시 링

    // 데이터 캐싱
    private Dictionary<int, GameObject> _planets = new Dictionary<int, GameObject>();
    private Dictionary<int, PlanetUIButton> _planetButtons = new Dictionary<int, PlanetUIButton>();
    private Dictionary<int, FleetUIButton> _fleets = new Dictionary<int, FleetUIButton>();

    private void Start()
    {
        // 시각화 초기화
        InitializeVisualization();

        // GameManager 이벤트 구독
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlanetSelected += UpdatePlanetSelection;
            GameManager.Instance.OnFleetSelected += UpdateFleetSelection;
        }
    }

    private void OnDestroy()
    {
        // GameManager 이벤트 구독 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlanetSelected -= UpdatePlanetSelection;
            GameManager.Instance.OnFleetSelected -= UpdateFleetSelection;
        }
    }

    /// <summary>
    /// 시각화 시스템 초기화
    /// </summary>
    private void InitializeVisualization()
    {
        Debug.Log("VisualizationManager initialized - 시각화 시스템 준비 완료");

        // 선택 링 초기화 (비활성화)
        if (fleetSelectionRing != null)
            fleetSelectionRing.SetActive(false);
        if (planetSelectionRing != null)
            planetSelectionRing.SetActive(false);

        // 안전장치: planetContainer나 planetPrefab이 설정되어 있지 않으면 런타임에서 기본값을 생성합니다.
        if (planetContainer == null)
        {
            var go = new GameObject("PlanetContainer");
            go.transform.SetParent(this.transform, false);
            planetContainer = go.transform;
            Debug.LogWarning("VisualizationManager: planetContainer was null — created runtime container.");
        }

        if (fleetContainer == null)
        {
            var go = new GameObject("FleetContainer");
            go.transform.SetParent(this.transform, false);
            fleetContainer = go.transform;
            Debug.LogWarning("VisualizationManager: fleetContainer was null — created runtime container.");
        }

        if (edgeContainer == null)
        {
            var go = new GameObject("EdgeContainer");
            go.transform.SetParent(this.transform, false);
            edgeContainer = go.transform;
            Debug.LogWarning("VisualizationManager: edgeContainer was null — created runtime container.");
        }

        if (planetPrefab == null)
        {
            // Create a simple placeholder sphere to use as a planet prefab at runtime
            GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            placeholder.name = "PlanetPrefab_Placeholder";
            // remove collider to avoid unexpected physics interactions
            var col = placeholder.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Add PlanetUIButton so code can interact with it (UI fields will be null but safe)
            placeholder.AddComponent<PlanetUIButton>();

            // Make the placeholder inactive as a template
            placeholder.SetActive(false);

            planetPrefab = placeholder;
            Debug.LogWarning("VisualizationManager: planetPrefab was null — created placeholder primitive prefab at runtime.");
        }
    }

    /// <summary>
    /// 행성 생성
    /// </summary>
    /// <param name="planetId">행성 ID</param>
    /// <param name="position">행성 위치</param>
    public void CreatePlanet(int planetId, Vector3 position)
    {
        if (planetPrefab != null && planetContainer != null)
        {
            // 지정된 위치에 행성 프리팹을 생성하고, 컨테이너의 자식으로 설정합니다.
            GameObject planetObj = Instantiate(planetPrefab, position, Quaternion.identity, planetContainer);

            // 이동 및 선택 로직을 위해 생성된 행성 게임 오브젝트를 딕셔너리에 캐싱합니다.
            _planets[planetId] = planetObj;

            var planetButton = planetObj.GetComponent<PlanetUIButton>();
            if (planetButton != null)
            {
                planetButton.planetId = planetId;
                _planetButtons[planetId] = planetButton;
            }
        }

        Debug.Log($"Planet {planetId} created at position {position}");
    }

    /// <summary>
    /// 행성 데이터와 함께 생성
    /// </summary>
    public void CreatePlanetWithData(int planetId, Vector3 position, PlanetData planetData)
    {
        if (planetPrefab != null && planetContainer != null)
        {
            // 지정된 위치에 행성 프리팹을 생성
            GameObject planetObj = Instantiate(planetPrefab, position, Quaternion.identity, planetContainer);
            _planets[planetId] = planetObj;

            var planetButton = planetObj.GetComponent<PlanetUIButton>();
            if (planetButton != null)
            {
                // PlanetData로부터 모든 정보 설정
                planetButton.SetPlanetData(planetData);
                _planetButtons[planetId] = planetButton;
            }
        }

        Debug.Log($"Planet {planetId} ({planetData.Name}) created at position {position}");
    }

    /// <summary>
    /// 행성 생성 + 소유자 설정 (에디터 디버그용)
    /// </summary>
    public void CreatePlanetWithOwner(int planetId, Vector3 position, int ownerId)
    {
        CreatePlanet(planetId, position);

        UpdatePlanetOwnership(planetId, ownerId);

        // 게임 매니저 캐시에 반영 및 첫 행성이면 모성 등록
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EditorRegisterPlanet(planetId, ownerId, new UnityEngine.Vector2(position.x, position.y));
        }
    }

    /// <summary>
    /// 함대 생성
    /// </summary>
    /// <param name="fleetId">함대 ID</param>
    /// <param name="fleetType">함대 유형</param>
    /// <param name="ownerId">소유자 ID</param>
    public void CreateFleet(int fleetId, int fleetType, int ownerId, int planetId)
    {
        if (fleetPrefab != null && fleetContainer != null)
        {
            GameObject fleetObj = Instantiate(fleetPrefab, fleetContainer);
            var fleetButton = fleetObj.GetComponent<FleetUIButton>();
            if (fleetButton != null)
            {
                // 함대 데이터 생성
                FleetSpawnData spawnData = new FleetSpawnData
                {
                    FleetId = fleetId,
                    FleetType = fleetType,
                    OwnerId = ownerId,
                    PlanetId = planetId
                };

                fleetButton.Initialize(spawnData);
                _fleets[fleetId] = fleetButton;
            }
        }
    }

    /// <summary>
    /// 함대 이동 애니메이션 시작
    /// </summary>
    /// <param name="fleetId">함대 ID</param>
    /// <param name="fromPlanetId">출발 행성 ID</param>
    /// <param name="toPlanetId">도착 행성 ID</param>
    public void AnimateFleetMovement(int fleetId, int fromPlanetId, int toPlanetId)
    {
        if (_fleets.TryGetValue(fleetId, out FleetUIButton fleet))
        {
            FleetMoveData moveData = new FleetMoveData
            {
                FleetId = fleetId,
                FromPlanetId = fromPlanetId,
                ToPlanetId = toPlanetId
            };

            StartCoroutine(AnimateFleetMovementCoroutine(fleet, moveData));
            Debug.Log($"Fleet {fleetId} movement animation started: {fromPlanetId} -> {toPlanetId}");
        }
        else
        {
            Debug.LogWarning($"Cannot animate fleet {fleetId}: fleet not found");
        }
    }

    /// <summary>
    /// 함대 이동 애니메이션 코루틴
    /// </summary>
    /// <param name="fleet">함대 UI 아이템</param>
    /// <param name="moveData">이동 데이터</param>
    private IEnumerator AnimateFleetMovementCoroutine(FleetUIButton fleet, FleetMoveData moveData)
    {
        Vector3 startPos = Vector3.zero;
        Vector3 endPos = Vector3.zero;

        // 시작 위치와 끝 위치 계산
        if (_planets.TryGetValue(moveData.FromPlanetId, out GameObject fromPlanet))
        {
            startPos = fromPlanet.transform.position;
        }

        if (_planets.TryGetValue(moveData.ToPlanetId, out GameObject toPlanet))
        {
            endPos = toPlanet.transform.position;
        }

        // 애니메이션 실행
        float duration = 2f; // 2초 동안 이동
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);
            fleet.transform.position = currentPos;

            yield return null;
        }

        // 최종 위치 설정
        fleet.transform.position = endPos;
        Debug.Log($"Fleet {moveData.FleetId} movement animation completed at {endPos}");
    }

    /// <summary>
    /// 행성 소유권 업데이트 - UI 색상 변경
    /// </summary>
    /// <param name="planetId">행성 ID</param>
    /// <param name="ownerId">새 소유자 ID</param>
    public void UpdatePlanetOwnership(int planetId, int ownerId)
    {
        if (_planetButtons.TryGetValue(planetId, out PlanetUIButton planetButton))
        {
            planetButton.UpdateOwnership(ownerId);
            Debug.Log($"Planet {planetId} ownership updated to player {ownerId}");
        }
        else
        {
            Debug.LogWarning($"Cannot update planet {planetId} ownership: planet not found");
        }
    }

    /// <summary>
    /// 행성 사이의 간선(경로)을 시각화합니다.
    /// </summary>
    /// <param name="routes">간선 데이터 배열</param>
    public void CreatePlanetEdges(MapRouteInfoData[] routes)
    {
        if (routes == null || routes.Length == 0)
        {
            Debug.Log("No routes to draw.");
            return;
        }

        foreach (var route in routes)
        {
            if (_planets.TryGetValue(route.planetFromId, out GameObject fromPlanet) &&
                _planets.TryGetValue(route.planetToId, out GameObject toPlanet))
            {
                GameObject lineObj;
                LineRenderer lineRenderer;

                if (linePrefab != null)
                {
                    lineObj = Instantiate(linePrefab, edgeContainer);
                    lineRenderer = lineObj.GetComponent<LineRenderer>();
                    if (lineRenderer == null)
                    {
                        lineRenderer = lineObj.AddComponent<LineRenderer>();
                        //Debug.LogWarning($"LinePrefab {linePrefab.name} did not have a LineRenderer. One was added at runtime.");
                    }
                }
                else
                {
                    lineObj = new GameObject($"Edge_{route.planetFromId}_to_{route.planetToId}");
                    lineObj.transform.SetParent(edgeContainer);
                    lineRenderer = lineObj.AddComponent<LineRenderer>();
                    //Debug.LogWarning("VisualizationManager: linePrefab was null — created runtime LineRenderer GameObject.");
                }

                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, fromPlanet.transform.position);
                lineRenderer.SetPosition(1, toPlanet.transform.position);

                // 기본 라인 설정 (필요에 따라 조절)
                lineRenderer.startWidth = 0.1f;
                lineRenderer.endWidth = 0.1f;
                lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // 기본 셰이더
                lineRenderer.startColor = Color.white;
                lineRenderer.endColor = Color.white;

                Debug.Log($"Created edge from Planet {route.planetFromId} to Planet {route.planetToId}");
            }
            else
            {
                Debug.LogWarning($"Could not create edge for route {route.id}: one or both planets not found (From: {route.planetFromId}, To: {route.planetToId})");
            }
        }
    }

    /// <summary>
    /// 함대 선택 시각적 피드백 업데이트
    /// </summary>
    /// <param name="fleetId">선택된 함대 ID. 선택 해제 시 -1 또는 음수</param>
    public void UpdateFleetSelection(int fleetId)
    {
        if (fleetSelectionRing == null) return;

        if (fleetId >= 0 && _fleets.TryGetValue(fleetId, out FleetUIButton fleet))
        {
            fleetSelectionRing.SetActive(true);
            fleetSelectionRing.transform.position = fleet.transform.position;
            Debug.Log($"Fleet {fleetId} selected, showing selection ring.");
        }
        else
        {
            fleetSelectionRing.SetActive(false);
            Debug.Log("Fleet selection cleared.");
        }
    }

    /// <summary>
    /// 행성 선택 시각적 피드백 업데이트
    /// </summary>
    /// <param name="planetId">선택된 행성 ID. 선택 해제 시 -1 또는 음수</param>
    public void UpdatePlanetSelection(int planetId)
    {
        if (planetSelectionRing == null) return;

        if (planetId >= 0 && _planets.TryGetValue(planetId, out GameObject planet))
        {
            Debug.Log(planetId);
            planetSelectionRing.SetActive(true);
            planetSelectionRing.transform.position = planet.transform.position;
            Debug.Log($"Planet {planetId} selected, showing selection ring.");
        }
        else
        {
            planetSelectionRing.SetActive(false);
            Debug.Log("Planet selection cleared.");
        }
    }
}
