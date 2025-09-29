using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class gameCtrl : MonoBehaviour
{
    public bool isDone, isPrinted, isPaused;
    private enemyGameCtrl enemyManager;
    private playerGameCtrl playerManager;
    private int winner;
    [SerializeField] private GameObject endScreen, pauseScreen, victorySign, defeatSign, menuButton, endBlinder, pauseMenu, pauseResume;
    private RectTransform victoryClip, defeatClip;
    private musicCtrl musicManager;
    void Start()
    {
        enemyManager = GameObject.Find("gameManager(computer)").GetComponent<enemyGameCtrl>();
        playerManager = GameObject.Find("gameManager(player)").GetComponent<playerGameCtrl>();

        victoryClip = victorySign.GetComponent<RectTransform>();
        defeatClip = defeatSign.GetComponent<RectTransform>();
        victorySign.SetActive(false);
        defeatSign.SetActive(false);
        menuButton.SetActive(false);
        endBlinder.SetActive(false);
        pauseScreen.SetActive(false);

        isDone = false;
        isPrinted = false;
        isPaused = false;
        winner = 0;

        // 뮤직 매니저를 탐색
        musicManager = GameObject.Find("musicManager").GetComponent<musicCtrl>();

        // 뮤직 매니저가 메인 화면 음악을 재생중일 경우
        if( musicManager.isPlayingGame == false )
        {
            // 현재 재생중인 음악을 멈추고
            musicManager.musicPlayer.Stop();

            // 게임 음악을 재생
            musicManager.PlayGameMusic();
            musicManager.isInGame = true;
        }
    }

    void Update()
    {
        // 일시정지 중이 아닌 경우
        if( isPaused == false )
        {
            // 상대 행성이 플레이어에게 점령될 경우 플레이어 승리
            if( enemyManager.enemyBase.captureRate == 501 )
            {
                winner = 1;
                isDone = true;
            }

            // 플레이어 행성이 상대에게 점령될 경우 상대 승리
            if( playerManager.playerBase.captureRate == -501 )
            {
                winner = 2;
                isDone = true;
            }

            // 승리 화면 연출
            if( isDone == true )
            {
                endBlinder.SetActive(true);
                if( isPrinted == false )
                {
                    isPrinted = true;
                    switch( winner )
                    {
                        case 1: StartCoroutine(DisplayVictory()); break;
                        case 2: StartCoroutine(DisplayDefeat()); break;
                    }
                }

                // 돌아가기 버튼을 누르면 메인 화면으로 이동
                if( Input.GetMouseButtonDown(0))
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

                    // 버튼을 눌러 메인 화면으로 씬 전환
                    if( hit.collider != null ) if( hit.collider.gameObject == menuButton ) SceneManager.LoadScene("selectGame");
                }
            }

            // esc 누르면 일시 정지
            if( Input.GetKeyDown(KeyCode.Escape))
            {
                Time.timeScale = 0;
                isPaused = true;
                pauseScreen.SetActive(true);
            }   
        }
        // 일시정지 중인 경우
        else
        {
            if( Input.GetMouseButtonDown(0) )
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

                // 버튼을 눌러 일시정지를 해제하거나 게임을 나간다
                if( hit.collider != null )
                {
                    if( hit.collider.gameObject == pauseResume )
                    {
                        Debug.Log("unpaused");
                        pauseScreen.SetActive(false);
                        Time.timeScale = 1;
                        isPaused = false;
                    }
                    if( hit.collider.gameObject == pauseMenu )
                    {
                        Time.timeScale = 1;
                        isPaused = false;
                        SceneManager.LoadScene("selectGame");
                    }
                }
            }

            // 일시정지 중에 esc를 한 번 더 눌러도 해제됨
            if( Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("unpaused");
                pauseScreen.SetActive(false);
                Time.timeScale = 1;
                isPaused = false;
            }
        }
    }

    IEnumerator DisplayVictory()
    {
        victorySign.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        for( float i = 50 ; i < 1500 ; i = 25 + i * 1.25f )
        {
            victoryClip.sizeDelta = new Vector2(i, 500);
            yield return new WaitForSeconds(0.025f);
        }
        yield return new WaitForSeconds(0.5f);
        menuButton.SetActive(true);
    }

    IEnumerator DisplayDefeat()
    {
        defeatSign.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        for( float i = 50 ; i < 1500 ; i = 25 + i * 1.25f )
        {
            defeatClip.sizeDelta = new Vector2(i, 500);
            yield return new WaitForSeconds(0.025f);
        }
        yield return new WaitForSeconds(0.5f);
        menuButton.SetActive(true);
    }
}
