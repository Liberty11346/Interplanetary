using UnityEngine;

namespace CommonLib
{
    /// <summary>
    /// 싱글톤 상태 관리를 위한 레지스트리 (Domain Reload 문제 해결용)
    /// </summary>
    public static class SingletonRegistry
    {
        public static bool IsQuitting { get; set; } = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            IsQuitting = false;
        }
    }

    /// <summary>
    /// Unity MonoBehaviour 기반 싱글톤 베이스 클래스
    /// </summary>
    /// <typeparam name="T">싱글톤으로 만들 클래스 타입</typeparam>
    public abstract class SingletonBase<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        
        /// <summary>
        /// 싱글톤 인스턴스
        /// </summary>
        public static T Instance
        {
            get
            {
                if (SingletonRegistry.IsQuitting)
                {
                    // 종료 중이라도 인스턴스가 살아있다면 반환
                    if (_instance != null)
                    {
                        return _instance;
                    }

                    Debug.LogWarning($"[SingletonBase] Instance '{typeof(T)}' already destroyed on application quit. Won't create again - returning null.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<T>();

                        if (FindObjectsOfType<T>().Length > 1)
                        {
                            Debug.LogError($"[SingletonBase] Something went really wrong - there should never be more than 1 singleton! Reopening the scene might fix it.");
                            return _instance;
                        }

                        if (_instance == null)
                        {
                            GameObject singleton = new GameObject();
                            _instance = singleton.AddComponent<T>();
                            singleton.name = $"(singleton) {typeof(T)}";

                            DontDestroyOnLoad(singleton);

                            Debug.Log($"[SingletonBase] An instance of {typeof(T)} is needed in the scene, so '{singleton}' was created with DontDestroyOnLoad.");
                        }
                        else
                        {
                            Debug.Log($"[SingletonBase] Using instance already created: {_instance.gameObject.name}");
                        }

                        // 초기화 호출
                        if (_instance is SingletonBase<T> singletonBase)
                        {
                            singletonBase.OnInitialize();
                        }
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// 싱글톤 인스턴스가 존재하는지 확인
        /// </summary>
        public static bool HasInstance => _instance != null;

        /// <summary>
        /// 싱글톤 초기화 (상속 클래스에서 구현)
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// MonoBehaviour Awake
        /// </summary>
        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
                OnInitialize();
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[SingletonBase] Instance of {typeof(T)} already exists, destroying duplicate!");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 애플리케이션 종료 시 호출
        /// </summary>
        public void OnApplicationQuit()
        {
            SingletonRegistry.IsQuitting = true;
        }

        /// <summary>
        /// 인스턴스 강제 삭제 (테스트용)
        /// </summary>
        public static void DestroyInstance()
        {
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }
    }

    /// <summary>
    /// 일반 C# 클래스용 싱글톤 베이스
    /// </summary>
    /// <typeparam name="T">싱글톤으로 만들 클래스 타입</typeparam>
    public abstract class Singleton<T> where T : class, new()
    {
        private static T _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// 싱글톤 인스턴스
        /// </summary>
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
                            _instance = new T();

                            // 초기화 호출
                            if (_instance is Singleton<T> singleton)
                            {
                                singleton.OnInitialize();
                            }
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 싱글톤 인스턴스가 존재하는지 확인
        /// </summary>
        public static bool HasInstance => _instance != null;

        /// <summary>
        /// 싱글톤 초기화 (상속 클래스에서 구현)
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// 인스턴스 강제 삭제 (테스트용)
        /// </summary>
        public static void DestroyInstance()
        {
            lock (_lock)
            {
                if (_instance is System.IDisposable disposable)
                {
                    disposable.Dispose();
                }
                _instance = null;
            }
        }
    }
}
