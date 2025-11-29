using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.PackageManager;
using UnityEngine;
using static ProtocolHandler;

namespace CommonLib
{
    /// <summary>
    /// 네트워크 연결 상태 및 설정을 관리하는 클래스
    /// </summary>
    public class NetworkConfig
    {
        public bool IsTryGoToTitle { get; set; } = false;
        public bool IsTryApplicationQuit { get; set; } = false;
        public bool IsReconnecting { get; set; } = false;
        public bool IsServerAllReady { get; set; } = false;
        public bool IsConnected { get; set; } = false;
        public DateTime LastActivityTime { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 네트워크 통신 및 프로토콜 처리를 담당하는 핵심 클래스
    /// </summary>
    public class NetworkManager
    {
        // --- 네트워크 연결 관련 ---
        private TcpClient tcpClient;
        private NetworkStream stream;
        private CancellationTokenSource cts;
        
        // 전송 동기화를 위한 세마포어 (Thread-Safety)
        private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);

        // --- 상태 관리 ---
        public NetworkConfig Config { get; private set; } = new NetworkConfig();

        // --- 프로토콜 핸들러 관리 ---
        private Dictionary<int, ProtocolHandlerDelegate> protocolHandlers = new Dictionary<int, ProtocolHandlerDelegate>();
        private readonly object handlersLock = new object();

        // --- Request-Response 패턴 ---
        private int nextProtocolId = 1;
        private readonly object protocolIdLock = new object();
        private Dictionary<int, ResponseAwaiter> pendingResponses = new Dictionary<int, ResponseAwaiter>();
        private readonly object pendingResponsesLock = new object();

        // --- 타이머 관리 ---
        private Timer heartbeatTimer;
        private Timer timeoutCheckTimer;

        // --- 상수 ---
        private const int HEARTBEAT_INTERVAL_MS = 10000; // 10초
        private const int TIMEOUT_SECONDS = 30;          // 30초
        private const int TIMEOUT_CHECK_INTERVAL = 5000; // 5초
        private const int RESPONSE_TIMEOUT_MS = 30000;   // 30초

        // --- 이벤트 ---
        public event Action<bool, string> ConnectionChanged;
        public event Action<string> ErrorOccurred;

        /// <summary>
        /// 서버에 연결
        /// </summary>
        public async Task ConnectAsync(string ip, int port)
        {
            if (Config.IsConnected)
                return;

            try
            {
                Debug.Log($"[NetworkManager] 서버 연결 시도: {ip}:{port}");

                // 기존 연결 정리
                Cleanup();

                tcpClient = new TcpClient();
                cts = new CancellationTokenSource();

                await tcpClient.ConnectAsync(ip, port);

                stream = tcpClient.GetStream();
                Config.IsConnected = true;
                UpdateLastActivity();

                Debug.Log($"<color=green>[NetworkManager] 서버 연결 성공: {ip}:{port}</color>");

                StartTimers();
                
                // 수신 루프 시작 (Fire-and-forget)
                _ = Task.Run(() => ReceiveLoop(cts.Token));

                ConnectionChanged?.Invoke(true, "Connected successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkManager] 연결 오류: {e.Message}");
                Cleanup();
                ConnectionChanged?.Invoke(false, e.Message);
                throw;
            }
        }

        /// <summary>
        /// 연결 종료
        /// </summary>
        public void Disconnect()
        {
            if (!Config.IsConnected)
                return;

            Debug.Log("[NetworkManager] 연결 종료 요청됨");
            
            // 먼저 연결 상태를 false로 변경하여 추가적인 전송/수신을 막음
            Config.IsConnected = false;

            // 취소 토큰으로 수신 루프 등 중단 요청
            try
            {
                cts?.Cancel();
            }
            catch (ObjectDisposedException) { }

            Cleanup();
            ConnectionChanged?.Invoke(false, "Disconnected");
        }

        /// <summary>
        /// 프로토콜 핸들러 등록
        /// </summary>
        public void RegisterHandler(int protocolType, ProtocolHandlerDelegate handler)
        {
            lock (handlersLock)
            {
                protocolHandlers[protocolType] = handler;
            }
        }

        /// <summary>
        /// 프로토콜 핸들러 제거
        /// </summary>
        public void UnregisterHandler(int protocolType)
        {
            lock (handlersLock)
            {
                protocolHandlers.Remove(protocolType);
            }
        }

        /// <summary>
        /// 비동기 프로토콜 전송 및 응답 대기 (통합 구조)
        /// </summary>
        public async Task<NetworkResponse> SendAsync(Protocol protocol)
        {
            // 상태 검증
            ValidateState(protocol);

            // 재접속 및 연결 상태 확인
            await EnsureConnection(protocol);

            // 서버 준비 상태 대기
            await WaitForServerReady(protocol);

            return await SendRequest(protocol);
        }

        /// <summary>
        /// 단방향 프로토콜 전송 (응답 불필요) - 통합 구조
        /// </summary>
        public async Task SendOneWayAsync(Protocol protocol)
        {
            ValidateState(protocol);

            if (!Config.IsConnected || stream == null)
                throw new InvalidOperationException("Not connected to server");

            await SendRawData(protocol.Serialize());
        }

        /// <summary>
        /// 마지막 활동 시간 갱신
        /// </summary>
        public void UpdateLastActivity()
        {
            Config.LastActivityTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 상태 검증
        /// </summary>
        private void ValidateState(Protocol protocol)
        {
            if (Config.IsTryGoToTitle || Config.IsTryApplicationQuit)
                throw new NetworkException("Application is shutting down or going to title");

            if (protocol == null)
                throw new ArgumentNullException(nameof(protocol));
        }

        /// <summary>
        /// 연결 상태 확인 및 재접속
        /// </summary>
        private async Task EnsureConnection(Protocol protocol)
        {
            // 재접속 중
            if (Config.IsReconnecting)
            {
                if (!IsReconnectingSend(protocol))
                {
                    Debug.Log($"[NetworkManager] 재접속 중에 차단된 프로토콜: {protocol.Type}");
                    throw new NetworkException("Reconnecting in progress");
                }
                await WaitWhileReconnecting();
            }

            // 연결 끊어짐 - 재접속
            if (!Config.IsConnected)
            {
                if (CanReconnect())
                {
                    await Reconnect();
                }
                else
                {
                    throw new NetworkException("Connection lost and cannot reconnect");
                }
            }
        }

        /// <summary>
        /// 서버 준비 상태 대기
        /// </summary>
        private async Task WaitForServerReady(Protocol protocol)
        {
            if (!IsEnterProtocol(protocol))
            {
                if (!Config.IsServerAllReady)
                    await WaitUntilServerReady();
            }
        }

        /// <summary>
        /// 실제 요청 전송 및 응답 대기 (통합 구조)
        /// </summary>
        private async Task<NetworkResponse> SendRequest(Protocol protocol)
        {
            if (!Config.IsConnected)
                throw new NetworkException("Not connected to server");

            ResponseAwaiter awaiter = new ResponseAwaiter(new Dictionary<string, object>());

            // 서버가 응답 시 protoId에 요청 타입을 포함하여 반환
            // 클라이언트는 protocol.Type을 키로 사용
            int protocolId = protocol.Type;

            lock (pendingResponsesLock)
            {
                pendingResponses[protocolId] = awaiter;
            }

            // 로그 출력
            if (IsShowLog(protocol))
            {
                Debug.Log($"<color=yellow>[클라 => 서버] Protocol_{protocol.Type}</color>");
            }

            // 요청 전송
            try
            {
                await SendRawData(protocol.Serialize());
            }
            catch (Exception)
            {
                // 전송 실패 시 대기열에서 제거
                lock (pendingResponsesLock)
                {
                    pendingResponses.Remove(protocolId);
                }
                throw;
            }

            // 응답 대기 (타임아웃 포함)
            using (var timeoutCts = new CancellationTokenSource(RESPONSE_TIMEOUT_MS))
            {
                try
                {
                    await WaitForAwaiterCompletion(awaiter, timeoutCts.Token);
                    return awaiter.GetResult();
                }
                catch (OperationCanceledException)
                {
                    lock (pendingResponsesLock)
                    {
                        pendingResponses.Remove(protocolId);
                    }
                    throw new TimeoutException($"Protocol {protocolId} response timeout");
                }
            }
        }

        /// <summary>
        /// 원시 데이터 전송 (Thread-Safe)
        /// </summary>
        private async Task SendRawData(byte[] data)
        {
            if (!Config.IsConnected || stream == null)
                return;

            // 세마포어를 사용하여 동시 전송 방지
            await _sendSemaphore.WaitAsync();
            try
            {
                if (!Config.IsConnected || stream == null)
                    return;

                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkManager] 전송 오류: {e.Message}");
                Disconnect();
                throw;
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        /// <summary>
        /// 수신 루프
        /// </summary>
        private async Task ReceiveLoop(CancellationToken cancellationToken)
        {
            byte[] lengthBuffer = new byte[4];

            try
            {
                while (Config.IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    if (stream == null || !stream.CanRead)
                        break;

                    // 1. 메시지 길이 읽기 (4바이트)
                    int bytesRead = 0;
                    try 
                    {
                        // ReadAsync가 0을 반환하면 연결이 종료된 것
                        bytesRead = await stream.ReadAsync(lengthBuffer, 0, 4, cancellationToken);
                    }
                    catch (Exception) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    if (bytesRead == 0)
                    {
                        Debug.Log("[NetworkManager] 서버 연결이 종료되었습니다. (Read 0 bytes)");
                        break;
                    }

                    if (bytesRead < 4)
                    {
                        // 4바이트 미만으로 읽혔다면 나머지 읽기 (드문 경우)
                        int remaining = 4 - bytesRead;
                        while (remaining > 0)
                        {
                            int read = await stream.ReadAsync(lengthBuffer, 4 - remaining, remaining, cancellationToken);
                            if (read == 0) throw new EndOfStreamException("Connection closed while reading length");
                            remaining -= read;
                        }
                    }

                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

                    // 유효성 검사 (최대 10MB로 제한 등)
                    if (messageLength <= 0 || messageLength > 10 * 1024 * 1024)
                    {
                        Debug.LogError($"[NetworkManager] 잘못된 메시지 길이: {messageLength}");
                        break;
                    }

                    // 2. 전체 메시지 읽기
                    // 헤더(4바이트)를 포함한 전체 크기가 messageLength라고 가정 (Protocol.cs의 Serialize 참조)
                    // Serialize에서는 result.Length를 맨 앞에 씀. result.Length는 헤더+데이터 전체 크기.
                    // 따라서 messageLength 만큼의 버퍼를 할당하고, 앞 4바이트는 이미 읽은 lengthBuffer 내용을 복사하거나
                    // 혹은 뒤의 데이터만 읽어서 합쳐야 함.
                    // Protocol.Deserialize는 전체 데이터를 요구함.

                    byte[] messageBuffer = new byte[messageLength];
                    Array.Copy(lengthBuffer, 0, messageBuffer, 0, 4);

                    int totalRead = 4; // 이미 4바이트 읽음
                    while (totalRead < messageLength)
                    {
                        int toRead = messageLength - totalRead;
                        int read = await stream.ReadAsync(messageBuffer, totalRead, toRead, cancellationToken);
                        if (read == 0)
                            throw new EndOfStreamException("Connection closed while reading body");
                        totalRead += read;
                    }

                    // 3. 역직렬화 및 처리
                    try
                    {
                        Protocol protocol = Protocol.Deserialize(messageBuffer);
                        if (protocol != null)
                        {
                            if (IsShowLog(protocol))
                                Debug.Log($"<color=cyan>[클라 <= 서버] Protocol_{protocol.Type}</color>");
                            
                            // 비동기 처리를 기다리지 않고 계속 수신 (순서 보장이 필요하다면 await 해야 함)
                            // 여기서는 await하여 처리 순서를 보장
                            await HandleIncomingProtocol(protocol);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[NetworkManager] 프로토콜 처리 오류: {ex.Message}");
                        // 프로토콜 하나 실패해도 연결은 유지
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[NetworkManager] 수신 루프 취소됨");
            }
            catch (Exception e)
            {
                if (Config.IsConnected) // 의도된 종료가 아닐 때만 에러 로그
                {
                    Debug.LogError($"[NetworkManager] 수신 루프 치명적 오류: {e.Message}");
                    ErrorOccurred?.Invoke($"Receive error: {e.Message}");
                    Disconnect(); // 에러 발생 시 연결 종료
                }
            }
            finally
            {
                Debug.Log("[NetworkManager] 수신 루프 종료");
                Cleanup();
            }
        }

        /// <summary>
        /// 들어오는 프로토콜 처리
        /// </summary>
        private async Task HandleIncomingProtocol(Protocol protocol)
        {
            // 응답 매칭 확인
            if (protocol.Type == (int)ProtocolType.RESPONSE)
            {
                int protocolId = protocol.GetParam<int>("protoId");
                lock (pendingResponsesLock)
                {
                    if (pendingResponses.TryGetValue(protocolId, out var awaiter))
                    {
                        pendingResponses.Remove(protocolId);

                        // Protocol 객체를 직접 사용하여 NetworkResponse 생성
                        var response = new NetworkResponse(protocol, awaiter.sendParam);
                        awaiter.Complete(response, null);
                        return;
                    }
                }
            }

            // 등록된 핸들러 실행
            ProtocolHandlerDelegate handler;
            lock (handlersLock)
            {
                protocolHandlers.TryGetValue(protocol.Type, out handler);
            }

            if (handler != null)
            {
                try
                {
                    await handler(protocol);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[NetworkManager] 프로토콜 핸들러 오류: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 타이머 시작
        /// </summary>
        private void StartTimers()
        {
            StopTimers(); // 기존 타이머 제거
            heartbeatTimer = new Timer(SendHeartbeat, null, HEARTBEAT_INTERVAL_MS, HEARTBEAT_INTERVAL_MS);
            timeoutCheckTimer = new Timer(CheckTimeout, null, TIMEOUT_CHECK_INTERVAL, TIMEOUT_CHECK_INTERVAL);
        }

        private void StopTimers()
        {
            heartbeatTimer?.Dispose();
            heartbeatTimer = null;
            timeoutCheckTimer?.Dispose();
            timeoutCheckTimer = null;
        }

        /// <summary>
        /// 하트비트 전송
        /// </summary>
        private void SendHeartbeat(object state)
        {
            if (!Config.IsConnected)
                return;

            try
            {
                var protocol = new Protocol((int)ProtocolType.HEARTBEAT);
                // Fire-and-forget, but catch exceptions
                Task.Run(async () => 
                {
                    try 
                    {
                        await SendRawData(protocol.Serialize());
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[NetworkManager] 하트비트 전송 실패: {ex.Message}");
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkManager] 하트비트 생성 오류: {e.Message}");
            }
        }

        /// <summary>
        /// 타임아웃 체크
        /// </summary>
        private void CheckTimeout(object state)
        {
            if (!Config.IsConnected)
                return;

            if ((DateTime.UtcNow - Config.LastActivityTime).TotalSeconds > TIMEOUT_SECONDS)
            {
                Debug.LogWarning("[NetworkManager] 타임아웃 감지. 연결 종료.");
                Disconnect();
            }
        }

        /// <summary>
        /// 재접속
        /// </summary>
        private async Task Reconnect()
        {
            Config.IsReconnecting = true;
            try
            {
                Debug.Log("[NetworkManager] 재접속 시도...");
                Cleanup();
                await Task.Delay(1000);
                Debug.Log("[NetworkManager] 재접속 완료 (로직 미구현)");
                // 실제 재접속 로직은 ClientServerHandler 등 상위 레벨에서 ConnectAsync를 다시 호출해야 함
            }
            finally
            {
                Config.IsReconnecting = false;
            }
        }

        /// <summary>
        /// 정리 작업 (Idempotent)
        /// </summary>
        private void Cleanup()
        {
            // 대기 중인 모든 응답 취소
            lock (pendingResponsesLock)
            {
                if (pendingResponses.Count > 0)
                {
                    foreach (var awaiter in pendingResponses.Values)
                    {
                        awaiter.Complete(new NetworkException("Connection closed"));
                    }
                    pendingResponses.Clear();
                }
            }

            StopTimers();

            try { stream?.Close(); } catch { }
            stream = null;
            
            try { tcpClient?.Close(); } catch { }
            tcpClient = null;

            try { cts?.Dispose(); } catch { }
            cts = null;

            Config.IsConnected = false;
            // _sendSemaphore는 Dispose하지 않음 (재사용 가능성 또는 수명 주기 고려)
            
            Debug.Log("[NetworkManager] 리소스 정리 완료");
        }

        // --- 유틸리티 메서드들 ---

        private int GetNextProtocolId()
        {
            lock (protocolIdLock)
            {
                return nextProtocolId++;
            }
        }

        private bool CanReconnect()
        {
            return !Config.IsTryGoToTitle && !Config.IsTryApplicationQuit;
        }

        private async Task WaitWhileReconnecting()
        {
            while (Config.IsReconnecting)
            {
                await Task.Delay(100);
            }
        }

        private async Task WaitUntilServerReady()
        {
            while (!Config.IsServerAllReady)
            {
                await Task.Delay(100);
            }
        }

        private async Task WaitForAwaiterCompletion(ResponseAwaiter awaiter, CancellationToken cancellationToken)
        {
            while (!awaiter.IsCompleted && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(10, cancellationToken);
            }
        }

        private bool IsShowLog(Protocol protocol)
        {
            return protocol.Type != (int)ProtocolType.HEARTBEAT && protocol.Type != (int)ProtocolType.HEARTBEAT_ACK;
        }

        private bool IsReconnectingSend(Protocol protocol)
        {
            return protocol.Type == (int)ProtocolType.HEARTBEAT;
        }

        private bool IsEnterProtocol(Protocol protocol)
        {
            return protocol.Type == 10001 || protocol.Type == 10002;
        }

        private Dictionary<string, object> ConvertProtocolToDict(Protocol protocol)
        {
            // Protocol 객체를 Dictionary로 변환
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// 응답 대기자
    /// </summary>
    public class ResponseAwaiter
    {
        public bool IsCompleted { get; private set; }
        private NetworkResponse result;
        private Exception exception;
        public readonly Dictionary<string, object> sendParam;

        public ResponseAwaiter(Dictionary<string, object> sendParam)
        {
            this.sendParam = sendParam ?? new Dictionary<string, object>();
        }

        public NetworkResponse GetResult()
        {
            if (!IsCompleted)
            {
                Debug.LogError("방어코드: 완료되지 않은 작업");
                return null;
            }
            if (exception != null)
                throw exception;
            return result;
        }

        public void Complete(NetworkResponse result, Exception exception)
        {
            if (IsCompleted)
            {
                Debug.LogError("방어코드: 이미 완료된 작업");
                return;
            }
            IsCompleted = true;
            this.exception = exception;
            this.result = result;
        }

        public void Complete(Exception exception)
        {
            Complete(null, exception);
        }
    }

    /// <summary>
    /// 네트워크 예외
    /// </summary>
    public class NetworkException : Exception
    {
        public NetworkException() : base("Network error occurred") { }
        public NetworkException(string message) : base(message) { }
        public NetworkException(string message, Exception innerException) : base(message, innerException) { }
    }
}
