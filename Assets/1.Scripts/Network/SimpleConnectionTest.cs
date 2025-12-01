using System;
using System.Threading.Tasks;
using UnityEngine;
using CommonLib;

/// <summary>
/// 서버 접속 간단 테스트용 컴포넌트
/// Inspector에서 설정하고 ContextMenu로 테스트
/// </summary>
public class SimpleConnectionTest : MonoBehaviour
{
    [Header("Connection Settings")]
    [SerializeField] private string serverAddress = "127.0.0.1";
    [SerializeField] private int serverPort = 7777;

    [Header("Test Account")]
    [SerializeField] private string testUsername = "test";
    [SerializeField] private string testPassword = "1234";

    [Header("Status (Read Only)")]
    [SerializeField] private bool isConnected = false;
    [SerializeField] private string lastResult = "";

    private void Update()
    {
        // 상태 동기화
        isConnected = ClientServerHandler.Instance?.IsConnected ?? false;
    }

    [ContextMenu("1. Connect to Server")]
    public async void ConnectToServer()
    {
        Debug.Log($"[Test] 서버 연결 시도: {serverAddress}:{serverPort}");

        try
        {
            var handler = ClientServerHandler.Instance;
            if (handler == null)
            {
                lastResult = "연결 오류: ClientServerHandler.Instance is null";
                Debug.LogError($"[Test] {lastResult}");
                return;
            }

            await handler.ConnectAsync(serverAddress, serverPort);

            if (handler.IsConnected)
            {
                lastResult = "연결 성공!";
                Debug.Log($"[Test] {lastResult}");
            }
            else
            {
                lastResult = "연결 실패";
                Debug.LogError($"[Test] {lastResult}");
            }
        }
        catch (Exception e)
        {
            lastResult = $"연결 오류: {e.Message}";
            Debug.LogError($"[Test] {lastResult}");
        }
    }

    [ContextMenu("2. Disconnect")]
    public void DisconnectFromServer()
    {
        ClientServerHandler.Instance.Disconnect();
        lastResult = "연결 해제됨";
        Debug.Log($"[Test] {lastResult}");
    }

    [ContextMenu("3. Test Auto Register")]
    public async void TestAutoRegister()
    {
        if (!CheckConnection()) return;

        Debug.Log("[Test] 자동 회원가입 요청...");

        var protocol = new Protocol(ProtocolType.REQUEST_REGISTER_AUTO);
        var response = await ClientServerHandler.Instance.AsyncSend(protocol);

        if (response.isSuccess)
        {
            string username = response.GetParam<string>("username");
            string password = response.GetParam<string>("password");

            // 테스트 계정 자동 저장
            testUsername = username;
            testPassword = password;

            lastResult = $"자동 회원가입 성공! ID: {username}, PW: {password}";
            Debug.Log($"[Test] {lastResult}");
        }
        else
        {
            lastResult = $"자동 회원가입 실패: {response.resultCode}";
            Debug.LogError($"[Test] {lastResult}");
        }
    }

    [ContextMenu("4. Test Login")]
    public async void TestLogin()
    {
        if (!CheckConnection()) return;

        Debug.Log($"[Test] 로그인 요청: {testUsername}");

        var protocol = new Protocol(ProtocolType.REQUEST_LOGIN)
            .AddParam("username", testUsername)
            .AddParam("password", testPassword);

        var response = await ClientServerHandler.Instance.AsyncSend(protocol);

        if (response.isSuccess)
        {
            int userId = response.GetParam<int>("userId");
            lastResult = $"로그인 성공! UserId: {userId}";
            Debug.Log($"[Test] {lastResult}");
        }
        else
        {
            string message = response.GetParam<string>("message") ?? response.resultCode.ToString();
            lastResult = $"로그인 실패: {message}";
            Debug.LogError($"[Test] {lastResult}");
        }
    }

