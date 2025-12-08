using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PlanetLibrary", menuName = "GameData/Planet Library")]
public class PlanetLibrary : ScriptableObject
{
    // 행성 하나하나의 정보를 담을 클래스
    [System.Serializable]
    public class PlanetInfo
    {
        public string id;           // 고유 ID (예: "planet_earth", "1001")
        public string displayName;  // 화면에 표시될 이름 (예: "지구")
        public Sprite icon;         // 행성 이미지
        [TextArea]
        public string description;  // (선택사항) 행성 설명
    }

    // 인스펙터에서 데이터를 입력할 리스트
    [SerializeField]
    private List<PlanetInfo> planetList = new List<PlanetInfo>();

    // 빠른 검색을 위한 딕셔너리
    private Dictionary<string, PlanetInfo> planetDictionary;

    // 초기화 함수 (딕셔너리 생성)
    private void InitializeDictionary()
    {
        planetDictionary = new Dictionary<string, PlanetInfo>();

        foreach (var planet in planetList)
        {
            // ID 중복 체크 (실수 방지)
            if (string.IsNullOrEmpty(planet.id)) continue;

            if (!planetDictionary.ContainsKey(planet.id))
            {
                planetDictionary.Add(planet.id, planet);
            }
            else
            {
                Debug.LogWarning($"중복된 행성 ID가 발견되었습니다: {planet.id}");
            }
        }
    }

    // ID로 행성 정보를 가져오는 함수
    public PlanetInfo GetPlanetInfo(string id)
    {
        // 딕셔너리가 없으면 생성 (Lazy Initialization)
        if (planetDictionary == null)
        {
            InitializeDictionary();
        }

        if (planetDictionary.TryGetValue(id, out PlanetInfo info))
        {
            return info;
        }

        Debug.LogError($"해당 ID의 행성을 찾을 수 없습니다: {id}");
        return null;
    }

    // ID로 이미지만 바로 가져오는 편의 함수
    public Sprite GetPlanetSprite(string id)
    {
        var info = GetPlanetInfo(id);
        return info != null ? info.icon : null;
    }
}
