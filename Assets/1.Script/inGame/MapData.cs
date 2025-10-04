using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 맵의 전체 행성 데이터를 저장하는 ScriptableObject입니다.
/// </summary>
[CreateAssetMenu(fileName = "New Map Data", menuName = "Interplanetary/Map Data")]
public class MapData : ScriptableObject
{
    public string realMapName; // 실제 게임할 씬의 이름
    public string displayMapName; // 플레이어에게 보이는 맵 이름
    [TextArea] public string displayExplain; // 플레이어에게 보여지는 맵 설명

    [Tooltip("맵 선택 화면 등에서 사용할 미리보기 이미지입니다.")]
    public Texture2D mapPreviewImage;
}