using UnityEngine;

public class explosionEffect : MonoBehaviour
{
    private AudioSource sound;
    [SerializeField] private AudioClip death;
    private int frame;
    void Start()
    {
        sound = transform.GetComponent<AudioSource>();
        sound.clip = death;
        sound.Play();

        frame = 0;
    }

    void Update()
    {
        frame++;
        if( frame > 30 ) Destroy(gameObject);
    }
}
