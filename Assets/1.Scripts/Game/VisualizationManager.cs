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
        Debug.Log("UnifiedVisualizationManager initialized - 시각화 시스템 준비 완료");

        // 선택 링 초기화 (비활성화)
        if (fleetSelectionRing != null)
            fleetSelectionRing.SetActive(false);
        if (planetSelectionRing != null)
            planetSelectionRing.SetActive(false);
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
                planetButton.Initialize();
                _planetButtons[planetId] = planetButton;
            }
        }

        Debug.Log($"Planet {planetId} created at position {position}");
    }

    /// <summary>
    /// 함대 생성
    /// </summary>
    /// <param name="fleetId">함대 ID</param>
    /// <param name="fleetType">함대 유형</param>
    /// <param name="ownerId">소유자 ID</param>
    public void CreateFleet(int fleetId, int fleetType, int ownerId)
    {
        if (fleetPrefab != null && fleetContainer != null)
        {
            GameObject fleetObj = Instantiate(fleetPrefab, fleetContainer);
            var fleetButton = fleetObj.GetComponent<FleetUIButton>();
            if (fleetButton != null)
            {
                // 함대 데이터 생성
                int homePlanetId = GameManager.Instance.GetHomePlanetId(ownerId);
                FleetSpawnData spawnData = new FleetSpawnData
                {
                    FleetId = fleetId,
                    FleetType = fleetType,
                    OwnerId = ownerId,
                    PlanetId = homePlanetId
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
