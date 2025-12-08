using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    // 1. 싱글톤 인스턴스
    public static ResourceManager Instance { get; private set; }

    [Header("Data Libraries")]
    // 아까 만든 행성 라이브러리를 인스펙터에서 연결
    [SerializeField] private PlanetLibrary planetLibrary;
    [SerializeField] private FleetLibrary[] fleetLibraries;

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않음
        }
        else
        {
            Destroy(gameObject); // 중복 생성 방지
        }
    }

    // --- 외부에서 호출할 메서드들 ---

    // 행성 ID로 스프라이트 가져오기 (Wrapper 메서드)
    public Sprite GetPlanetSprite(string id)
    {
        if (planetLibrary == null)
        {
            Debug.LogError("PlanetLibrary가 연결되지 않았습니다!");
            return null;
        }
        return planetLibrary.GetPlanetSprite(id);
    }

    public Sprite GetFleetSprite(int index, string id)
    {
        if(index >= fleetLibraries.Length || index < 0)
        {
            Debug.LogError($"FleetLibrary 인덱스 '{index}'가 유효하지 않습니다!");
            return null;
        }

        var library = fleetLibraries[index];
        if (library == null)
        {
            Debug.LogError($"FleetLibrary 인덱스 '{index}'에 라이브러리가 연결되지 않았습니다!");
            return null;
        }
        var sprite = library.GetFleetSprite(id);
        if (sprite != null)
        {
            return sprite;
        }
        Debug.LogError($"Fleet ID '{id}'에 해당하는 스프라이트를 찾을 수 없습니다!");
        return null;
    }

    // 행성 전체 정보 가져오기
    public PlanetLibrary.PlanetInfo GetPlanet(string id)
    {
        if (planetLibrary == null) return null;
        return planetLibrary.GetPlanetInfo(id);
    }
}
