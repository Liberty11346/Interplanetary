using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using CommonLib;

public class LoginManager : MonoBehaviour
{
    // --- 싱글톤 ---
    private static LoginManager _instance;
    private static readonly object _lock = new object();

    public static LoginManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<LoginManager>();
                        if (_instance == null)
                        {
                            GameObject go = new GameObject(typeof(LoginManager).Name);
                            _instance = go.AddComponent<LoginManager>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }
            }
            return _instance;
        }
    }

    // --- 네트워크 및 공통 이벤트 ---
    private ClientServerHandler networkClient;
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;
    public event Action<string> OnError;
    public event Action<string> OnStatusMessage;

    // --- 로그인 관련 이벤트들 ---
    public event Action<UserInfo> OnLoginSuccess;
    public event Action<string> OnLoginFailure;
    public event Action<string, string> OnAutoRegisterSuccess; // username, password
    public event Action<string> OnRegisterSuccess;
    public event Action<string> OnRegisterFailure;
    public event Action OnLogout;

    // --- 현재 상태 ---
    private UserInfo? currentUser = null;
    private bool isLoggedIn = false;
    private string sessionToken = null;

    // --- 속성들 ---
    public UserInfo? CurrentUser => currentUser;
    public bool IsLoggedIn => isLoggedIn;
    public string SessionToken => sessionToken;

    // --- 초기화 및 생명주기 ---
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        try
        {
            networkClient = ClientServerHandler.Instance;
            if (networkClient != null)
            {
                Debug.Log("[LoginManager] 네트워크 클라이언트 연결됨");
            }
            else
            {
                Debug.LogError("[LoginManager] 네트워크 클라이언트를 찾을 수 없음");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[LoginManager] 네트워크 클라이언트 초기화 실패: {e.Message}");
        }

        RegisterNetworkHandlers();
        isInitialized = true;
        EmitStatusMessage("LoginManager 초기화 완료");
    }

    private void RegisterNetworkHandlers()
    {
        RegisterHandler(ProtocolType.BRODCAST_SYSTEM, HandleSystemBroadcast);
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            Cleanup();
            _instance = null;
        }
    }

    private void Cleanup()
    {
        currentUser = null;
        isLoggedIn = false;
        sessionToken = null;
        EmitStatusMessage("LoginManager 정리 완료");
    }

    public void Reset()
    {
        currentUser = null;
        isLoggedIn = false;
        sessionToken = null;
    }

    // --- 로그인 관련 공개 메서드들 ---

    /// <summary>
    /// 로그인 요청
    /// </summary>
    public async Task<bool> RequestLoginAsync(string username, string password)
    {
        try
        {
            ValidateNetworkConnection();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                OnLoginFailure?.Invoke("아이디와 비밀번호를 입력해주세요");
                return false;
            }

            var protocol = new Protocol(ProtocolType.REQUEST_LOGIN)
                .AddParam("username", username)
                .AddParam("password", password);

            NetworkResponse response = await SafeSendAsync(protocol, "로그인");

            if (response.isSuccess)
            {
                sessionToken = response.GetParam<string>("sessionToken");

                // CommonLib.UserInfo 사용
                currentUser = new UserInfo
                {
                    UserId = response.GetParam<int>("userId"),
                    UserName = response.GetParam<string>("username") ?? username
                };

                isLoggedIn = true;
                EmitStatusMessage($"로그인 성공: {currentUser.Value.UserName}");
                OnLoginSuccess?.Invoke(currentUser.Value);
                return true;
            }
            else
            {
                string failureReason = response.GetParam<string>("message");
                if (string.IsNullOrEmpty(failureReason))
                    failureReason = response.resultCode.ToString();

                OnLoginFailure?.Invoke(failureReason);
                return false;
            }
        }
        catch (Exception e)
        {
            EmitError($"로그인 오류: {e.Message}");
            OnLoginFailure?.Invoke($"로그인 오류: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 회원가입 요청
    /// </summary>
    public async Task<bool> RequestRegisterAsync(string username, string password)
    {
        try
        {
            ValidateNetworkConnection();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                OnRegisterFailure?.Invoke("아이디와 비밀번호를 입력해주세요");
                return false;
            }

            if (username.Length < 3 || username.Length > 20)
            {
                OnRegisterFailure?.Invoke("아이디는 3~20자 사이여야 합니다");
                return false;
            }

            if (password.Length < 4)
            {
                OnRegisterFailure?.Invoke("비밀번호는 4자 이상이어야 합니다");
                return false;
            }

            var protocol = new Protocol(ProtocolType.REQUEST_REGISTER)
                .AddParam("username", username)
                .AddParam("password", password);

            NetworkResponse response = await SafeSendAsync(protocol, "회원가입");

            if (response.isSuccess)
            {
                string registeredUsername = response.GetParam<string>("username") ?? username;
                EmitStatusMessage($"회원가입 성공: {registeredUsername}");
                OnRegisterSuccess?.Invoke(registeredUsername);
                return true;
            }
            else
            {
                string failureReason = response.GetParam<string>("message");
                if (string.IsNullOrEmpty(failureReason))
                    failureReason = response.resultCode.ToString();

                OnRegisterFailure?.Invoke(failureReason);
                return false;
            }
        }
        catch (Exception e)
        {
            EmitError($"회원가입 오류: {e.Message}");
            OnRegisterFailure?.Invoke($"회원가입 오류: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 자동 회원가입(게스트) 요청
    /// </summary>
    public async Task<bool> RequestAutoRegisterAsync()
    {
        try
        {
            ValidateNetworkConnection();

            var protocol = new Protocol(ProtocolType.REQUEST_REGISTER_AUTO);

            NetworkResponse response = await SafeSendAsync(protocol, "자동 회원가입");

            if (response.isSuccess)
            {
                string generatedUsername = response.GetParam<string>("username");
                string generatedPassword = response.GetParam<string>("password");

                EmitStatusMessage($"자동 회원가입 성공: {generatedUsername}");
                OnAutoRegisterSuccess?.Invoke(generatedUsername, generatedPassword);
                return true;
            }
            else
            {
                string failureReason = response.GetParam<string>("message");
                if (string.IsNullOrEmpty(failureReason))
                    failureReason = response.resultCode.ToString();

                OnRegisterFailure?.Invoke(failureReason);
                return false;
            }
        }
        catch (Exception e)
        {
            EmitError($"자동 회원가입 오류: {e.Message}");
            OnRegisterFailure?.Invoke($"자동 회원가입 오류: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 로그아웃 요청
    /// </summary>
    public async Task<bool> RequestLogoutAsync()
    {
        try
        {
            if (!isLoggedIn)
            {
                EmitStatusMessage("현재 로그인 상태가 아님");
                return true;
            }

            ValidateNetworkConnection();

            var protocol = new Protocol(ProtocolType.REQUEST_LOGOUT);

            NetworkResponse response = await SafeSendAsync(protocol, "로그아웃");

            PerformLocalLogout();

            return response.isSuccess;
        }
        catch (Exception e)
        {
            EmitError($"로그아웃 오류: {e.Message}");
            PerformLocalLogout();
            return false;
        }
    }

    /// <summary>
    /// 로컬 로그아웃 처리
    /// </summary>
    private void PerformLocalLogout()
    {
        currentUser = null;
        isLoggedIn = false;
        sessionToken = null;
        OnLogout?.Invoke();
        EmitStatusMessage("로그아웃 완료");
    }

    // --- 네트워크 이벤트 핸들러들 ---

    private async Task HandleSystemBroadcast(Protocol protocol)
    {
        string messageType = protocol.GetParam<string>("messageType");

        switch (messageType)
        {
            case "SESSION_EXPIRED":
                string expiredReason = protocol.GetParam<string>("reason") ?? "세션이 만료되었습니다";
                EmitStatusMessage($"세션 만료: {expiredReason}");
                PerformLocalLogout();
                OnLoginFailure?.Invoke(expiredReason);
                break;

            case "FORCE_LOGOUT":
                string forceReason = protocol.GetParam<string>("reason") ?? "다른 기기에서 로그인되었습니다";
                EmitStatusMessage($"강제 로그아웃: {forceReason}");
                PerformLocalLogout();
                OnLoginFailure?.Invoke(forceReason);
                break;
        }

        await Task.CompletedTask;
    }

    // --- UI 호출용 간단 래퍼 메서드들 ---

    public async void Login(string username, string password)
    {
        await RequestLoginAsync(username, password);
    }

    public async void Register(string username, string password)
    {
        await RequestRegisterAsync(username, password);
    }

    public async void AutoRegister()
    {
        await RequestAutoRegisterAsync();
    }

    public async void Logout()
    {
        await RequestLogoutAsync();
    }

    // --- 공통 유틸리티 ---
    private void EmitStatusMessage(string message)
    {
        Debug.Log($"[LoginManager] {message}");
        OnStatusMessage?.Invoke(message);
    }

    private void EmitError(string error)
    {
        Debug.LogError($"[LoginManager] {error}");
        OnError?.Invoke(error);
    }

    private bool IsNetworkReady()
    {
        return networkClient != null && networkClient.IsConnected;
    }

    private void ValidateNetworkConnection()
    {
        if (!IsNetworkReady())
        {
            throw new InvalidOperationException("서버에 연결되지 않았습니다");
        }
    }

    private void RegisterHandler(int protocolType, ProtocolHandler.ProtocolHandlerDelegate handler)
    {
        if (networkClient != null)
        {
            networkClient.RegisterHandler(protocolType, handler);
            Debug.Log($"[LoginManager] 핸들러 등록됨: {protocolType}");
        }
        else
        {
            Debug.LogError($"[LoginManager] 네트워크 클라이언트가 없어 핸들러 등록 실패: {protocolType}");
        }
    }

    private async Task<NetworkResponse> SafeSendAsync(Protocol protocol, string operationName = "")
    {
        try
        {
            if (networkClient == null)
            {
                throw new InvalidOperationException("네트워크 클라이언트가 초기화되지 않음");
            }

            if (!string.IsNullOrEmpty(operationName))
            {
                Debug.Log($"[LoginManager] {operationName} 요청 중...");
            }

            NetworkResponse response = await networkClient.AsyncSend(protocol);

            if (response.isSuccess)
            {
                if (!string.IsNullOrEmpty(operationName))
                {
                    Debug.Log($"[LoginManager] {operationName} 성공");
                }
            }
            else
            {
                string errorMsg = $"{operationName} 실패: {response.resultCode}";
                Debug.LogError($"[LoginManager] {errorMsg}");
                OnError?.Invoke(errorMsg);
                response.ShowResultCode();
            }

            return response;
        }
        catch (Exception e)
        {
            string errorMsg = $"{operationName} 오류: {e.Message}";
            Debug.LogError($"[LoginManager] {errorMsg}");
            OnError?.Invoke(errorMsg);
            throw;
        }
    }
}
