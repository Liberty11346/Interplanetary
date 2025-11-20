using UnityEngine;

public class UILobby : MonoBehaviour
{
    [SerializeField] private UIRoomList roomList;

    LobbyPresenter presenter;
    private void Start()
    {
        // 방 입장 이벤트 구독
        if (roomList == null)
        {
            return;
        }
        presenter.OnRefreshRoomList += RefreshRoomList;
        roomList.OnRoomEnterRequested += presenter.HandleRoomEnter;
        presenter.RequestRoomListFromServer();
    }

    public void RequestRefreshLobby()
    {
        presenter.RequestRoomListFromServer();
    }

    private void RefreshRoomList(UIRoomList.Data roomData)
    {
        if (roomData != null)
        {
            Debug.LogError("roomDataNull!!!");
            return;
        }
        presenter.OnRoomUpdated += roomList.UpdateRoom;
        presenter.OnRoomCreated += roomList.AddRoom;
        presenter.OnRoomClosed += roomList.RemoveRoom;
        roomList.SetData(roomData);
    }

    private void OnDestroy()
    {
        if (roomList != null)
        {
            roomList.OnRoomEnterRequested -= presenter.HandleRoomEnter;
        }
    }
}
