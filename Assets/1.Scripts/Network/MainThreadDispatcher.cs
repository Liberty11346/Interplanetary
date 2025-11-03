using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CommonLib
{
    /// <summary>
    /// 백그라운드 스레드에서 Unity 메인 스레드로 작업을 전달하는 디스패처
    /// </summary>
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static MainThreadDispatcher _instance;
        private static readonly Queue<Action> _executionQueue = new Queue<Action>();
        private static readonly object _queueLock = new object();

        /// <summary>
        /// 싱글톤 인스턴스
        /// </summary>
        public static MainThreadDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    CreateInstance();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 메인 스레드에서 실행 중인지 확인
        /// </summary>
        public static bool IsMainThread => _instance != null && _instance.gameObject != null;

        /// <summary>
        /// 인스턴스 생성
        /// </summary>
        private static void CreateInstance()
        {
            if (_instance != null) return;

            // 씬에서 기존 인스턴스 찾기
            _instance = FindObjectOfType<MainThreadDispatcher>();

            if (_instance == null)
            {
                // 새로운 GameObject 생성하여 인스턴스 추가
                GameObject go = new GameObject("MainThreadDispatcher");
                _instance = go.AddComponent<MainThreadDispatcher>();

                // 씬 전환 시에도 유지
                DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            // 중복 인스턴스 방지
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Update()
        {
            // 메인 스레드에서 큐에 있는 작업들을 실행
            lock (_queueLock)
            {
                while (_executionQueue.Count > 0)
                {
                    try
                    {
                        _executionQueue.Dequeue().Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[MainThreadDispatcher] 작업 실행 중 오류: {e.Message}\n{e.StackTrace}");
                    }
                }
            }
        }

        /// <summary>
        /// 작업을 메인 스레드에서 실행하도록 큐에 추가
        /// </summary>
        /// <param name="action">실행할 작업</param>
        public static void Enqueue(Action action)
        {
            if (action == null)
            {
                Debug.LogWarning("[MainThreadDispatcher] Null action을 큐에 추가하려고 시도했습니다.");
                return;
            }

            // 이미 메인 스레드에서 실행 중이면 즉시 실행
            if (IsMainThread && Application.isPlaying)
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MainThreadDispatcher] 즉시 실행 중 오류: {e.Message}\n{e.StackTrace}");
                }
                return;
            }

            // 인스턴스 확인
            if (Instance == null)
            {
                Debug.LogError("[MainThreadDispatcher] 인스턴스를 생성할 수 없습니다. Unity가 종료 중일 수 있습니다.");
                return;
            }

            // 큐에 추가
            lock (_queueLock)
            {
                _executionQueue.Enqueue(action);
            }
        }

        /// <summary>
        /// 비동기 작업을 메인 스레드에서 실행하도록 큐에 추가
        /// </summary>
        /// <param name="asyncAction">실행할 비동기 작업</param>
        public static void Enqueue(Func<System.Threading.Tasks.Task> asyncAction)
        {
            if (asyncAction == null)
            {
                Debug.LogWarning("[MainThreadDispatcher] Null async action을 큐에 추가하려고 시도했습니다.");
                return;
            }

            Enqueue(() =>
            {
                try
                {
                    var task = asyncAction.Invoke();
                    // Fire and forget - 에러는 asyncAction 내부에서 처리되어야 함
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MainThreadDispatcher] 비동기 작업 시작 중 오류: {e.Message}\n{e.StackTrace}");
                }
            });
        }

        /// <summary>
        /// 지연 실행 (코루틴 사용)
        /// </summary>
        /// <param name="action">실행할 작업</param>
        /// <param name="delay">지연 시간 (초)</param>
        public static void EnqueueDelayed(Action action, float delay)
        {
            if (action == null)
            {
                Debug.LogWarning("[MainThreadDispatcher] Null action을 지연 실행하려고 시도했습니다.");
                return;
            }

            if (Instance == null)
            {
                Debug.LogError("[MainThreadDispatcher] 인스턴스를 생성할 수 없습니다.");
                return;
            }

            Instance.StartCoroutine(DelayedExecution(action, delay));
        }

        /// <summary>
        /// 다음 프레임에 실행
        /// </summary>
        /// <param name="action">실행할 작업</param>
        public static void EnqueueNextFrame(Action action)
        {
            if (action == null)
            {
                Debug.LogWarning("[MainThreadDispatcher] Null action을 다음 프레임에 실행하려고 시도했습니다.");
                return;
            }

            if (Instance == null)
            {
                Debug.LogError("[MainThreadDispatcher] 인스턴스를 생성할 수 없습니다.");
                return;
            }

            Instance.StartCoroutine(NextFrameExecution(action));
        }

        /// <summary>
        /// 큐 크기 확인 (디버깅용)
        /// </summary>
        public static int QueueSize
        {
            get
            {
                lock (_queueLock)
                {
                    return _executionQueue.Count;
                }
            }
        }

        /// <summary>
        /// 큐 비우기 (긴급 상황용)
        /// </summary>
        public static void ClearQueue()
        {
            lock (_queueLock)
            {
                int count = _executionQueue.Count;
                _executionQueue.Clear();
                Debug.Log($"[MainThreadDispatcher] 큐에서 {count}개의 작업을 제거했습니다.");
            }
        }

        /// <summary>
        /// 지연 실행 코루틴
        /// </summary>
        private static IEnumerator DelayedExecution(Action action, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[MainThreadDispatcher] 지연 실행 중 오류: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 다음 프레임 실행 코루틴
        /// </summary>
        private static IEnumerator NextFrameExecution(Action action)
        {
            yield return null; // 다음 프레임까지 대기
            
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[MainThreadDispatcher] 다음 프레임 실행 중 오류: {e.Message}\n{e.StackTrace}");
            }
        }

        private void OnDestroy()
        {
            // 인스턴스가 파괴될 때 큐 정리
            if (_instance == this)
            {
                ClearQueue();
                _instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            // 애플리케이션 종료 시 큐 정리
            ClearQueue();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터용 디버깅 정보
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void OnGUI()
        {
            if (!Application.isPlaying) return;

            // 화면 상단에 큐 상태 표시
            GUI.Label(new Rect(10, 10, 200, 20), $"MainThread Queue: {QueueSize}");
            
            if (QueueSize > 100)
            {
                GUI.color = Color.red;
                GUI.Label(new Rect(10, 30, 300, 20), "Warning: Queue size is too large!");
                GUI.color = Color.white;
            }
        }
#endif
    }

    /// <summary>
    /// MainThreadDispatcher 확장 메서드들
    /// </summary>
    public static class MainThreadDispatcherExtensions
    {
        /// <summary>
        /// Task를 메인 스레드에서 실행
        /// </summary>
        public static void RunOnMainThread(this System.Threading.Tasks.Task task, Action onComplete = null)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                try
                {
                    task.Wait();
                    onComplete?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MainThreadDispatcher] Task 실행 중 오류: {e.Message}");
                }
            });
        }

        /// <summary>
        /// Task<T>를 메인 스레드에서 실행하고 결과 처리
        /// </summary>
        public static void RunOnMainThread<T>(this System.Threading.Tasks.Task<T> task, Action<T> onComplete)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                try
                {
                    T result = task.Result;
                    onComplete?.Invoke(result);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MainThreadDispatcher] Task<T> 실행 중 오류: {e.Message}");
                }
            });
        }
    }
}