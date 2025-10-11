using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public List<planetCtrl> scenePlanets = new List<planetCtrl>();

    [Tooltip("행성 정보를 저장하거나 불러올 MapData ScriptableObject를 할당해주세요.")]
    public MapData mapData;

    [Tooltip("스냅샷을 캡처할 카메라를 할당해주세요.")]
    public Camera snapshotCamera;

    /// <summary>
    /// 씬에 있는 모든 행성을 찾아 초기화합니다. (연결선, 텍스트 등)
    /// </summary>
    public void InitPlanets()
    {
        // 자식 오브젝트에 있는 모든 planetCtrl 컴포넌트를 가져옵니다.
        scenePlanets = new List<planetCtrl>(GetComponentsInChildren<planetCtrl>());

        // 가져온 모든 행성에 대해 초기화를 실행합니다.
        foreach (planetCtrl scenePlanet in scenePlanets)
        {
            scenePlanet.InitPlanet(); // InitPlanet을 호출하여 텍스트, 라인 등을 갱신합니다.
        }
    }

    /// <summary>
    /// 현재 씬의 행성 데이터를 MapData에 저장합니다.
    /// 실제 저장 로직은 MapManagerEditor에서 처리합니다.
    /// </summary>
    public void SavePlanetsToMapData()
    {
#if UNITY_EDITOR
        // 실제 구현은 MapManagerEditor에서 처리합니다.
#else
        Debug.LogWarning("행성 데이터 저장 기능은 Unity 에디터에서만 사용할 수 있습니다.");
#endif
    }

    /// <summary>
    /// MapData에서 행성 데이터를 불러와 맵을 재구성합니다.
    /// </summary>
    public void LoadMapFromData()
    {
        if (mapData == null)
        {
            Debug.LogError("MapData가 할당되지 않았습니다.");
            return;
        }

        // 1. 기존 맵의 모든 행성 데이터를 완전히 제거
        ClearAllPlanets();

        // 2. 저장된 행성 데이터를 로드하여 새로운 행성 생성
        for (int i = 0; i < mapData.planets.Count; i++)
        {
            // 행성 데이터와 위치 정보 가져오기
            PlanetData planetData = mapData.planets[i];
            Vector2 position = mapData.planetPositions[i];

            // 행성 프리팹 인스턴스 생성
            planetCtrl planet = PlanetFactory.CreatePlanetFromData(planetData, position);
            planet.transform.parent = transform;
        }

        // 3. 행성 간 연결 정보 설정
        scenePlanets = new List<planetCtrl>(GetComponentsInChildren<planetCtrl>());
        foreach (var path in mapData.planetPaths)
        {
            if (path.fromID < scenePlanets.Count && path.toID < scenePlanets.Count)
            {
                // 연결할 행성 찾기
                planetCtrl fromPlanet = scenePlanets[path.fromID];
                planetCtrl toPlanet = scenePlanets[path.toID];

                // 빈 슬롯 찾아서 연결 설정
                for (int i = 0; i < fromPlanet.nearPlanet.Length; i++)
                {
                    if (fromPlanet.nearPlanet[i] == null)
                    {
                        fromPlanet.nearPlanet[i] = toPlanet.gameObject;
                        break;
                    }
                }
            }
        }

        // 4. 모든 행성 초기화하여 정상 작동 확인
        InitPlanets();

        Debug.Log($"MapData에서 {mapData.planets.Count}개의 행성을 불러와 맵을 재구성했습니다.");
    }

    /// <summary>
    /// 현재 맵의 모든 행성을 제거합니다.
    /// </summary>
    private void ClearAllPlanets()
    {
        // 기존 행성 목록 복사 (삭제 중 컬렉션 변경 방지)
        List<planetCtrl> planetsToRemove = new List<planetCtrl>(scenePlanets);

        // 모든 행성 게임오브젝트 제거
        foreach (var planet in planetsToRemove)
        {
            if (planet != null && planet.gameObject != null)
            {
                DestroyImmediate(planet.gameObject);
            }
        }

        // 행성 목록 초기화
        scenePlanets.Clear();
    }

    /// <summary>
    /// 현재 씬의 이름을 MapData 에셋에 저장합니다.
    /// 실제 저장 로직은 MapManagerEditor에서 처리합니다.
    /// </summary>
    public void SaveSceneName()
    {
#if UNITY_EDITOR
        // 실제 구현은 MapManagerEditor에서 처리합니다.
#else
        Debug.LogWarning("씬 이름 저장 기능은 Unity 에디터에서만 사용할 수 있습니다.");
#endif
    }

    /// <summary>
    /// 할당된 카메라를 사용하여 캡처하고 MapData 에셋에 미리보기 이미지로 저장합니다.
    /// 실제 캡처 로직은 MapManagerEditor에서 처리합니다.
    /// </summary>
    /// <param name="width">캡처할 이미지의 너비</param>
    /// <param name="height">캡처할 이미지의 높이</param>
    public void CaptureMapSnapshot(int width = 512, int height = 512)
    {
#if UNITY_EDITOR
        // 실제 구현은 MapManagerEditor에서 처리합니다.
#else
        Debug.LogWarning("맵 스냅샷 캡처 기능은 Unity 에디터에서만 사용할 수 있습니다.");
#endif
    }

    /// <summary>
    /// 행성 이름으로 씬에 있는 planetCtrl을 찾습니다.
    /// </summary>
    /// <param name="name">찾을 행성의 이름</param>
    /// <returns>찾은 planetCtrl. 없으면 null을 반환합니다.</returns>
    public planetCtrl FindPlanetByName(string name)
    {
        return scenePlanets.FirstOrDefault(p => p.planetName == name);
    }
}
