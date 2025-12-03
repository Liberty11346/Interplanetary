using CommonLib;
using System;

public class LoginPresenter
{
    private UserManager loginManager;

    public event Action<string, string> OnResponseAutoRegister;
    public event Action<string> OnLoginSuccess;
    public event Action<string> OnLoginFailure;
    public event Action<string> OnRegisterSuccess;
    public event Action<string> OnRegisterFailure;
    public event Action<string> OnStatusMessage;

    public LoginPresenter()
    {
        loginManager = UserManager.Instance;

        loginManager.OnLoginSuccess += HandleLoginSuccess;
        loginManager.OnLoginFailure += HandleLoginFailure;
        loginManager.OnAutoRegisterSuccess += HandleAutoRegisterSuccess;
        loginManager.OnRegisterSuccess += HandleRegisterSuccess;
        loginManager.OnRegisterFailure += HandleRegisterFailure;
        loginManager.OnStatusMessage += HandleStatusMessage;
    }

    public void Dispose()
    {
        if (loginManager != null)
        {
            loginManager.OnLoginSuccess -= HandleLoginSuccess;
            loginManager.OnLoginFailure -= HandleLoginFailure;
            loginManager.OnAutoRegisterSuccess -= HandleAutoRegisterSuccess;
            loginManager.OnRegisterSuccess -= HandleRegisterSuccess;
            loginManager.OnRegisterFailure -= HandleRegisterFailure;
            loginManager.OnStatusMessage -= HandleStatusMessage;
        }
    }

    public void RequestLogin(string username, string password)
    {
        loginManager.Login(username, password);
    }

    public void RequestRegister(string username, string password)
    {
        loginManager.Register(username, password);
    }

    public void RequestAutoRegister()
    {
        loginManager.AutoRegister();
    }

    private void HandleLoginSuccess(UserInfo userInfo)
    {
        OnLoginSuccess?.Invoke($"환영합니다, {userInfo.UserName}님!");
    }

    private void HandleLoginFailure(string reason)
    {
        OnLoginFailure?.Invoke(reason);
    }

    private void HandleAutoRegisterSuccess(string username, string password)
    {
        OnResponseAutoRegister?.Invoke(username, password);
    }

    private void HandleRegisterSuccess(string username)
    {
        OnRegisterSuccess?.Invoke($"회원가입 완료: {username}");
    }

    private void HandleRegisterFailure(string reason)
    {
        OnRegisterFailure?.Invoke(reason);
    }

    private void HandleStatusMessage(string message)
    {
        OnStatusMessage?.Invoke(message);
    }
}
