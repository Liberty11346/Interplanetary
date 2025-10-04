using UnityEngine;

/// <summary>
/// 행성 하나의 데이터를 정의하는 직렬화 가능한 클래스입니다.
/// ScriptableObject나 다른 클래스에서 행성 정보를 저장하고 관리하는 데 사용됩니다.
/// </summary>
[System.Serializable]
public class PlanetData
{
    public string planetName;
    public Vector2 position;
    public Sprite sprite;
    public int mineralAmount;
    public int gasAmount;
    public int supplyAmount;
    // MapData의 행성 리스트 내에서 연결될 다른 행성들의 인덱스 배열
    public int[] nearPlanetIndices;
}