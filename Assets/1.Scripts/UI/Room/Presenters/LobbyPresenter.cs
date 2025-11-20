using CommonLib;
using GameClient;
using UnityEngine;
using System.Collections.Generic;
using System.Xml.Linq;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine.InputSystem;
using System;
using System.Linq;
using UnityEditor.EditorTools;

public class LobbyPresenter
{
    public System.Action<UIElement_Room.Data> OnRoomUpdated;
    public System.Action<UIElement_Room.Data> OnRoomCreated;
    public System.Action<string> OnRoomClosed;

    public System.Action<RoomInfo> OnResponseJoinRoom;
    public System.Action<UIRoomList.Data> OnRefreshRoomList;

    RoomManager roomManager;
    List<RoomState> stateValues = new List<RoomState>();
    UIWindow_CreateRoom.Data createRoomData = new UIWindow_CreateRoom.Data();

    public LobbyPresenter()
    {
        roomManager = RoomManager.Instance;
        roomManager.OnRoomJoinSuccess += RoomManager_OnRoomJoinSuccess;
        roomManager.OnRoomListUpdated += RoomManager_OnRoomListUpdated;

        foreach (var item in Enum.GetValues(typeof(RoomState)).Cast<RoomState>().ToList())
        {
            stateValues.Add(item);
        }

        createRoomData.defaultRoomName = "나의 방";
        // 나중에 DB에서 테이블 불러오면 그거로 세팅하기
        createRoomData.maps = new List<string>()
        {
            "RUERY SPACE",
            "TWISTED LIBRA"
        };
    }
    public UIWindow_CreateRoom.Data UIWindow_CreateRoomData => createRoomData;

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

    public void RequestRefreshRoomList()
    {
        roomManager.RefreshRoomList();
    }

    public void HandleRoomEnter(string roomId)
    {
        Debug.Log($"LobbyController: Attempting to enter room {roomId}");

        // 서버에 방 입장 요청
        roomManager.RequestJoinRoomAsync(roomId);
    }

    public RoomState IndexToState(int index)
    {
        if (stateValues.Count <= index || index < 0)
            return RoomState.None;
        return stateValues[index];
    }

    public void RequestCreateRoom(string roomName, int mapIndex, bool IsPrivate)
    {
        roomManager.RequestCreateRoomAsync(roomName, mapIndex, IsPrivate);
    }
}
