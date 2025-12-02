using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class LobbyButton : MonoBehaviour, IPointerClickHandler
{
    private void Start()
    {
        // RoomManager의 방 퇴장 이벤트 구독
        RoomManager.Instance.OnRoomLeft += HandleRoomLeft;
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnRoomLeft -= HandleRoomLeft;
        }
    }

    // 버튼 클릭 시 호출
    public void OnPointerClick(PointerEventData eventData)
    {
        // 방 퇴장 요청
        RoomManager.Instance.LeaveRoom();

        // 방 퇴장 (임시)
        SceneManager.LoadScene("Lobby");
    }

    // 방 퇴장 완료 시 호출
    private void HandleRoomLeft()
    {
        // 로비 씬으로 이동
        SceneManager.LoadScene("Lobby");
    }
}