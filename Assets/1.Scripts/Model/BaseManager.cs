using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using CommonLib;

namespace GameClient
{
    /// <summary>
    /// 모든 Manager의 베이스 클래스 - 싱글톤 패턴과 네트워크 통신 기능 제공 (간단 버전)
    /// </summary>
    public abstract class BaseManager<T> : MonoBehaviour where T : BaseManager<T>
    {
        // --- 싱글톤 패턴 ---
        private static T _instance;
        private static readonly object _lock = new object();

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            // Scene에서 기존 인스턴스 찾기
                            _instance = FindObjectOfType<T>();

                            if (_instance == null)
                            {
                                // 새 GameObject 생성하고 컴포넌트 추가
                                GameObject go = new GameObject(typeof(T).Name);
                                _instance = go.AddComponent<T>();
                                DontDestroyOnLoad(go);
                            }
                        }
                    }
                }
                return _instance;
            }
        }

        // --- 네트워크 클라이언트 ---
        protected ClientServerHandler networkClient;

        // --- 공통 이벤트들 ---
        public event Action<string> OnError;
        public event Action<string> OnStatusMessage;

        // --- 공통 속성들 ---
        protected bool isInitialized = false;
        public bool IsInitialized => isInitialized;

        // --- Unity 생명주기 ---
        protected virtual void Awake()
        {
            // 싱글톤 중복 방지
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = (T)this;
            DontDestroyOnLoad(gameObject);

            // 네트워크 클라이언트 초기화
            InitializeNetworkClient();
        }

        protected virtual void Start()
        {
            // 파생 클래스에서 추가 초기화
            Initialize();
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                Cleanup();
                _instance = null;
            }
        }

        // --- 추상 메서드들 (파생 클래스에서 구현 필수) ---

        /// <summary>
        /// 매니저별 초기화 로직 (파생 클래스에서 구현)
        /// </summary>
        protected abstract void Initialize();

        /// <summary>
        /// 네트워크 핸들러 등록 (파생 클래스에서 구현)
        /// </summary>
        protected abstract void RegisterNetworkHandlers();

        /// <summary>
        /// 정리 작업 (파생 클래스에서 구현)
        /// </summary>
        protected abstract void Cleanup();

        // --- 가상 메서드들 (파생 클래스에서 선택적 오버라이드) ---

        /// <summary>
        /// 연결 해제시 상태 초기화 (필요한 경우 오버라이드)
        /// </summary>
        public virtual void Reset()
        {
            Debug.Log($"[{GetType().Name}] 상태 초기화됨");
        }

        // --- 공통 네트워크 메서드들 ---

        /// <summary>
        /// 네트워크 클라이언트 초기화
        /// </summary>
        private void InitializeNetworkClient()
        {
            try
            {
                networkClient = ClientServerHandler.Instance;
                if (networkClient != null)
                {
                    Debug.Log($"[{GetType().Name}] 네트워크 클라이언트 연결됨");
                }
                else
                {
                    Debug.LogError($"[{GetType().Name}] 네트워크 클라이언트를 찾을 수 없음");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetType().Name}] 네트워크 클라이언트 초기화 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 안전한 프로토콜 전송 (공통 에러 처리 포함)
        /// </summary>
        protected async Task<NetworkResponse> SafeSendAsync(Protocol protocol, string operationName = "")
        {
            try
            {
                if (networkClient == null)
                {
                    throw new InvalidOperationException("네트워크 클라이언트가 초기화되지 않음");
                }

                if (!string.IsNullOrEmpty(operationName))
                {
                    Debug.Log($"[{GetType().Name}] {operationName} 요청 중...");
                }

                NetworkResponse response = await networkClient.AsyncSend(protocol);

                if (response.isSuccess)
                {
                    if (!string.IsNullOrEmpty(operationName))
                    {
                        Debug.Log($"[{GetType().Name}] {operationName} 성공");
                    }
                }
                else
                {
                    string errorMsg = $"{operationName} 실패: {response.resultCode}";
                    Debug.LogError($"[{GetType().Name}] {errorMsg}");
                    OnError?.Invoke(errorMsg);
                    response.ShowResultCode();
                }

                return response;
            }
            catch (Exception e)
            {
                string errorMsg = $"{operationName} 오류: {e.Message}";
                Debug.LogError($"[{GetType().Name}] {errorMsg}");
                OnError?.Invoke(errorMsg);
                throw;
            }
        }

        // --- 공통 유틸리티 메서드들 ---

        /// <summary>
        /// 안전한 int 값 추출
        /// </summary>
        protected int GetIntValue(Dictionary<string, object> data, string key, int defaultValue = 0)
        {
            if (data != null && data.TryGetValue(key, out object value))
            {
                if (value is int intValue) return intValue;
                if (int.TryParse(value.ToString(), out int parsedValue)) return parsedValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// 안전한 string 값 추출
        /// </summary>
        protected string GetStringValue(Dictionary<string, object> data, string key, string defaultValue = "")
        {
            if (data != null && data.TryGetValue(key, out object value))
            {
                return value?.ToString() ?? defaultValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// 안전한 bool 값 추출
        /// </summary>
        protected bool GetBoolValue(Dictionary<string, object> data, string key, bool defaultValue = false)
        {
            if (data != null && data.TryGetValue(key, out object value))
            {
                if (value is bool boolValue) return boolValue;
                if (bool.TryParse(value.ToString(), out bool parsedValue)) return parsedValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// 안전한 long 값 추출
        /// </summary>
        protected long GetLongValue(Dictionary<string, object> data, string key, long defaultValue = 0)
        {
            if (data != null && data.TryGetValue(key, out object value))
            {
                if (value is long longValue) return longValue;
                if (long.TryParse(value.ToString(), out long parsedValue)) return parsedValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// 상태 메시지 발생
        /// </summary>
        protected void EmitStatusMessage(string message)
        {
            Debug.Log($"[{GetType().Name}] {message}");
            OnStatusMessage?.Invoke(message);
        }

        /// <summary>
        /// 에러 메시지 발생
        /// </summary>
        protected void EmitError(string error)
        {
            Debug.LogError($"[{GetType().Name}] {error}");
            OnError?.Invoke(error);
        }

        // --- 네트워크 연결 상태 체크 ---

        /// <summary>
        /// 네트워크 연결 상태 확인
        /// </summary>
        protected bool IsNetworkReady()
        {
            return networkClient != null && networkClient.IsConnected;
        }

        /// <summary>
        /// 네트워크 연결 상태 검증 (연결되지 않으면 예외 발생)
        /// </summary>
        protected void ValidateNetworkConnection()
        {
            if (!IsNetworkReady())
            {
                throw new InvalidOperationException("서버에 연결되지 않았습니다");
            }
        }

        // --- 기존 방식 핸들러 등록 (람다 문제 회피) ---

        /// <summary>
        /// 기존 방식으로 핸들러 직접 등록 (람다 문제 회피)
        /// </summary>
        protected void RegisterHandler(int protocolType, ProtocolHandler.ProtocolHandlerDelegate handler)
        {
            if (networkClient != null)
            {
                networkClient.RegisterHandler(protocolType, handler);
                Debug.Log($"[{GetType().Name}] 핸들러 등록됨: {protocolType}");
            }
            else
            {
                Debug.LogError($"[{GetType().Name}] 네트워크 클라이언트가 없어 핸들러 등록 실패: {protocolType}");
            }
        }
    }
}
