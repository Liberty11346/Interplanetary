using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
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
        private Dictionary<int, ResponseAwaiter> pendingResponses = new Dictionary<int, ResponseAwaiter>();
        private readonly object pendingResponsesLock = new object();

        // --- 타이머 관리 ---
        private Timer heartbeatTimer;
        private Timer timeoutCheckTimer;

        // --- 상수 ---
        private const int HEARTBEAT_INTERVAL_MS = 5000;  // 5초 (10초에서 변경)
        private const int TIMEOUT_SECONDS = 60;          // 60초
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
                Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 서버 연결 시도: {ip}:{port}");

                // 기존 연결 정리
                Cleanup();

                tcpClient = new TcpClient();
                cts = new CancellationTokenSource();

                // Nagle 알고리즘 비활성화 - 즉시 전송!
                tcpClient.NoDelay = true;

                //  송신 버퍼 크기 최소화 (선택사항)
                tcpClient.SendBufferSize = 1024;
                await tcpClient.ConnectAsync(ip, port);

                stream = tcpClient.GetStream();
                Config.IsConnected = true;
                Config.IsServerAllReady = true; // ✅ 연결 즉시 서버 준비 완료로 설정
                UpdateLastActivity();

                Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] <color=green>[NetworkManager] 서버 연결 성공: {ip}:{port}</color>");

                StartTimers();

                // 수신 루프 시작 (Fire-and-forget)
                _ = Task.Run(() => ReceiveLoop(cts.Token));

                ConnectionChanged?.Invoke(true, "Connected successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 연결 오류: {e.Message}");
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

            Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 연결 종료 요청됨");

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
            Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] [EnsureConnection] 시작 - Protocol: {protocol.Type}");
            Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] [EnsureConnection] IsReconnecting: {Config.IsReconnecting}");
            Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] [EnsureConnection] IsConnected: {Config.IsConnected}");

            // 재접속 중
            if (Config.IsReconnecting)
            {
                if (!IsReconnectingSend(protocol))
                {
                    Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] 재접속 중에 차단된 프로토콜: {protocol.Type}");
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

            Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] [EnsureConnection] 완료");
        }

        /// <summary>
        /// 서버 준비 상태 대기
        /// </summary>
        private async Task WaitForServerReady(Protocol protocol)
        {
            Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] [WaitForServerReady] 시작");
            Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] [WaitForServerReady] IsEnterProtocol: {IsEnterProtocol(protocol)}");
            Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] [WaitForServerReady] IsServerAllReady: {Config.IsServerAllReady}");

            if (!IsEnterProtocol(protocol))
            {
                if (!Config.IsServerAllReady)
                {
                    Debug.LogWarning($"[{DateTime.Now:HH:mm:ss.fff}] ⚠️ 서버 준비 대기 시작!");
                    await WaitUntilServerReady();
                    Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] 서버 준비 완료");
                }
            }

            Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] [WaitForServerReady] 완료");
        }

        /// <summary>
        /// 실제 요청 전송 및 응답 대기 (TaskCompletionSource 사용)
        /// </summary>
        private async Task<NetworkResponse> SendRequest(Protocol protocol)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            ResponseAwaiter awaiter = new ResponseAwaiter(new Dictionary<string, object>());
            int protocolId = protocol.Type;

            Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] ━━━ [SendRequest 시작] ━━━");
            Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] Protocol Type: {protocolId}");
            Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] Time: 0ms");

            lock (pendingResponsesLock)
            {
                pendingResponses[protocolId] = awaiter;
                Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] 대기자 등록 완료. 현재 대기 중: [{string.Join(", ", pendingResponses.Keys)}]");
            }

            if (IsShowLog(protocol))
            {
                Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] <color=yellow>[클라 => 서버] Protocol_{protocol.Type} ({sw.ElapsedMilliseconds}ms)</color>");
            }

            // 요청 전송
            try
            {
                await SendRawData(protocol.Serialize());
                Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] 전송 완료: {sw.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                lock (pendingResponsesLock)
                {
                    pendingResponses.Remove(protocolId);
                }
                throw;
            }

            Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] 응답 대기 시작... ({sw.ElapsedMilliseconds}ms)");

            // ✅ TaskCompletionSource 직접 await - Unity 프레임 독립적!
            try
            {
                using (var timeoutCts = new CancellationTokenSource(RESPONSE_TIMEOUT_MS))
                {
                    var completedTask = await Task.WhenAny(
                        awaiter.Task,
                        Task.Delay(RESPONSE_TIMEOUT_MS, timeoutCts.Token)
                    );

                    if (completedTask == awaiter.Task)
                    {
                        timeoutCts.Cancel();
                        Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] <color=green>✅ 응답 수신 성공! ({sw.ElapsedMilliseconds}ms)</color>");
                        return await awaiter.Task;
                    }
                    else
                    {
                        Debug.LogError($"[{DateTime.Now:HH:mm:ss.fff}] <color=red>❌ 타임아웃! ({sw.ElapsedMilliseconds}ms)</color>");
                        lock (pendingResponsesLock)
                        {
                            pendingResponses.Remove(protocolId);
                        }
                        throw new TimeoutException($"Protocol {protocolId} response timeout after {RESPONSE_TIMEOUT_MS}ms");
                    }
                }
            }
            catch (Exception)
            {
                lock (pendingResponsesLock)
                {
                    pendingResponses.Remove(protocolId);
                }
                throw;
            }
        }

        /// <summary>
        /// 원시 데이터 전송 (Thread-Safe)
        /// </summary>
        private async Task SendRawData(byte[] data)
        {
            if (!Config.IsConnected || stream == null)
                return;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] [SendRawData] 전송 시작 - Size: {data.Length} bytes");

            await _sendSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!Config.IsConnected || stream == null)
                    return;

                Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] [SendRawData] 세마포어 획득: {sw.ElapsedMilliseconds}ms");

                await stream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] [SendRawData] WriteAsync 완료: {sw.ElapsedMilliseconds}ms");

                await stream.FlushAsync().ConfigureAwait(false);
                Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] [SendRawData] FlushAsync 완료: {sw.ElapsedMilliseconds}ms");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 전송 오류: {e.Message}");
                Disconnect();
                throw;
            }
            finally
            {
                _sendSemaphore.Release();
                Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] [SendRawData] 세마포어 해제: {sw.ElapsedMilliseconds}ms");
            }
        }

        /// <summary>
        /// 수신 루프
        /// </summary>
        private async Task ReceiveLoop(CancellationToken cancellationToken)
        {
            Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] [ReceiveLoop] 시작!");
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
                        bytesRead = await stream.ReadAsync(lengthBuffer, 0, 4, cancellationToken);
                    }
                    catch (Exception) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    if (bytesRead == 0)
                    {
                        Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 서버 연결이 종료되었습니다. (Read 0 bytes)");
                        break;
                    }

                    if (bytesRead < 4)
                    {
                        int remaining = 4 - bytesRead;
                        while (remaining > 0)
                        {
                            int read = await stream.ReadAsync(lengthBuffer, 4 - remaining, remaining, cancellationToken);
                            if (read == 0) throw new EndOfStreamException("Connection closed while reading length");
                            remaining -= read;
                        }
                    }

                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

                    // 유효성 검사
                    if (messageLength <= 0 || messageLength > 1024 * 1024)
                    {
                        Debug.LogError($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 잘못된 메시지 길이: {messageLength}");
                        break;
                    }

                    // 2. 전체 메시지 읽기
                    byte[] messageBuffer = new byte[messageLength];
                    Array.Copy(lengthBuffer, 0, messageBuffer, 0, 4);

                    int totalRead = 4;
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
                                Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] <color=cyan>[클라 <= 서버] Protocol_{protocol.Type}</color>");

                            await HandleIncomingProtocol(protocol);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 프로토콜 처리 오류: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 수신 루프 취소됨");
            }
            catch (Exception e)
            {
                if (Config.IsConnected)
                {
                    Debug.LogError($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 수신 루프 치명적 오류: {e.Message}");
                    ErrorOccurred?.Invoke($"Receive error: {e.Message}");
                    Disconnect();
                }
            }
            finally
            {
                Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 수신 루프 종료");
                Cleanup();
            }
        }

        /// <summary>
        /// 들어오는 프로토콜 처리
        /// </summary>
        private async Task HandleIncomingProtocol(Protocol protocol)
        {
            Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] ━━━ [HandleIncoming 시작] ━━━");
            Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] Protocol Type: {protocol.Type}");

            // 파라미터 전체 출력
            var parameters = protocol.GetParams();
            Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] Parameters: {string.Join(", ", parameters.Select(kv => $"{kv.Key}={kv.Value}"))}");

            // 응답 매칭 확인
            if (protocol.Type == (int)ProtocolType.RESPONSE)
            {
                Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] <color=cyan>Response 감지!</color>");

                // protoId 확인
                if (!protocol.HasParam("protoId"))
                {
                    Debug.LogError($"[{DateTime.Now:HH:mm:ss.fff}] <color=red>⚠️ Response에 protoId 없음!</color>");
                    Debug.LogError($"[{DateTime.Now:HH:mm:ss.fff}] 사용 가능한 키: {string.Join(", ", parameters.Keys)}");
                    return;
                }

                int protocolId = protocol.GetParam<int>("protoId");
                Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] protoId: {protocolId}");

                lock (pendingResponsesLock)
                {
                    Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] 현재 대기 중인 요청: [{string.Join(", ", pendingResponses.Keys)}]");

                    if (pendingResponses.TryGetValue(protocolId, out var awaiter))
                    {
                        Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] <color=green>✅ 매칭 성공! protocolId: {protocolId}</color>");
                        pendingResponses.Remove(protocolId);

                        var response = new NetworkResponse(protocol, awaiter.sendParam);
                        awaiter.Complete(response, null);

                        Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] <color=green>awaiter.Complete() 호출 완료</color>");
                        return;
                    }
                    else
                    {
                        Debug.LogError($"[{DateTime.Now:HH:mm:ss.fff}] <color=red>⚠️ 매칭 실패!</color>");
                        Debug.LogError($"[{DateTime.Now:HH:mm:ss.fff}] 찾는 protocolId: {protocolId}");
                        Debug.LogError($"[{DateTime.Now:HH:mm:ss.fff}] 대기 중인 요청: [{string.Join(", ", pendingResponses.Keys)}]");
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
                Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] 핸들러 실행: Protocol {protocol.Type}");
                try
                {
                    await handler(protocol);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 프로토콜 핸들러 오류: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[{DateTime.Now:HH:mm:ss.fff}] <color=yellow>⚠️ 등록된 핸들러 없음: Protocol {protocol.Type}</color>");
            }
        }

        /// <summary>
        /// 타이머 시작
        /// </summary>
        private void StartTimers()
        {
            StopTimers();
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
                Task.Run(async () =>
                {
                    try
                    {
                        await SendRawData(protocol.Serialize());
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 하트비트 전송 실패: {ex.Message}");
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 하트비트 생성 오류: {e.Message}");
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
                Debug.LogWarning($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 타임아웃 감지. 연결 종료.");
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
                Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 재접속 시도...");
                Cleanup();
                await Task.Delay(1000);
                Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 재접속 완료 (로직 미구현)");
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

            Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] [NetworkManager] 리소스 정리 완료");
        }

        // --- 유틸리티 메서드들 ---

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
            int waitCount = 0;
            while (!Config.IsServerAllReady)
            {
                await Task.Delay(100);
                waitCount++;

                if (waitCount % 10 == 0) // 1초마다
                {
                    Debug.LogWarning($"[{DateTime.Now:HH:mm:ss.fff}] 서버 준비 대기 중... ({waitCount * 100}ms)");
                }

                if (waitCount > 300) // 30초 타임아웃
                {
                    Debug.LogError($"[{DateTime.Now:HH:mm:ss.fff}] 서버 준비 대기 타임아웃!");
                    throw new TimeoutException("Server ready timeout");
                }
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
            // ✅ 로그인(10000)도 EnterProtocol로 인정
            return protocol.Type == 10000 || protocol.Type == 10001 || protocol.Type == 10002;
        }
    }

    /// <summary>
    /// 응답 대기자 (TaskCompletionSource 사용)
    /// </summary>
    public class ResponseAwaiter
    {
        private TaskCompletionSource<NetworkResponse> tcs = new TaskCompletionSource<NetworkResponse>();
        public readonly Dictionary<string, object> sendParam;

        public ResponseAwaiter(Dictionary<string, object> sendParam)
        {
            this.sendParam = sendParam ?? new Dictionary<string, object>();
        }

        public Task<NetworkResponse> Task => tcs.Task;

        public void Complete(NetworkResponse result, Exception exception)
        {
            if (exception != null)
            {
                tcs.TrySetException(exception);
            }
            else if (result != null)
            {
                tcs.TrySetResult(result);
            }
            else
            {
                tcs.TrySetException(new InvalidOperationException("Response is null"));
            }
        }

        public void Complete(Exception exception)
        {
            tcs.TrySetException(exception ?? new Exception("Unknown error"));
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
