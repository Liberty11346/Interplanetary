using System.Threading.Tasks;
using UnityEngine;
using CommonLib;

/// <summary>
/// GameScene 초기화 담당 스크립트
/// 씬 로딩 완료 후 게임 데이터 로드 및 준비 완료 프로토콜 전송
/// </summary>
public class GameSceneInitializer : MonoBehaviour
{
    [Header("초기화 설정")]
    [Tooltip("게임 데이터 로드 최대 대기 시간 (초)")]
    [SerializeField] private float maxWaitTime = 10f;

    private async void Start()
    {
        await InitializeGameScene();
    }

    private async Task InitializeGameScene()
    {
        Debug.Log("[GameSceneInitializer] 게임 씬 초기화 시작");

        // 1. UserManager와 RoomManager 확인
        var userManager = UserManager.Instance;
        var roomManager = RoomManager.Instance;

        if (userManager == null || !userManager.IsLoggedIn)
        {
            Debug.LogError("[GameSceneInitializer] UserManager가 초기화되지 않았거나 로그인되지 않았습니다!");
            return;
        }

        if (roomManager == null || !roomManager.IsInRoom)
        {
            Debug.LogError("[GameSceneInitializer] RoomManager가 초기화되지 않았거나 방에 입장하지 않았습니다!");
            return;
        }

        // 2. GamePlayManager 초기화
        var gamePlayManager = GamePlayManager.Instance;
        if (!gamePlayManager.IsInitialized)
        {
            Debug.Log("[GameSceneInitializer] GamePlayManager 초기화 중...");
            var currentUser = userManager.CurrentUser;
            var sessionToken = userManager.SessionToken;

            if (!currentUser.HasValue)
            {
                Debug.LogError("[GameSceneInitializer] 현재 사용자 정보가 없습니다!");
                return;
            }

            await gamePlayManager.Initialize(currentUser.Value, sessionToken);
            Debug.Log("[GameSceneInitializer] GamePlayManager 초기화 완료");
        }

        // 3. 게임 데이터 로드 대기 (GameStarted 이벤트 구독)
        Debug.Log("[GameSceneInitializer] 게임 데이터 로드 대기 중...");
        var tcs = new TaskCompletionSource<GameStartData>();
        System.Action<GameStartData> onGameStarted = (data) =>
        {
            Debug.Log($"[GameSceneInitializer] 게임 데이터 수신: 맵 {data.MapInfo.Name}, 행성 {data.Planets.Length}개");
            tcs.TrySetResult(data);
        };

        gamePlayManager.GameStarted += onGameStarted;

        try
        {
            // 최대 대기 시간 설정
            var timeoutTask = Task.Delay((int)(maxWaitTime * 1000));
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                Debug.LogError($"[GameSceneInitializer] 게임 데이터 로드 타임아웃! ({maxWaitTime}초)");
                return;
            }

            var gameStartData = await tcs.Task;
            Debug.Log("[GameSceneInitializer] 게임 데이터 로드 완료");

            // 4. UI 및 게임 오브젝트 초기화
            // TODO: 게임 UI 초기화, 맵 렌더링 등
            // 예: FindObjectOfType<GameUIManager>()?.Initialize(gameStartData);
            // 예: FindObjectOfType<MapRenderer>()?.RenderMap(gameStartData);

            Debug.Log("[GameSceneInitializer] UI 및 게임 오브젝트 초기화 중...");
            await Task.Delay(100); // UI 초기화 대기 (필요시 실제 초기화 로직으로 대체)

            // 5. 준비 완료 프로토콜 전송
            Debug.Log("[GameSceneInitializer] 서버에 준비 완료 알림 전송 중...");
            bool success = await gamePlayManager.RequestGameClientReady();

            if (success)
            {
                Debug.Log("[GameSceneInitializer] 게임 씬 초기화 완료!");
            }
            else
            {
                Debug.LogError("[GameSceneInitializer] 준비 완료 프로토콜 전송 실패!");
            }
        }
        finally
        {
            gamePlayManager.GameStarted -= onGameStarted;
        }
    }
}
