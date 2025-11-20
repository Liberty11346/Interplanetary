using CommonLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIElement_Room : MonoBehaviour
{
    private const string TOK_PLAYER_COUNT = "PC";
    private const string TOK_PLAYER_MAX_COUNT = "PM";
    private const string TOK_MAP = "MAP";

    private const string PLAYER_COUNT_FORMAT = "플레이어: PC/PM";
    private const string MAP_FORMAT = "맵: MAP";
    public class Data
    {
        public string roomId;
        public int playerCount;
        public int playerMaxCount;
        public string roomName;
        public string mapName;
        public RoomState roomState;
    }

    [SerializeField]
    TextMeshProUGUI txt_roomName;
    [SerializeField]
    TextMeshProUGUI txt_playerCount;
    [SerializeField]
    TextMeshProUGUI txt_mapName;

    [SerializeField]
    Button btnEnterButton;
    [SerializeField]
    string roomId;
    [SerializeField]
    RoomState roomState;

    public string RoomId => roomId;
    public RoomState State => roomState;

    public System.Action<string> OnClickedEnter;
    private void Awake()
    {
        btnEnterButton.onClick.AddListener(HandleOnClick);
    }
    void HandleOnClick()
    {
        OnClickedEnter?.Invoke(roomId);
    }

    public void SetData(Data data)
    {
        txt_roomName.text = data.roomName;

        txt_playerCount.text = PLAYER_COUNT_FORMAT
            .Replace(TOK_PLAYER_COUNT, data.playerCount.ToString())
            .Replace(TOK_PLAYER_MAX_COUNT, data.playerMaxCount.ToString());

        txt_mapName.text = MAP_FORMAT
            .Replace(TOK_MAP, data.mapName);

        roomId = data.roomId;
        roomState = data.roomState;
    }
}
