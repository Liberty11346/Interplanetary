using CommonLib;
using GameClient;
using UnityEngine;
using System.Collections.Generic;

public class LobbyPresenter
{
    public System.Action<UIElement_Room.Data> OnRoomUpdated;
    public System.Action<UIElement_Room.Data> OnRoomCreated;
    public System.Action<string> OnRoomClosed;

    public System.Action<RoomInfo> OnResponseJoinRoom;
    public System.Action<UIRoomList.Data> OnRefreshRoomList;

    RoomManager roomManager;

    public LobbyPresenter()
    {
        roomManager = RoomManager.Instance;
        roomManager.OnRoomJoinSuccess += RoomManager_OnRoomJoinSuccess;
        roomManager.OnRoomListUpdated += RoomManager_OnRoomListUpdated;
    }

    private void RoomManager_OnRoomListUpdated(List<RoomInfo> obj)
    {
        // 받아온 RoomInfo 쪼개서 원하는 데이터로 변환

        var roomData = new UIRoomList.Data();
        List<UIElement_Room.Data> temp = new List<UIElement_Room.Data>();
        foreach (var item in obj)
        {
            UIElement_Room.Data data =
                new UIElement_Room.Data
                {
                    roomId = item.RoomId,
                    roomName = item.RoomName,
                    playerCount = item.PlayerCount,
                    playerMaxCount = item.MaxPlayers,
                    mapName = item.MapID.ToString(),
                    roomState = item.RoomState
                };

            temp.Add(data);
        }
        roomData.roomDatas = temp.ToArray();

        OnRefreshRoomList?.Invoke(roomData);
    }

    private void RoomManager_OnRoomJoinSuccess(RoomInfo obj)
    {
        OnResponseJoinRoom?.Invoke(obj);
    }

    public void RequestRoomListFromServer()
    {
        roomManager.RequestJoinLobbyAsync();
    }

    public void HandleRoomEnter(string roomId)
    {
        Debug.Log($"LobbyController: Attempting to enter room {roomId}");

        // 서버에 방 입장 요청
        roomManager.RequestJoinRoomAsync(roomId);
    }
}
