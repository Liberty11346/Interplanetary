using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 행성 정적 데이터 (GameSet에서 받아서 고정)
/// </summary>
public class PlanetStaticData
{
    public int PlanetId;
    public Vector2 Position;  // 고정 위치
    public int MaxMinerals;
    public int MaxGas;
    public List<int> ConnectedPlanetIds = new List<int>(); // 연결된 행성
}
