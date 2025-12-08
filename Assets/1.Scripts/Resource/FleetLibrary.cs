using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "FleetLibrary", menuName = "GameData/Fleet Library")]
public class FleetLibrary : ScriptableObject
{
    // 함대 하나하나의 정보를 담을 클래스
    [System.Serializable]
    public class FleetInfo
    {
        public string id;           // 고유 ID (예: "fleet_blue", "0")
        public string displayName;  // 화면에 표시될 이름 (예: "블루 함대")
        public Sprite icon;         // 함대 이미지
        [TextArea]
        public string description;  // (선택사항) 함대 설명
    }

    // 인스펙터에서 데이터를 입력할 리스트
    [SerializeField]
    private List<FleetInfo> fleetList = new List<FleetInfo>();

    // 빠른 검색을 위한 딕셔너리
    private Dictionary<string, FleetInfo> fleetDictionary;

    // 초기화 함수 (딕셔너리 생성)
    private void InitializeDictionary()
    {
        fleetDictionary = new Dictionary<string, FleetInfo>();

        foreach (var fleet in fleetList)
        {
            // ID 중복 체크 (실수 방지)
            if (string.IsNullOrEmpty(fleet.id)) continue;

            if (!fleetDictionary.ContainsKey(fleet.id))
            {
                fleetDictionary.Add(fleet.id, fleet);
            }
            else
            {
                Debug.LogWarning($"중복된 함대 ID가 발견되었습니다: {fleet.id}");
            }
        }
    }

    // ID로 함대 정보를 가져오는 함수
    public FleetInfo GetFleetInfo(string id)
    {
        // 딕셔너리가 없으면 생성 (Lazy Initialization)
        if (fleetDictionary == null)
        {
            InitializeDictionary();
        }

        if (fleetDictionary.TryGetValue(id, out FleetInfo info))
        {
            return info;
        }

        Debug.LogError($"해당 ID의 함대를 찾을 수 없습니다: {id}");
        return null;
    }

    // ID로 이미지만 바로 가져오는 편의 함수
    public Sprite GetFleetSprite(string id)
    {
        var info = GetFleetInfo(id);
        return info != null ? info.icon : null;
    }
}
