using UnityEngine;

public class explosionEffect : MonoBehaviour
{
    private int frame;
    void Start()
    {
        soundCtrl.Instance.PlaySound(soundCtrl.SoundType.Death);
        frame = 0;
    }

    void Update()
    {
        frame++;
        if (frame > 30) Destroy(gameObject);
    }
}
