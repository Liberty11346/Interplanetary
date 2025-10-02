using UnityEngine;
using UnityEngine.Assertions.Must;

public class soundCtrl : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static soundCtrl Instance { get; private set; }

    [SerializeField] private AudioClip command, select, fleet, fleetError;
    public AudioSource soundPlayer;
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
        soundPlayer = gameObject.GetComponent<AudioSource>();
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void PlaySound(string input)
    {
        soundPlayer.pitch = 1.0f;
        switch( input )
        {
            case "command": soundPlayer.clip = command; break;
            case "select": soundPlayer.clip = select; break;
            case "fleet": soundPlayer.clip = fleet; break;
            case "fleetError": soundPlayer.pitch = 2.0f; soundPlayer.clip = fleetError; break;
            case "fleetDisturb": soundPlayer.clip = command; break;
        }
        soundPlayer.Play();
    }
}
