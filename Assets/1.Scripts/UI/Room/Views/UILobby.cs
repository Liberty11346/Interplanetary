using CommonLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UILobby : MonoBehaviour
{
    [SerializeField] private UIRoomList roomList;
    [SerializeField] private UIRoomListMenu roomMenu;
    [SerializeField] private UIWindow_CreateRoom uIWindow_CreateRoom;
    [SerializeField] private Button closeBtn;
    [SerializeField] private string prevScene;

    LobbyPresenter presenter;
    private void Awake()
    {
        // 방 입장 이벤트 구독
        if (roomList == null)
        {
            return;
        }
        presenter = new LobbyPresenter();
        presenter.OnRefreshRoomList += RefreshRoomList;

        roomList.OnRoomEnterRequested += presenter.HandleRoomEnter;

        roomMenu.OnClickedRefresh += RequestRefreshLobby;
        roomMenu.OnFilterValueChanged += HandleChangeFilter;

        uIWindow_CreateRoom.SetData(presenter.UIWindow_CreateRoomData);
        uIWindow_CreateRoom.OnClickedCreate += HandleCreateRoom;

        // 단순 종료 처리용 람다.
        closeBtn.onClick.AddListener(() => { SceneManager.LoadScene(prevScene); });
    }

    public void OnInit()
    {
        presenter.RequestRoomListFromServer();
    }

    private void RequestRefreshLobby()
    {
        presenter.RequestRefreshRoomList();
    }

    private void HandleChangeFilter(int index)
    {
        roomList.ResetFilter();

        roomList.SetFilter(presenter.IndexToState(index));
    }

    private void HandleCreateRoom(string roomName, int mapIndex, bool IsPrivate)
    {
        presenter.RequestCreateRoom(roomName, mapIndex, IsPrivate);
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
