using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class mainScreenCtrl : MonoBehaviour
{
    [SerializeField] private Sprite[] maskList = new Sprite[13];
    [SerializeField] private GameObject titleMask;

    [Header("Main Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button multiPlayButton;
    [SerializeField] private Button endButton;

    [Header("multi Play Buttons")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button backButton;

    private soundCtrl soundManager;
    void Start()
    {
        // 게임 프레임을 60으로 고정
        Application.targetFrameRate = 60;

        // 매번 게임을 실행할 때마다 로고 이미지가 달라진다
        int randomRate = Random.Range(0, 12);
        titleMask.GetComponent<Image>().sprite = maskList[randomRate];

        soundManager = soundCtrl.Instance;

        if (startButton != null)
            startButton.onClick.AddListener(() =>
            {
                soundManager.PlaySound(soundCtrl.SoundType.Command);
                SceneManager.LoadScene("selectGame");
            });

        if (multiPlayButton != null)
            multiPlayButton.onClick.AddListener(() =>
            {
                soundManager.PlaySound(soundCtrl.SoundType.Command);
                ToggleMultiplayerButtons(true);
            });

        if (endButton != null)
            endButton.onClick.AddListener(() =>
            {
                soundManager.PlaySound(soundCtrl.SoundType.Command);
                StartCoroutine(QuitGame());
            });

        if (hostButton != null)
            hostButton.onClick.AddListener(() =>
            {
                soundManager.PlaySound(soundCtrl.SoundType.Command);
                ToRobby(true);
            });

        if (joinButton != null)
            joinButton.onClick.AddListener(() =>
            {
                soundManager.PlaySound(soundCtrl.SoundType.Command);
                ToRobby(false);
            });
        if (backButton != null)
            backButton.onClick.AddListener(() =>
            {
                soundManager.PlaySound(soundCtrl.SoundType.Command);
                ToggleMultiplayerButtons(false);
            });
    }

    IEnumerator QuitGame()
    {
        yield return new WaitForSeconds(0.5f);
        Application.Quit();
    }
    /// <summary>
    /// 멀티플레이 관련 버튼(호스트, 참가, 뒤로가기)의 활성화 상태를 토글합니다.
    /// </summary>
    /// <param name="show">true이면 버튼들을 화면에 표시하고, false이면 숨깁니다.</param>
    void ToggleMultiplayerButtons(bool show)
    {
        // .enabled는 버튼의 상호작용만 비활성화할 뿐, 화면에는 계속 보입니다.
        // .gameObject.SetActive()를 사용해야 오브젝트를 화면에서 숨기거나 표시할 수 있습니다.
        startButton.gameObject.SetActive(!show);
        multiPlayButton.gameObject.SetActive(!show);
        endButton.gameObject.SetActive(!show);

        hostButton.gameObject.SetActive(show);
        joinButton.gameObject.SetActive(show);
        backButton.gameObject.SetActive(show);
    }

    /// <summary>
    /// 멀티플레이 로비 씬으로 이동합니다.
    /// </summary>
    /// <param name="isHost">true이면 방장(Host)으로, false이면 참가자(Join)로 로비에 입장합니다.</param>
    void ToRobby(bool isHost)
    {
        if (isHost)
        {
            SceneManager.LoadScene("Robby_Host");
        }
        else
        {
            SceneManager.LoadScene("Robby_Join");
        }
    }
}
