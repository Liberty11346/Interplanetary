using UnityEngine;

/// <summary>
/// 함대 UI 레이어 - 모든 함대 UI를 관리
/// </summary>
public class UIFleet_Layer : UIEntityLayerBase<UIFleet, GamePlayManager.GameState.FleetInfo>
{
    public System.Action<int, int> OnClickFleet;

    protected override void InitializeEntity(UIFleet entity, int id, GamePlayManager.GameState.FleetInfo data)
    {
        // 초기화 (보간 없이 즉시 위치 설정)
        entity.Initialize(data);
        
        // 클릭 이벤트 연결
        entity.OnClickFleet = OnFleetClicked;
    }

    protected override void UpdateEntity(UIFleet entity, GamePlayManager.GameState.FleetInfo data)
    {
        // 매 틱마다 전체 데이터 업데이트
        entity.SetData(data);
    }

    protected override void OnEntityRemoved(UIFleet entity, int id)
    {
        // 파괴 애니메이션 재생
        entity.PlayDestroyAnimation();
    }

    /// <summary>
    /// 함대 클릭 이벤트 핸들러
    /// </summary>
    private void OnFleetClicked(int fleetId, int ownerId)
    {
        OnClickFleet?.Invoke(fleetId, ownerId);
    }
}
