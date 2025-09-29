using UnityEngine;
using UnityEngine.UI;

public class blindCtrl : MonoBehaviour
{
    private Image image;
    [SerializeField] private float alpha;
    void Start()
    {
        alpha = 1.2f;
        image = gameObject.GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        alpha -= 0.02f;
        image.color = new Color(0,0,0,alpha);

        if( alpha <= 0 ) Destroy(gameObject);
    }
}
