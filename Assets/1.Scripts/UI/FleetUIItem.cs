using UnityEngine;
using UnityEngine.UI;
using CommonLib;
/// <summary>
/// 함대 정보를 표시하는 UI 아이템, 
/// 클릭 시 해당 함대를 생산합니다.
/// </summary>
public class FleetUIItem : MonoBehaviour
{
    [Header("UI Components")]
    public Button selectButton;

    private FleetSpawnData _fleetData;
    private UnityGameClient _gameClient;

    public void Setup(FleetSpawnData fleetData)
    {
        _fleetData = fleetData;
        _gameClient = FindFirstObjectByType<UnityGameClient>();
        SetupButtons();
    }

    private void SetupButtons()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnSelectButtonClicked);
        }
    }

    private void OnSelectButtonClicked()
    {
        // 함대 생산 요청
        if (_gameClient != null)
        {
            _gameClient.RequestProduceFleet(_fleetData.PlanetId);
            Debug.Log($"Requested production of fleet type: {_fleetData.FleetType} at planet {_fleetData.PlanetId}");
        }
    }


}
