using UnityEngine;

/// <summary>
/// 경로 UI 레이어 - 모든 경로 UI를 관리
/// </summary>
public class UIPath_Layer : UIEntityLayerBase<UIPath, PathData>
{
    protected override void InitializeEntity(UIPath entity, int id, PathData data)
    {
        // 초기화 (위치, 색상 설정)
        entity.Initialize(data);
    }

    protected override void UpdateEntity(UIPath entity, PathData data)
    {
        // 색상 등 데이터만 업데이트
        entity.SetData(data);
    }

    protected override void OnEntityRemoved(UIPath entity, int id)
    {
        // 경로는 단순히 제거
    }
}
