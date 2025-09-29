using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class mainScreenCtrl : MonoBehaviour
{
    [SerializeField] private Sprite[] maskList = new Sprite[13];
    [SerializeField] private GameObject titleMask;
    private soundCtrl soundManager;
    void Start()
    {
        // 게임 프레임을 60으로 고정
        Application.targetFrameRate = 60;

        // 매번 게임을 실행할 때마다 로고 이미지가 달라진다
        int randomRate = Random.Range(0,12);
        titleMask.GetComponent<Image>().sprite = maskList[randomRate];

        soundManager = GameObject.Find("soundManager").GetComponent<soundCtrl>();
    }

    void Update()
    {
        if( Input.GetMouseButtonDown(0) )
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

            if( hit.collider != null )
            {
                if( hit.collider.gameObject.name == "startButton" )
                {
                    soundManager.PlaySound("command");
                    SceneManager.LoadScene("selectGame");
                }

                if( hit.collider.gameObject.name == "endButton" )
                {
                    soundManager.PlaySound("command");
                    StartCoroutine(QuitGame());
                }
            }
        }
    }

    IEnumerator QuitGame()
    {
        yield return new WaitForSeconds(0.5f);
        Application.Quit();
    }
}
