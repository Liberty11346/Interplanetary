using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIWindow_Login : MonoBehaviour
{
    [SerializeField] TMP_InputField usernameInput;
    [SerializeField] TMP_InputField passwordInput;
    [SerializeField] Button registerBtn;
    [SerializeField] Button autoRegisterBtn;
    [SerializeField] Button loginBtn;
    [SerializeField] TextMeshProUGUI statusText;

    private LoginPresenter presenter;

    private void Awake()
    {
        presenter = new LoginPresenter();

        // 버튼 이벤트 등록
        registerBtn.onClick.AddListener(HandleRegisterClicked);
        autoRegisterBtn.onClick.AddListener(HandleAutoRegisterClicked);
        loginBtn.onClick.AddListener(HandleLoginClicked);

        // Presenter 이벤트 구독
        presenter.OnResponseAutoRegister += HandleOnResponseAutoRegister;
        presenter.OnLoginSuccess += HandleLoginSuccess;
        presenter.OnLoginFailure += HandleFailure;
        presenter.OnRegisterSuccess += HandleRegisterSuccess;
        presenter.OnRegisterFailure += HandleFailure;
        presenter.OnStatusMessage += UpdateStatus;
    }

    private void OnDestroy()
    {
        // 버튼 이벤트 해제
        registerBtn.onClick.RemoveListener(HandleRegisterClicked);
        autoRegisterBtn.onClick.RemoveListener(HandleAutoRegisterClicked);
        loginBtn.onClick.RemoveListener(HandleLoginClicked);

        // Presenter 이벤트 구독 해제
        if (presenter != null)
        {
            presenter.OnResponseAutoRegister -= HandleOnResponseAutoRegister;
            presenter.OnLoginSuccess -= HandleLoginSuccess;
            presenter.OnLoginFailure -= HandleFailure;
            presenter.OnRegisterSuccess -= HandleRegisterSuccess;
            presenter.OnRegisterFailure -= HandleFailure;
            presenter.OnStatusMessage -= UpdateStatus;
            
            presenter.Dispose();
        }
    }

    private void HandleOnResponseAutoRegister(string username, string password)
    {
        usernameInput.text = username;
        passwordInput.text = password;
        UpdateStatus("게스트 계정이 생성되었습니다. 로그인 버튼을 눌러주세요.");
    }

    private void HandleLoginSuccess(string message)
    {
        UpdateStatus(message);

        gameObject.SetActive(false);
    }

    private void HandleRegisterSuccess(string message)
    {
        UpdateStatus(message);
    }

    private void HandleFailure(string reason)
    {
        UpdateStatus($"<color=red>{reason}</color>");
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"[UIWindow_Login] {message}");
    }

    private void HandleRegisterClicked()
    {
        SetButtonsInteractable(false);
        presenter.RequestRegister(usernameInput.text, passwordInput.text);
        SetButtonsInteractable(true);
    }

    private void HandleAutoRegisterClicked()
    {
        SetButtonsInteractable(false);
        presenter.RequestAutoRegister();
        SetButtonsInteractable(true);
    }

    private void HandleLoginClicked()
    {
        SetButtonsInteractable(false);
        presenter.RequestLogin(usernameInput.text, passwordInput.text);
        SetButtonsInteractable(true);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        loginBtn.interactable = interactable;
        registerBtn.interactable = interactable;
        autoRegisterBtn.interactable = interactable;
    }
}
