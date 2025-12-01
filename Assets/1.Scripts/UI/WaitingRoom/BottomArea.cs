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

        // RoomManager의 방 정보 변경 이벤트 구독
        RoomManager.Instance.OnRoomInfoChanged += UpdateInfo;

        // 현재 방 정보가 있으면 초기 표시
        if (RoomManager.Instance.CurrentRoom.HasValue)
        {
            UpdateInfo(RoomManager.Instance.CurrentRoom.Value);
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnRoomInfoChanged -= UpdateInfo;
        }
    }

    // RoomManager 이벤트에서 호출됨
    // RoomInfo를 던져주면 그 정보를 토대로 텍스트 메쉬를 업데이트 한다.
    public void UpdateInfo(RoomInfo? roomInfo)
    {
        // 맵 이름 업데이트
        string mapName = "";
        switch( roomInfo?.MapID )
        {
            case 0:
                mapName = "TRAINNING SCHOOL";
                break;

            case 1:
                mapName = "RUERY SPACE";
                break;

            case 2:
                mapName = "TWISTED LIBRA SECTOR";
                break;

            default:
                mapName = "unknown";
                break;
        }
        _mapName.text = mapName;

        // 최대 플레이어 수 업데이트
        _maxPlayerValue.text = roomInfo?.MaxPlayers.ToString() + "명";
        
        // 방 상태 업데이트
        string roomState = "";
        switch( roomInfo?.RoomState )
        {
            case RoomState.Open:
                roomState = "공개";
                break;

            case RoomState.Closed:
                roomState = "비공개";
                break;
            
            default:
                roomState = "unknown";
                break;
        }
        _roomTypeValue.text = roomState;
    }
}
