using UnityEngine;
using CommonLib;

/// <summary>
/// 행성 UI 레이어 - 모든 행성 UI를 관리
/// </summary>
public class UIPlanet_Layer : UIEntityLayerBase<UIPlanet, PlanetData>
{
    public System.Action<int, int> OnClickPlanet;
    protected override void InitializeEntity(UIPlanet entity, int id, PlanetData data)
    {
        // 초기화 (위치, 스프라이트 설정)
        entity.Initialize(data);
        
        // 클릭 이벤트 연결
        entity.OnClickPlanet = OnPlanetClicked;
    }

    protected override void UpdateEntity(UIPlanet entity, PlanetData data)
    {
        // 자원 등 데이터만 업데이트
        entity.SetData(data);
    }

    protected override void OnEntityRemoved(UIPlanet entity, int id)
    {
        // 행성은 파괴되지 않으므로 특별한 처리 없음
        // 필요시 애니메이션 추가 가능
    }

    /// <summary>
    /// 행성 클릭 이벤트 핸들러
    /// </summary>
    private void OnPlanetClicked(int planetId, int ownerId)
    {
        OnClickPlanet?.Invoke(planetId, ownerId);
    }
}
