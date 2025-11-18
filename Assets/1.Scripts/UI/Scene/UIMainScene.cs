using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class UIMainScene : MonoBehaviour
{
    [SerializeField]
    Button BtnStart;
    [SerializeField]
    Button BtnMulti;
    [SerializeField]
    Button BtnEnd;

    [SerializeField]
    string SceneStart;
    [SerializeField]
    string SceneMulti;
    [SerializeField]
    string SceneEnd;

    private void Awake()
    {
        BtnStart.onClick.AddListener(OnClickedStart);
        BtnMulti.onClick.AddListener(OnClickedMulti);
        BtnEnd.onClick.AddListener(OnClickedEnd);
    }

    void OnClickedStart()
    {
        SceneManager.LoadScene(SceneStart);
    }

    void OnClickedMulti()
    {
        SceneManager.LoadScene(SceneMulti);
    }

    void OnClickedEnd()
    {
        Application.Quit();
    }
}
