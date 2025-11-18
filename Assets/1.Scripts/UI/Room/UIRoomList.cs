using System;
using System.Collections.Generic;
using System.Linq;
using CommonLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIRoomList : MonoBehaviour
{
    [Serializable]
    public class Data
    {
        public UIElement_Room.Data[] roomDatas;
    }

    [Header("Prefab & Layout")]
    [SerializeField] private UIElement_Room roomPrefab;
    [SerializeField] private Transform layout;

    [Header("Pool Settings")]
    [SerializeField] private Transform poolContainer;
    [SerializeField] private int initialPoolSize = 10;

    [Header("Controller")]
    [SerializeField] private TMP_Dropdown filterDropdown;

    [Header("Debug Info")]
    [SerializeField] private bool showDebugInfo = false;

    private ObjectPool<UIElement_Room> roomPool;
    private Dictionary<int, UIElement_Room> roomDictionary = new Dictionary<int, UIElement_Room>();
    private RoomState currentFilter = RoomState.All;

    // 방 입장 이벤트
    public System.Action<int> OnRoomEnterRequested;

    #region Unity Lifecycle

    private void Awake()
    {
        InitializePool();
        InitializeDropdown();
    }

    private void OnDestroy()
    {
        ClearRooms();
        roomPool?.Clear();

        if (filterDropdown != null)
        {
            filterDropdown.onValueChanged.RemoveListener(OnFilterDropdownChanged);
        }
    }

    #endregion

    #region Initialization

    private void InitializePool()
    {
        if (roomPrefab == null)
        {
            Debug.LogError("UIRoomList: Room Prefab is not assigned!");
            return;
        }

        if (layout == null)
        {
            Debug.LogError("UIRoomList: Layout is not assigned!");
            return;
        }

        if (poolContainer == null)
        {
            GameObject poolObj = new GameObject("RoomPool");
            poolObj.transform.SetParent(transform);
            poolContainer = poolObj.transform;
        }

        roomPool = new ObjectPool<UIElement_Room>(roomPrefab, poolContainer, initialPoolSize);

        if (showDebugInfo)
        {
            Debug.Log($"UIRoomList: Pool initialized with {initialPoolSize} objects.");
        }
    }

    private void InitializeDropdown()
    {
        if (filterDropdown == null)
        {
            Debug.LogWarning("UIRoomList: Filter Dropdown is not assigned!");
            return;
        }

        filterDropdown.ClearOptions();

        List<string> options = new List<string>
        {
            "전체",              // All
            "입장 가능",         // Open
            "인원 마감",         // Full
            "게임 중",           // Ingame
            "비활성",            // Disabled
            "종료됨",            // Closed
            "오류",              // Error
            "--- 프리셋 ---",
            "참가 가능한 방",    // Joinable (Open)
            "활성화된 방"        // Active (Open | Full | Ingame)
        };

        filterDropdown.AddOptions(options);
        filterDropdown.value = 0;
        filterDropdown.onValueChanged.AddListener(OnFilterDropdownChanged);

        if (showDebugInfo)
        {
            Debug.Log("UIRoomList: Dropdown initialized.");
        }
    }

    #endregion

    #region Dropdown Event Handler

    private void OnFilterDropdownChanged(int index)
    {
        // 구분선 체크
        if (index == 7) // "--- 프리셋 ---"
        {
            filterDropdown.value = 0;
            return;
        }

        RoomState newFilter = GetFilterFromDropdownIndex(index);
        SetFilter(newFilter);

        if (showDebugInfo)
        {
            Debug.Log($"UIRoomList: Dropdown changed to index {index} - Filter: {newFilter}");
        }
    }

    private RoomState GetFilterFromDropdownIndex(int index)
    {
        switch (index)
        {
            case 0: return RoomState.All;
            case 1: return RoomState.Open;
            case 2: return RoomState.Full;
            case 3: return RoomState.Ingame;
            case 4: return RoomState.Disabled;
            case 5: return RoomState.Closed;
            case 6: return RoomState.Error;
            case 8: return RoomState.Open; // Joinable
            case 9: return RoomState.Open | RoomState.Full | RoomState.Ingame; // Active
            default: return RoomState.All;
        }
    }

    #endregion

    #region Public Methods - Data Management

    public void SetData(Data data)
    {
        ClearRooms();

        if (data?.roomDatas == null || data.roomDatas.Length == 0)
        {
            Debug.LogWarning("UIRoomList: SetData received null or empty data.");
            return;
        }

        foreach (var roomData in data.roomDatas)
        {
            AddRoom(roomData);
        }

        if (showDebugInfo)
        {
            Debug.Log($"UIRoomList: Loaded {data.roomDatas.Length} rooms.");
        }
    }

    public void AddRoom(UIElement_Room.Data data)
    {
        if (roomPool == null)
        {
            Debug.LogError("UIRoomList: Room pool is not initialized!");
            return;
        }

        if (roomDictionary.ContainsKey(data.roomId))
        {
            Debug.LogWarning($"UIRoomList: Room {data.roomId} already exists. Use UpdateRoom instead.");
            UpdateRoom(data);
            return;
        }

        var room = roomPool.Get(layout);

        if (room != null)
        {
            room.SetData(data);

            // 방 입장 이벤트 연결
            room.OnClickedEnter = OnRoomEnterClicked;

            roomDictionary.Add(data.roomId, room);
            ApplyFilterToRoom(room);

            if (showDebugInfo)
            {
                Debug.Log($"UIRoomList: Added room {data.roomId} - {data.roomName}");
            }
        }
    }

    public void UpdateRoom(UIElement_Room.Data data)
    {
        if (roomDictionary.TryGetValue(data.roomId, out var room))
        {
            room.SetData(data);
            ApplyFilterToRoom(room);

            if (showDebugInfo)
            {
                Debug.Log($"UIRoomList: Updated room {data.roomId} - {data.roomName}");
            }
        }
        else
        {
            Debug.LogWarning($"UIRoomList: Room {data.roomId} not found for update. Adding new room.");
            AddRoom(data);
        }
    }

    public void RemoveRoom(int roomId)
    {
        if (roomDictionary.TryGetValue(roomId, out var room))
        {
            // 이벤트 연결 해제
            room.OnClickedEnter = null;

            roomDictionary.Remove(roomId);
            roomPool.Return(room);

            if (showDebugInfo)
            {
                Debug.Log($"UIRoomList: Removed room {roomId}");
            }
        }
        else
        {
            Debug.LogWarning($"UIRoomList: Room {roomId} not found for removal.");
        }
    }

    public void ClearRooms()
    {
        // 모든 방의 이벤트 연결 해제
        foreach (var room in roomDictionary.Values)
        {
            if (room != null)
            {
                room.OnClickedEnter = null;
            }
        }

        if (roomPool != null)
        {
            roomPool.ReturnAll();
        }

        roomDictionary.Clear();

        if (showDebugInfo)
        {
            Debug.Log("UIRoomList: All rooms cleared.");
        }
    }

    #endregion

    #region Room Enter Event

    private void OnRoomEnterClicked(int roomId)
    {
        if (showDebugInfo)
        {
            Debug.Log($"UIRoomList: Room {roomId} enter button clicked.");
        }

        // 방 상태 체크
        if (roomDictionary.TryGetValue(roomId, out var room))
        {
            if (room.State == RoomState.Open)
            {
                // 외부로 이벤트 전달
                OnRoomEnterRequested?.Invoke(roomId);
            }
            else
            {
                Debug.LogWarning($"UIRoomList: Cannot enter room {roomId}. State: {room.State}");
                // TODO: UI 피드백 (팝업, 토스트 메시지 등)
            }
        }
    }

    #endregion

    #region Public Methods - Filtering

    public void UpdateFilter(RoomState flag, bool enabled)
    {
        if (enabled)
            currentFilter |= flag;
        else
            currentFilter &= ~flag;

        RefreshRoomList();

        if (showDebugInfo)
        {
            Debug.Log($"UIRoomList: Filter updated - {currentFilter}");
        }
    }

    public void SetFilter(RoomState filter)
    {
        currentFilter = filter;
        RefreshRoomList();

        if (showDebugInfo)
        {
            Debug.Log($"UIRoomList: Filter set to {currentFilter}");
        }
    }

    public void ResetFilter()
    {
        currentFilter = RoomState.All;

        if (filterDropdown != null)
        {
            filterDropdown.value = 0;
        }

        RefreshRoomList();
    }

    public void SetDropdownFilter(int index)
    {
        if (filterDropdown != null && index >= 0 && index < filterDropdown.options.Count)
        {
            filterDropdown.value = index;
        }
    }

    #endregion

    #region Private Methods

    private void RefreshRoomList()
    {
        foreach (var kvp in roomDictionary)
        {
            ApplyFilterToRoom(kvp.Value);
        }
    }

    private void ApplyFilterToRoom(UIElement_Room room)
    {
        if (room == null) return;

        bool isIncluded = (room.State & currentFilter) != 0;
        room.gameObject.SetActive(isIncluded);
    }

    #endregion

    #region Public Getters

    public int GetActiveRoomCount()
    {
        return roomDictionary.Count(kvp => kvp.Value.gameObject.activeSelf);
    }

    public int GetTotalRoomCount()
    {
        return roomDictionary.Count;
    }

    public UIElement_Room GetRoom(int roomId)
    {
        roomDictionary.TryGetValue(roomId, out var room);
        return room;
    }

    public IEnumerable<UIElement_Room> GetAllActiveRooms()
    {
        return roomDictionary.Values.Where(room => room.gameObject.activeSelf);
    }

    public RoomState GetCurrentFilter()
    {
        return currentFilter;
    }

    #endregion

    #region Debug Methods

    [ContextMenu("Debug Pool Status")]
    private void DebugPoolStatus()
    {
        if (roomPool != null)
        {
            Debug.Log($"=== UIRoomList Pool Status ===\n" +
                     $"Active Objects: {roomPool.ActiveCount}\n" +
                     $"Pooled Objects: {roomPool.PoolCount}\n" +
                     $"Total Objects: {roomPool.TotalCount}\n" +
                     $"Rooms in Dictionary: {roomDictionary.Count}\n" +
                     $"Active Rooms: {GetActiveRoomCount()}\n" +
                     $"Current Filter: {currentFilter}");
        }
        else
        {
            Debug.LogWarning("Pool is not initialized!");
        }
    }

    [ContextMenu("Debug Room States")]
    private void DebugRoomStates()
    {
        Debug.Log("=== Room States ===");
        foreach (var kvp in roomDictionary)
        {
            var room = kvp.Value;
            Debug.Log($"Room ID: {kvp.Key}, Name: {room.RoomId}, State: {room.State}, Active: {room.gameObject.activeSelf}");
        }
    }

    [ContextMenu("Generate Test Rooms")]
    private void GenerateTestRooms()
    {
        var testData = new Data
        {
            roomDatas = new UIElement_Room.Data[]
            {
                new UIElement_Room.Data
                {
                    roomId = 1,
                    roomName = "초보자 방",
                    playerCount = 2,
                    playerMaxCount = 4,
                    mapName = "Forest",
                    roomState = RoomState.Open
                },
                new UIElement_Room.Data
                {
                    roomId = 2,
                    roomName = "고수방",
                    playerCount = 4,
                    playerMaxCount = 4,
                    mapName = "Desert",
                    roomState = RoomState.Full
                },
                new UIElement_Room.Data
                {
                    roomId = 3,
                    roomName = "배틀 아레나",
                    playerCount = 3,
                    playerMaxCount = 6,
                    mapName = "Arena",
                    roomState = RoomState.Ingame
                },
                new UIElement_Room.Data
                {
                    roomId = 4,
                    roomName = "점검 중",
                    playerCount = 0,
                    playerMaxCount = 8,
                    mapName = "City",
                    roomState = RoomState.Disabled
                },
                new UIElement_Room.Data
                {
                    roomId = 5,
                    roomName = "VIP 라운지",
                    playerCount = 1,
                    playerMaxCount = 4,
                    mapName = "Mansion",
                    roomState = RoomState.Open
                },
            }
        };

        SetData(testData);
    }

    #endregion
}
