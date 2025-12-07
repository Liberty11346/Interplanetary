using CommonLib;
using CommonLib.TableData;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WaitingRoomPresenter
{
    private UserManager _userManager;
    private RoomManager _roomManager;


    public System.Action OnRoomLeft;
    public System.Action<WaitingRoomData> OnRoomInfoChanged;

    WaitingRoomData cashedData;
    bool IsReady = false;

    public WaitingRoomPresenter()
    {
        _roomManager = RoomManager.Instance;
        AddEvent();
    }

    public WaitingRoomData GetRoomInfo()
    {
        if (cashedData == null)
        {
            cashedData = new WaitingRoomData();

            cashedData.roomInfo = _roomManager.CachedCurrRoom.Item1;
            cashedData.playerData = _roomManager.CachedCurrRoom.Item2;
        }
        return cashedData;
    }

    private void AddEvent()
    {
        _roomManager.OnRoomLeft += HandleOnRoomLeft;
        _roomManager.OnWaittingRoomInfoChanged += HandleOnRoomInfoChanged;
        _roomManager.OnGameSetReceived += HandleOnGameSetReceived;
        _roomManager.OnUserJoinedRoom += HandleOnUserJoinedRoom;
        _roomManager.OnUserLeftRoom += HandleOnUserLeftRoom;
    }

    private async void HandleOnGameSetReceived(GameStartData data)
    {
        // GAME_SET 수신 - 맵 데이터 저장하고 씬 로드
        GamePlayManager.Instance.SetGameStartData(data);

        // GameScene 로드
        SceneManager.LoadScene("GameScene");

        // ⭐ 씬 로드 완료 대기 (다음 프레임까지)
        await System.Threading.Tasks.Task.Delay(100);

        // ⭐ TODO: 여기서는 씬만 로드, 실제 초기화 완료 후 REQUEST_GAME_CL_READY는
        // GameScene의 초기화 컴포넌트에서 전송해야 함
    }

    private void HandleOnRoomInfoChanged(RoomInfo obj, WaittingRoomUser[] users)
    {
        WaitingRoomData data = new WaitingRoomData();
        data.roomInfo = obj;
        data.playerData = users;
        cashedData = data;
        OnRoomInfoChanged?.Invoke(data);
    }

    private void HandleOnRoomLeft()
    {
        // 방 퇴장 성공 시 로비 씬으로 이동
        // SceneManager.LoadScene("Lobby");
        // 방 퇴장 프로토콜 응답이 서버로부터 오지 않아서, 그냥 LobbyButton.cs에서 바로 퇴장하게끔 해놓음.
        // 프로토콜 고쳐지면 주석 해제할 것.
    }

    /// <summary>
    /// 유저 입장 이벤트 처리 - 방 정보 새로고침 요청
    /// </summary>
    private void HandleOnUserJoinedRoom(int userId, string userName, int playerCount)
    {
        Debug.Log($"[WaitingRoom] 유저 입장: {userName} (ID: {userId}), 총 {playerCount}명");
        // 방 정보 새로고침하여 최신 유저 목록 가져오기
        _roomManager.RequestJoinedRoomInfoRefresh();
    }

    /// <summary>
    /// 유저 퇴장 이벤트 처리 - 방 정보 새로고침 요청
    /// </summary>
    private void HandleOnUserLeftRoom(int userId, string userName, int playerCount)
    {
        Debug.Log($"[WaitingRoom] 유저 퇴장: {userName} (ID: {userId}), 총 {playerCount}명");
        // 방 정보 새로고침하여 최신 유저 목록 가져오기
        _roomManager.RequestJoinedRoomInfoRefresh();
    }

    public void HandleOnReady()
    {
        IsReady = !IsReady;
        _roomManager.RequestReadyAsync(IsReady);
    }

    public void Dispose()
    {
        if (_roomManager != null)
        {
            _roomManager.OnRoomLeft -= HandleOnRoomLeft;
            _roomManager.OnWaittingRoomInfoChanged -= HandleOnRoomInfoChanged;
            _roomManager.OnGameSetReceived -= HandleOnGameSetReceived;
            _roomManager.OnUserJoinedRoom -= HandleOnUserJoinedRoom;
            _roomManager.OnUserLeftRoom -= HandleOnUserLeftRoom;
        }
    }
}


public class WaitingRoomData
{
    public RoomInfo roomInfo;
    public WaittingRoomUser[] playerData;
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
    [SerializeField]
    private TopArea _topArea;
    [SerializeField]
    private BottomArea _bottomArea;
    [SerializeField]
    private PlayerArea _playerArea;
    [SerializeField]
    Button readyBtn;

    WaitingRoomPresenter presenter;

    private void Awake()
    {
        presenter = new WaitingRoomPresenter();
        presenter.OnRoomInfoChanged += UpdateUI;
        presenter.OnRoomLeft += HandleRoomLeft;
        readyBtn.onClick.AddListener(presenter.HandleOnReady);

        UpdateUI(presenter.GetRoomInfo());
    }

    private void HandleRoomLeft()
    {
        // 방 퇴장 성공 시 로비로 이동
        SceneManager.LoadScene("Lobby");
    }

    public void UpdateUI(WaitingRoomData data)
    {
        RoomInfo room = data.roomInfo;
        ConvertedRoomInfo convertedRoomInfo = new ConvertedRoomInfo(room);

        _topArea.UpdateUI(convertedRoomInfo);
        _bottomArea.UpdateUI(convertedRoomInfo);
        _playerArea.UpdateUI(data.playerData);
    }

    private void OnDestroy()
    {
        if (presenter != null)
        {
            presenter.OnRoomInfoChanged -= UpdateUI;
            presenter.OnRoomLeft -= HandleRoomLeft;
            presenter.Dispose();
        }
    }
}
