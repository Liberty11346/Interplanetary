using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class mainScreenCtrl : MonoBehaviour
{
    [SerializeField] private Sprite[] maskList = new Sprite[13];
    [SerializeField] private GameObject titleMask;
    [SerializeField] private Button startButton, multiPlayButton, endButton;
    private soundCtrl soundManager;
    void Start()
    {
        // 게임 프레임을 60으로 고정
        Application.targetFrameRate = 60;

        // 매번 게임을 실행할 때마다 로고 이미지가 달라진다
        int randomRate = Random.Range(0, 12);
        titleMask.GetComponent<Image>().sprite = maskList[randomRate];

        soundManager = GameObject.Find("soundManager").GetComponent<soundCtrl>();

        // 버튼 클릭 이벤트 연결
        startButton = GameObject.Find("startButton")?.GetComponent<Button>();
        multiPlayButton = GameObject.Find("MultiPlayButton")?.GetComponent<Button>();
        endButton = GameObject.Find("endButton")?.GetComponent<Button>();

        if (startButton != null)
            startButton.onClick.AddListener(() =>
            {
                soundManager.PlaySound("command");
                SceneManager.LoadScene("selectGame");
            });

        if (multiPlayButton != null)
            multiPlayButton.onClick.AddListener(() =>
            {
                soundManager.PlaySound("command");
                SceneManager.LoadScene("Robby");
            });

        if (endButton != null)
            endButton.onClick.AddListener(() =>
            {
                soundManager.PlaySound("command");
                StartCoroutine(QuitGame());
            });
    }

    IEnumerator QuitGame()
    {
        yield return new WaitForSeconds(0.5f);
        Application.Quit();
    }
}
