using CommonLib;
using TMPro;
using UnityEngine;

public class TopArea : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _roomNameTMP,
                            _mapNameTMP;

    public void UpdateUI(ConvertedRoomInfo roomInfo)
    {
        _roomNameTMP.text = roomInfo.origin.RoomName;
        _mapNameTMP.text = roomInfo.mapName;
    }
}
