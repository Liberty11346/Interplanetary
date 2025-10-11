using System.Collections;
using UnityEngine;
using UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 
/// </summary>
public class gameCtrl : MonoBehaviour
{
    public static gameCtrl Instance { get; private set; }

    public bool isDone, isPrinted, isPaused;
    private enemyGameCtrl enemyManager;
    private playerGameCtrl playerManager;
    private int winner;

    private void Awake()
    {
        // 싱글톤 인스턴스가 이미 존재하고, 현재 인스턴스가 아니라면 이 오브젝트를 파괴합니다.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        // 인스턴스를 현재 오브젝트로 설정합니다.
        Instance = this;
    }

    void Start()
    {
        enemyManager = GameObject.Find("gameManager(computer)").GetComponent<enemyGameCtrl>();
        playerManager = GameObject.Find("gameManager(player)").GetComponent<playerGameCtrl>();

        isDone = false;
        isPrinted = false;
        isPaused = false;
        winner = 0;

        // 뮤직 매니저가 메인 화면 음악을 재생중일 경우
        if (musicCtrl.Instance.isPlayingGame == false)
        {
            // 현재 재생중인 음악을 멈추고
            musicCtrl.Instance.musicPlayer.Stop();

            // 게임 음악을 재생
            musicCtrl.Instance.PlayGameMusic();
            musicCtrl.Instance.isInGame = true;
        }
    }

    void Update()
    {
        // 일시정지 중이 아닌 경우
        if (isPaused == false)
        {
            CheckEndGameConditions();

            // esc 누르면 일시 정지
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                PauseGame(true);
            }
        }
        // 일시정지 중인 경우
        else
        {
            // 일시정지 중에 esc를 한 번 더 눌러도 해제됨
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                PauseGame(false);
            }
        }
    }

    private void PauseGame(bool pause = true)
    {
        if (pause)
        {
            if (InGameUIManager.Instance != null) InGameUIManager.Instance.ShowPauseScreen(true);
            Time.timeScale = 0;
            isPaused = true;
        }
        else
        {
            if (InGameUIManager.Instance != null) InGameUIManager.Instance.ShowPauseScreen(false);
            Time.timeScale = 1;
            isPaused = false;
        }
    }


    /// <summary>
    /// 게임의 승리/패배 조건을 확인하고 게임 종료를 처리합니다.
    /// </summary>
    private void CheckEndGameConditions()
    {
        // 이미 게임이 종료되었다면 더 이상 확인하지 않습니다.
        if (isDone) return;

        // 상대 모성을 점령하여 플레이어가 승리한 경우
        if (enemyManager.enemyBase.captureRate > 500)
        {
            EndGame(1); // 1: 플레이어 승리
        }
        // 플레이어 모성이 점령당하여 패배한 경우
        else if (playerManager.playerBase.captureRate < -500)
        {
            EndGame(2); // 2: 상대 승리
        }
    }

    public void ResetGame()
    {
        isDone = false;
        isPrinted = false;
        winner = 0;
        return;
    }
    private void OnDestroy()
    {
        // 이 인스턴스가 파괴될 때, static Instance 참조도 null로 만들어줍니다.
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// 게임 종료 연출을 시작합니다.
    /// </summary>
    /// <param name="winnerId">승자 ID (1: 플레이어, 2: 상대)</param>
    public void EndGame(int winnerId)
    {
        // 중복 호출을 방지합니다.
        if (isPrinted) return;

        isDone = true;
        isPrinted = true;
        winner = winnerId;

        InGameUIManager.Instance.TriggerEndGame(winnerId);
    }
}
