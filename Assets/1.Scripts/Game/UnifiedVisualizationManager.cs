using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonLib;

/// <summary>
/// 통합 시각화 매니저 - 게임 내 모든 시각적 요소 관리
/// GameManager로부터 데이터를 받아 시각적 표현만 담당
/// </summary>
public class UnifiedVisualizationManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject planetPrefab;          // 행성 3D 오브젝트 프리팹
    public GameObject fleetPrefab;           // 함대 3D 오브젝트 프리팹
    public GameObject planetUIButtonPrefab;  // 행성 UI 버튼 프리팹
    public GameObject fleetUIItemPrefab;     // 함대 UI 아이템 프리팹

    [Header("UI Containers")]
    public Transform planetContainer;        // 행성 3D 오브젝트 컨테이너
    public Transform fleetContainer;         // 함대 3D 오브젝트 컨테이너
    public Transform planetUIContainer;      // 행성 UI 버튼 컨테이너
    public Transform fleetUIContainer;       // 함대 UI 아이템 컨테이너

    [Header("Map Settings")]
    public Vector3[] planetPositions = {     // 테스트용 행성 위치
        new Vector3(-5, 0, 0),
        new Vector3(5, 0, 0),
        new Vector3(0, 0, 5),
        new Vector3(0, 0, -5)
    };

    [Header("Selection")]
    public GameObject fleetSelectionRing;    // 함대 선택 표시 링
    public GameObject planetSelectionRing;   // 행성 선택 표시 링

    [Header("Camera")]
    public Camera gameCamera;                // 게임 카메라 참조

    // 시각화 오브젝트 캐싱
    private Dictionary<int, GameObject> _planets = new Dictionary<int, GameObject>();        // 행성 ID -> 행성 오브젝트
    private Dictionary<int, FleetUIItem> _fleets = new Dictionary<int, FleetUIItem>();       // 함대 ID -> 함대 UI 아이템
    private Dictionary<int, PlanetUIButton> _planetButtons = new Dictionary<int, PlanetUIButton>(); // 행성 ID -> 행성 UI 버튼

    /// <summary>
    /// 컴포넌트 초기화 및 테스트 행성 생성
    /// </summary>
    private void Start()
    {
        // 시각화 초기화
        InitializeVisualization();
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

        // 테스트용 행성 생성 (실제 게임에서는 GameManager에서 데이터 수신 후 생성)
        for (int i = 0; i < planetPositions.Length; i++)
        {
            CreatePlanet(i + 1, planetPositions[i]);
        }
    }

    /// <summary>
    /// 행성 생성 - 3D 오브젝트 및 UI 버튼 생성
    /// </summary>
    /// <param name="planetId">행성 ID</param>
    /// <param name="position">행성 위치</param>
    public void CreatePlanet(int planetId, Vector3 position)
    {
        // 3D 행성 오브젝트 생성
        if (planetPrefab != null && planetContainer != null)
        {
            GameObject planetObj = Instantiate(planetPrefab, position, Quaternion.identity, planetContainer);
            planetObj.name = $"Planet_{planetId}";
            _planets[planetId] = planetObj;
        }

        // UI 버튼 생성
        if (planetUIButtonPrefab != null && planetUIContainer != null)
        {
            GameObject buttonObj = Instantiate(planetUIButtonPrefab, planetUIContainer);
            var planetButton = buttonObj.GetComponent<PlanetUIButton>();
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
    /// 함대 생성 - UI 아이템 생성 및 초기 위치 설정
    /// </summary>
    /// <param name="fleetId">함대 ID</param>
    /// <param name="planetId">소속 행성 ID</param>
    /// <param name="fleetType">함대 유형</param>
    /// <param name="ownerId">소유자 ID</param>
    public void CreateFleet(int fleetId, int planetId, int fleetType, int ownerId)
    {
        if (fleetUIItemPrefab != null && fleetUIContainer != null)
        {
            GameObject fleetObj = Instantiate(fleetUIItemPrefab, fleetUIContainer);
            var fleetItem = fleetObj.GetComponent<FleetUIItem>();
            if (fleetItem != null)
            {
                // 함대 데이터 생성
                FleetSpawnData spawnData = new FleetSpawnData
                {
                    FleetId = fleetId,
                    PlanetId = planetId,
                    FleetType = fleetType,
                    OwnerId = ownerId
                };

                fleetItem.Setup(spawnData);
                _fleets[fleetId] = fleetItem;

                // 함대 위치를 행성 위치로 설정
                if (_planets.TryGetValue(planetId, out GameObject planet))
                {
                    fleetItem.transform.position = planet.transform.position;
                }

                Debug.Log($"Fleet {fleetId} created at planet {planetId} (Owner: {ownerId}, Type: {fleetType})");
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
        if (_fleets.TryGetValue(fleetId, out FleetUIItem fleet))
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
    private IEnumerator AnimateFleetMovementCoroutine(FleetUIItem fleet, FleetMoveData moveData)
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
    /// 행성 시각적 효과 표시 (선택, 강조 등)
    /// </summary>
    /// <param name="planetId">행성 ID</param>
    /// <param name="effectType">효과 유형 (0: 없음, 1: 선택, 2: 강조)</param>
    public void ShowPlanetEffect(int planetId, int effectType)
    {
        if (_planets.TryGetValue(planetId, out GameObject planet))
        {
            // 효과 유형에 따라 시각적 효과 적용
            // 예: 선택 링 표시, 파티클 효과 등
            if (effectType == 1 && planetSelectionRing != null)
            {
                planetSelectionRing.SetActive(true);
                planetSelectionRing.transform.position = planet.transform.position;
            }
        }
    }

    /// <summary>
    /// 함대 시각적 효과 표시 (선택, 강조 등)
    /// </summary>
    /// <param name="fleetId">함대 ID</param>
    /// <param name="effectType">효과 유형 (0: 없음, 1: 선택, 2: 강조)</param>
    public void ShowFleetEffect(int fleetId, int effectType)
    {
        if (_fleets.TryGetValue(fleetId, out FleetUIItem fleet))
        {
            // 효과 유형에 따라 시각적 효과 적용
            if (effectType == 1 && fleetSelectionRing != null)
            {
                fleetSelectionRing.SetActive(true);
                fleetSelectionRing.transform.position = fleet.transform.position;
            }
        }
    }

    /// <summary>
    /// 모든 시각적 효과 제거
    /// </summary>
    public void ClearAllEffects()
    {
        if (planetSelectionRing != null)
            planetSelectionRing.SetActive(false);

        if (fleetSelectionRing != null)
            fleetSelectionRing.SetActive(false);
    }
}
