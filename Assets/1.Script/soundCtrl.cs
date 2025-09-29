using UnityEngine;
using UnityEngine.Assertions.Must;

public class soundCtrl : MonoBehaviour
{
    [SerializeField] private AudioClip command, select, fleet, fleetError;
    public AudioSource soundPlayer;
    void Awake()
    {
        soundPlayer = gameObject.GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        // 사운드 매니저 수가 1 초과일 경우, 하나만 남기고 다 지운다.
        DontDestroyOnLoad(gameObject);
        if( GameObject.FindGameObjectsWithTag("soundManager").Length > 1 )
        {
            for( int i = 1 ; i < GameObject.FindGameObjectsWithTag("soundManager").Length ; i++ )
                Destroy(GameObject.FindGameObjectsWithTag("soundManager")[i]);
        }
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
