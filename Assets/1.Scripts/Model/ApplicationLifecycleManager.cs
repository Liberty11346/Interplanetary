using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

/// <summary>
/// 애플리케이션 생명주기 관리 - 종료 시 방 퇴장 처리
/// </summary>
public class ApplicationLifecycleManager : MonoBehaviour
{
    private static ApplicationLifecycleManager _instance;
    private RoomManager roomManager;
    private bool isQuitting = false;

    private void Awake()
    {
        // 싱글톤 패턴
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        roomManager = RoomManager.Instance;

        // 씬 전환 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // WaitingRoom에서 다른 씬으로 이동할 때 (GameScene 제외)
        if (roomManager.IsInRoom && scene.name != "WaitingRoom" && scene.name != "GameScene")
        {
            Debug.Log($"[ApplicationLifecycleManager] 씬 전환 감지: {scene.name}, 방 퇴장 요청");
            // 비동기로 방 퇴장 - fire and forget
            _ = roomManager.RequestLeaveRoomAsync();
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;

        // 방에 있다면 서버에 퇴장 알림
        if (roomManager != null && roomManager.IsInRoom)
        {
            Debug.Log("[ApplicationLifecycleManager] 애플리케이션 종료 - 방 퇴장 요청");

            // 동기적으로 방 퇴장 메시지 전송 (애플리케이션 종료 전에 완료되어야 함)
            var task = roomManager.RequestLeaveRoomAsync();

            // 최대 1초 대기 (종료 시간이 너무 길면 안됨)
            task.Wait(1000);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
