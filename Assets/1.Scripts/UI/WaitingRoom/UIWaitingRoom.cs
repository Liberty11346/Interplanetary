using CommonLib;
using UnityEngine;

public class WaitingRoomData
{
    public RoomInfo roomInfo;
    public PlayerInfo[] playerInfo;
}

public class ConvertedRoomInfo
{
    public RoomInfo origin;
    public string mapName;
    public string roomState;
    public ConvertedRoomInfo(RoomInfo roomInfo)
    {
        origin = roomInfo;
        mapName = ConvertMapIDToString(roomInfo.MapID);
        roomState = ConvertStateToString(roomInfo.RoomState);
    }

    private string ConvertMapIDToString(int mapID)
    {
        string mapName = "";
        switch( mapID )
        {
            case 0: mapName = "TRAINNING SCHOOL"; break;
            case 1: mapName = "RUERY SPACE"; break;
            case 2: mapName = "TWISTED LIBRA SECTOR"; break;
            default: mapName = "unknown"; break;
        }
        return mapName;
    }

    private string ConvertStateToString(RoomState state)
    {
        string roomState = "";
        switch( state )
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
        return roomState;
    }
}

public class UIWaitingRoom : MonoBehaviour
{
    public static UIWaitingRoom Instance;
    [SerializeField]
    private TopArea _topArea;
    private BottomArea _bottomArea;
    private PlayerArea _playerArea;

    private void Awake()
    {
        if( Instance == null ) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        _topArea = transform.GetComponentInChildren<TopArea>();
        _bottomArea = transform.GetComponentInChildren<BottomArea>();
        _playerArea = transform.GetComponentInChildren<PlayerArea>();
    }

    public void UpdateUI(WaitingRoomData data)
    {
        RoomInfo room = data.roomInfo;
        ConvertedRoomInfo convertedRoomInfo = new ConvertedRoomInfo(room);
        
        _topArea.UpdateUI(convertedRoomInfo);
        _bottomArea.UpdateUI(convertedRoomInfo);
        _playerArea.UpdateUI(data.playerInfo);
    }
}