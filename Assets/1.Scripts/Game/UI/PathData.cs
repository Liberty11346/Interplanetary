using UnityEngine;

/// <summary>
/// 행성 간 경로 데이터
/// </summary>
[System.Serializable]
public struct PathData
{
    public int PathId;          // 경로 고유 ID (FromPlanetId * 1000 + ToPlanetId 등)
    public int FromPlanetId;    // 시작 행성 ID
    public int ToPlanetId;      // 도착 행성 ID
    public Vector2 FromPosition; // 시작 위치
    public Vector2 ToPosition;   // 도착 위치
    public Color LineColor;      // 선 색상 (소유자에 따라)
}
