using UnityEngine;

public class musicCtrl : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static musicCtrl Instance { get; private set; }

    [SerializeField] private AudioClip[] mainMusics = new AudioClip[3];
    [SerializeField] private AudioClip[] gameMusics = new AudioClip[4];
    public bool isInGame, isPlayingGame;
    public AudioSource musicPlayer;
    private int mainCount, gameCount;

    void Awake()
    {
        // --- 싱글톤 패턴 구현 ---
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        // -----------------------

        musicPlayer = gameObject.GetComponent<AudioSource>();
        isInGame = false;
        isPlayingGame = false;
    }

    void Start()
    {
        mainCount = 0;
        gameCount = 0;

        // 처음엔 메인 음악을 재생 (메인 화면에서 게임이 시작되므로)
        PlayMainMusic();
    }

    void Update()
    {
        // 음악 재생이 멈추었을 때 현재 씬에 맞추어 다음 곡을 재생
        if( musicPlayer.isPlaying == false )
        {
            if( isInGame == false ) PlayMainMusic(); // 현재 게임 씬이 아닐 경우 메인화면 음악을 재생
            else PlayGameMusic(); // 현재 게임 씬일 경우 게임화면 음악을 재생
        }
    }

    public void PlayMainMusic()
    {
        if( musicPlayer.enabled == true )
        {
            // 현재 재생중인 음악을 멈추고
            if( musicPlayer.isPlaying == true ) musicPlayer.Stop();

            // 메인화면 음악을 재생
            musicPlayer.clip = mainMusics[mainCount];
            musicPlayer.Play();

            // 현재 메인 음악을 재생중이다.
            isPlayingGame = false;
                    
            // 다음 곡 번호 지정
            mainCount = mainCount < mainMusics.Length-1 ? mainCount + 1 : 0;
        }
    }

    public void PlayGameMusic()
    {
        if( musicPlayer.enabled == true )
        {
            // 현재 재생중인 음악을 멈추고
            if( musicPlayer.isPlaying == true ) musicPlayer.Stop();

            // 게임화면 음악을 재생
            musicPlayer.clip = gameMusics[gameCount];
            musicPlayer.Play();

            // 현재 게임 음악을 재생중이다.
            isPlayingGame = true;

            // 다음 곡 번호 지정
            gameCount = gameCount < gameMusics.Length-1 ? gameCount + 1 : 0;
        }
    }
}
