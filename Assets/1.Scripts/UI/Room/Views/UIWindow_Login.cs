using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIWindow_Login : MonoBehaviour
{
    [SerializeField]
    TMP_InputField usernameInput;
    [SerializeField]
    TMP_InputField passwordInput;
    [SerializeField]
    Button registerByn;
    [SerializeField]
    Button autoRegisterBtn;
    [SerializeField]
    Button loginBtn;
    [SerializeField]
    TextMeshProUGUI statusText;

    LoginPresenter presenter;

    private void Awake()
    {
        presenter = new LoginPresenter();
        registerByn.onClick.AddListener(HandleRegisterClicked);
        autoRegisterBtn.onClick.AddListener(HandleAutoRegisterClicked);
        loginBtn.onClick.AddListener(HandleLoginClicked);

        presenter.OnResponseAutoRegister += HandleOnResponseAutoRegister;
    }

    void HandleOnResponseAutoRegister(string username, string password)
    {
        usernameInput.text = username;
        passwordInput.text = password;
    }

    void HandleRegisterClicked()
    {
        presenter.RequestRegister(usernameInput.text, passwordInput.text);
    }

    void HandleAutoRegisterClicked()
    {
        presenter.RequestAutoRegister();
    }

    void HandleLoginClicked()
    {
        presenter.RequestLogin(usernameInput.text, passwordInput.text);
    }
}
public class LoginPresenter
{
    public System.Action<string, string> OnResponseAutoRegister;
    public void RequestLogin(string username, string password)
    {
        //TODO : 로그인 매니저한테 로그인 요청 전달
    }

    public void RequestRegister(string username, string password)
    {
        //TODO : 로그인 매니저한테 회원가입 요청 전달
    }

    public void RequestAutoRegister()
    {
        //TODO : 로그인 매니저한테 자동회원가입(게스트) 요청 전달
    }
}
