//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using GameClient;
//using CommonLib; // RoomInfo 등 공용 타입 참조
//
//// 로비 씬 UI 컨트롤러: 룸 목록 새로고침, 룸 생성 요청을 기존 UI에 연결
//public class LobbyUIController : MonoBehaviour
//{
//    [Header("UI References")]
//    [SerializeField] private Button refreshButton;
//    [SerializeField] private TMP_InputField roomNameInput;
//    [SerializeField] private TMP_Dropdown roomSelectInput;
//    [SerializeField] private Toggle roomPrivateToggle;
//    [SerializeField] private Button createRoomButton;
//
//    [Header("Optional: Room List")]
//    [SerializeField] private Transform roomListContent;
//
//    private void Awake()
//    {
//        // 버튼 리스너 연결
//        refreshButton?.onClick.AddListener(OnClickRefreshLobby);
//
//        if (createRoomButton != null)
//            createRoomButton.onClick.AddListener(OnClickCreateRoom);
//        else
//        {
//            roomNameInput?.onSubmit.AddListener(_ => OnClickCreateRoom());
//        }
//    }
//
//    /*
//    private async void Start()
//    {
//        var manager = RoomManager.Instance;
//        manager.OnRoomListUpdated += HandleRoomListUpdated;
//
//        await SafeCall(async () =>
//        {
//            if (!ClientServerHandler.Instance.IsConnected)
//            {
//                await ClientServerHandler.Instance.ConnectAsync("127.0.0.1", 7777);
//            }
//            await manager.RequestJoinLobbyAsync(0);
//        }, "로비 접속");
//    }
//    */
//
//    private void Start() // ?????
//    {
//        var manager = RoomManager.Instance;
//        manager.OnRoomListUpdated += HandleRoomListUpdated;
//        if (!ClientServerHandler.Instance.IsConnected)
//        {
//            ClientServerHandler.Instance.ConnectAsync("127.0.0.1", 7777)
//                .ContinueWith(t =>
//                {
//                    if (t.IsFaulted)
//                    {
//                        var ex = t.Exception?.GetBaseException();
//                        Debug.LogError($"[로비 접속] 오류: {ex?.Message}");
//                        return;
//                    }
//                    manager.JoinLobby(0);
//                }, TaskScheduler.FromCurrentSynchronizationContext());
//        }
//        else
//        {
//            manager.JoinLobby(0);
//        }
//    }
//
//    private void OnClickRefreshLobby()
//    {
//        RoomManager.Instance.RefreshRoomList();
//    }
//
//    private void OnClickCreateRoom()
//    {
//        string roomName = roomNameInput != null ? roomNameInput.text : "새로운 방";
//        string mapId = GetSelectedMapId();
//        bool isPrivate = roomPrivateToggle != null && roomPrivateToggle.isOn;
//
//        RoomManager.Instance.CreateRoom(roomName, mapId, isPrivate);
//        RoomManager.Instance.RefreshRoomList();
//    }
//
//    private void HandleRoomListUpdated(List<RoomInfo> rooms)
//    {
//        Debug.Log($"[Lobby] 수신한 룸 개수: {rooms.Count}");
//    }
//
//    private string GetSelectedMapId()
//    {
//        if (roomSelectInput == null || roomSelectInput.options.Count == 0)
//            return "game1"; // 기본 맵 ID
//
//        var option = roomSelectInput.options[roomSelectInput.value];
//        return option.text;
//    }
//
//    private async Task SafeCall(Func<Task> action, string actionName)
//    {
//        try
//        {
//            await action();
//        }
//        catch (Exception e)
//        {
//            Debug.LogError($"[{actionName}] 오류: {e.Message}");
//        }
//    }
//}
