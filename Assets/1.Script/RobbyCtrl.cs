using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // TextMeshPro 사용을 위해 추가

public class RobbyCtrl : MonoBehaviour
{
    private soundCtrl soundManager;

    [Header("UI Elements")]
    [SerializeField] private TMP_InputField roomNameInputField; // 방 이름 입력 필드
    [SerializeField] private Button createRoomButton; // 방 만들기 버튼
    [SerializeField] private Button joinRoomButton;   // 방 참가 버튼
    [SerializeField] private Button startGameButton;  // 게임 시작 버튼
    [SerializeField] private Button leaveButton;      // 나가기 버튼
    [SerializeField] private TextMeshProUGUI statusText;      // 현재 상태 표시 텍스트

    void Start()
    {
        soundManager = GameObject.Find("soundManager").GetComponent<soundCtrl>();

        // 초기에는 게임 시작 버튼을 비활성화 (방장이 아닐 경우)
        if (startGameButton != null)
        {
            startGameButton.interactable = false;
        }

        // 상태 텍스트 초기화
        if (statusText != null)
        {
            statusText.text = "온라인에 연결되었습니다. 방을 만들거나 참가하세요.";
        }
    }

    // 방 만들기 버튼 클릭 시 호출될 함수
    public void OnCreateRoomButtonClick()
    {
        soundManager.PlaySound("command");
        string roomName = roomNameInputField.text;

        if (string.IsNullOrEmpty(roomName))
        {
            statusText.text = "방 이름을 입력해주세요.";
            return;
        }

        statusText.text = $"'{roomName}' 방을 생성하는 중...";
        // TODO: 여기에 네트워크 라이브러리를 사용한 방 생성 로직을 추가하세요.
        // 예: PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    // 방 참가 버튼 클릭 시 호출될 함수
    public void OnJoinRoomButtonClick()
    {
        soundManager.PlaySound("command");
        string roomName = roomNameInputField.text;

        statusText.text = $"'{roomName}' 방에 참가하는 중...";
        // TODO: 여기에 네트워크 라이브러리를 사용한 방 참가 로직을 추가하세요.
        // 예: PhotonNetwork.JoinRoom(roomName);
    }

    // 게임 시작 버튼 클릭 시 호출될 함수 (주로 방장만 호출 가능)
    public void OnStartGameButtonClick()
    {
        soundManager.PlaySound("command");
        statusText.text = "게임을 시작합니다!";
        // TODO: 여기에 모든 플레이어를 게임 씬으로 이동시키는 로직을 추가하세요.
        // 예: PhotonNetwork.LoadLevel("GameSceneName");
    }

    // 메인 화면으로 돌아가는 버튼
    public void ToMainScreen()
    {
        soundManager.PlaySound("command");
        // TODO: 만약 네트워크에 연결되어 있다면 연결을 끊는 로직을 추가해야 합니다.
        // 예: PhotonNetwork.Disconnect();
        SceneManager.LoadScene("mainScreen");
    }
}
