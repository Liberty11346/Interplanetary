using CommonLib;
using UnityEngine;

public class LobbyController : MonoBehaviour
{
    [SerializeField] private UIRoomList roomList;

    private void Start()
    {
        // 방 입장 이벤트 구독
        if (roomList != null)
        {
            roomList.OnRoomEnterRequested += HandleRoomEnter;
        }

        // 서버에서 방 목록 받아오기 (예시)
        RequestRoomListFromServer();
    }

    private void OnDestroy()
    {
        if (roomList != null)
        {
            roomList.OnRoomEnterRequested -= HandleRoomEnter;
        }
    }

    private void HandleRoomEnter(int roomId)
    {
        Debug.Log($"LobbyController: Attempting to enter room {roomId}");

        // 서버에 방 입장 요청
        SendJoinRoomRequest(roomId);
    }

    private void RequestRoomListFromServer()
    {
        // 서버 통신 예시 (실제로는 네트워크 코드 필요)
        // TODO: 서버에서 방 목록 받아오기

        // 테스트용 더미 데이터
        var dummyData = new UIRoomList.Data
        {
            roomDatas = new UIElement_Room.Data[]
            {
                new UIElement_Room.Data
                {
                    roomId = 1,
                    roomName = "초보자 환영 방",
                    playerCount = 2,
                    playerMaxCount = 4,
                    mapName = "Forest",
                    roomState = RoomState.Open
                },
                new UIElement_Room.Data
                {
                    roomId = 2,
                    roomName = "랭크 게임",
                    playerCount = 4,
                    playerMaxCount = 4,
                    mapName = "Arena",
                    roomState = RoomState.Full
                },
                new UIElement_Room.Data
                {
                    roomId = 3,
                    roomName = "프리 플레이",
                    playerCount = 3,
                    playerMaxCount = 8,
                    mapName = "City",
                    roomState = RoomState.Ingame
                }
            }
        };

        roomList.SetData(dummyData);
    }

    private void SendJoinRoomRequest(int roomId)
    {
        // TODO: 실제 서버 통신 코드
        Debug.Log($"Sending join request for room {roomId}");

        // 예시: 성공 시 씬 전환
        // SceneManager.LoadScene("GameScene");
    }

    // 서버로부터 방 정보 업데이트 받을 때
    public void OnRoomUpdated(UIElement_Room.Data data)
    {
        roomList.UpdateRoom(data);
    }

    // 새 방 추가
    public void OnRoomCreated(UIElement_Room.Data data)
    {
        roomList.AddRoom(data);
    }

    // 방 제거
    public void OnRoomClosed(int roomId)
    {
        roomList.RemoveRoom(roomId);
    }
}
