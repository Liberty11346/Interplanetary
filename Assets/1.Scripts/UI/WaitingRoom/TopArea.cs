using CommonLib;
using TMPro;
using UnityEngine;

public class TopArea : MonoBehaviour
{
    private TextMeshProUGUI _roomNameTMP,
                            _mapNameTMP;
    private void Start()
    {
        _roomNameTMP = transform.Find("RoomName").GetComponent<TextMeshProUGUI>();
        _mapNameTMP = transform.Find("MapName").GetComponent<TextMeshProUGUI>();        
    }

    public void UpdateUI(ConvertedRoomInfo roomInfo)
    {
        _roomNameTMP.text = roomInfo.origin.RoomName;
        _mapNameTMP.text = roomInfo.mapName;
    }
}