    [ContextMenu("5. Test Join Lobby")]
    public async void TestJoinLobby()
    {
        if (!CheckConnection()) return;

        Debug.Log("[Test] 로비 접속 요청...");

        var protocol = new Protocol(ProtocolType.REQUEST_JOIN_LOBBY)
            .AddParam("Page", 0);

        var response = await ClientServerHandler.Instance.AsyncSend(protocol);

        if (response.isSuccess)
        {
            int roomCount = response.GetParam<int>("roomCount");
            lastResult = $"로비 접속 성공! 룸 개수: {roomCount}";
            Debug.Log($"[Test] {lastResult}");
        }
        else
        {
            lastResult = $"로비 접속 실패: {response.resultCode}";
            Debug.LogError($"[Test] {lastResult}");
        }
    }

    [ContextMenu("6. Full Test (Connect → Register → Login → Lobby)")]
    public async void RunFullTest()
    {
        Debug.Log("[Test] === Full Test 시작 ===");

        // 1. 연결
        Debug.Log("[Test] Step 1: 서버 연결...");
        try
        {
            var handler = ClientServerHandler.Instance;
            if (handler == null)
            {
                Debug.LogError("[Test] ClientServerHandler.Instance is null. 테스트 중단.");
                return;
            }

            await handler.ConnectAsync(serverAddress, serverPort);
            if (!handler.IsConnected)
            {
                Debug.LogError("[Test] 연결 실패. 테스트 중단.");
                return;
            }
            Debug.Log("[Test] 연결 성공!");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Test] 연결 오류: {e.Message}");
            return;
        }

        await Task.Delay(500);

        // 2. 자동 회원가입
        Debug.Log("[Test] Step 2: 자동 회원가입...");
        var registerProtocol = new Protocol(ProtocolType.REQUEST_REGISTER_AUTO);
        var registerResponse = await ClientServerHandler.Instance.AsyncSend(registerProtocol);

        if (registerResponse.isSuccess)
        {
            testUsername = registerResponse.GetParam<string>("username");
            testPassword = registerResponse.GetParam<string>("password");
            Debug.Log($"[Test] 회원가입 성공! ID: {testUsername}");
        }
        else
        {
            Debug.LogError($"[Test] 회원가입 실패: {registerResponse.resultCode}");
            return;
        }

        await Task.Delay(500);

        // 3. 로그인
        Debug.Log("[Test] Step 3: 로그인...");
        var loginProtocol = new Protocol(ProtocolType.REQUEST_LOGIN)
            .AddParam("username", testUsername)
            .AddParam("password", testPassword);

        var loginResponse = await ClientServerHandler.Instance.AsyncSend(loginProtocol);

        if (loginResponse.isSuccess)
        {
            int userId = loginResponse.GetParam<int>("userId");
            Debug.Log($"[Test] 로그인 성공! UserId: {userId}");
        }
        else
        {
            Debug.LogError($"[Test] 로그인 실패: {loginResponse.resultCode}");
            return;
        }

        await Task.Delay(500);

        // 4. 로비 접속
        Debug.Log("[Test] Step 4: 로비 접속...");
        var lobbyProtocol = new Protocol(ProtocolType.REQUEST_JOIN_LOBBY)
            .AddParam("Page", 0);

        var lobbyResponse = await ClientServerHandler.Instance.AsyncSend(lobbyProtocol);

        if (lobbyResponse.isSuccess)
        {
            int roomCount = lobbyResponse.GetParam<int>("roomCount");
            Debug.Log($"[Test] 로비 접속 성공! 룸 개수: {roomCount}");
        }
        else
        {
            Debug.LogError($"[Test] 로비 접속 실패: {lobbyResponse.resultCode}");
            return;
        }

        Debug.Log("[Test] === Full Test 완료! ===");
        lastResult = "Full Test 성공!";
    }

    private bool CheckConnection()
    {
        var handler = ClientServerHandler.Instance;
        if (handler == null)
        {
            lastResult = "ClientServerHandler.Instance is null.";
            Debug.LogError($"[Test] {lastResult}");
            return false;
        }

        if (!handler.IsConnected)
        {
            lastResult = "서버에 연결되어 있지 않습니다.";
            Debug.LogError($"[Test] {lastResult}");
            return false;
        }
        return true;
    }
}
