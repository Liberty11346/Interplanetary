using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 맵의 전체 행성 데이터를 저장하는 ScriptableObject입니다.
/// </summary>
[CreateAssetMenu(fileName = "New Map Data", menuName = "Interplanetary/Map Data")]
public class MapData : ScriptableObject
{
    public bool isTuto;
    public string realMapName; // 실제 게임할 씬의 이름
    public string displayMapName; // 플레이어에게 보이는 맵 이름
    [TextArea] public string displayExplain; // 플레이어에게 보여지는 맵 설명

    [Tooltip("맵 선택 화면 등에서 사용할 미리보기 이미지입니다.")]
    public Texture2D mapPreviewImage;

    [Header("행성 데이터")]
    [Tooltip("맵에 포함된 모든 행성 데이터입니다.")]
    public List<PlanetData> planets = new List<PlanetData>();

    [Tooltip("각 행성의 위치 정보입니다. planets 리스트와 인덱스가 일치합니다.")]
    public List<Vector2> planetPositions = new List<Vector2>();

    [Tooltip("행성 간의 연결 정보입니다.")]
    public List<PlanetPath> planetPaths = new List<PlanetPath>();
}