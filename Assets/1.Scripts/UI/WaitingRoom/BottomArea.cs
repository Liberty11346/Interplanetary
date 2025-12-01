using CommonLib;
using TMPro;
using UnityEngine;

// WaitingRoom 씬 내부에서 현재 방 상태를 보여준다.
public class BottomArea : MonoBehaviour
{
    private TextMeshProUGUI _mapName, // 맵 이름을 표시할 텍스트 메쉬
                            _maxPlayerValue, // 최대 플레이어 수를 표시할 텍스트 메쉬
                            _roomTypeValue; // 방 상태를 표시할 텍스트 메쉬

    private void Start()
    {
        // 본인의 자식 텍스트 메쉬 참조
        _mapName = transform.Find("MapName").GetComponent<TextMeshProUGUI>();
        _maxPlayerValue = transform.Find("MaxPlayerValue").GetComponent<TextMeshProUGUI>();
        _roomTypeValue = transform.Find("RoomTypeValue").GetComponent<TextMeshProUGUI>();
    }

    // RoomManager 이벤트에서 호출됨
    // RoomInfo를 던져주면 그 정보를 토대로 텍스트 메쉬를 업데이트 한다.
    public void UpdateUI(ConvertedRoomInfo roomInfo)
    {
        // 맵 이름 업데이트
        _mapName.text = roomInfo.mapName;

        // 최대 플레이어 수 업데이트
        _maxPlayerValue.text = roomInfo.origin.MaxPlayers.ToString() + "명";
        
        // 방 상태 업데이트
        _roomTypeValue.text = roomInfo.roomState;
    }
}
