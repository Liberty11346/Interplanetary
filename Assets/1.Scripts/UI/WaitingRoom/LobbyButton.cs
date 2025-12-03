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
        // 방에 참가 중인 경우에만 방 퇴장 요청
        if (RoomManager.Instance.IsInRoom)
        {
            RoomManager.Instance.LeaveRoom();
        }
        else
        {
            // 방에 참가하지 않은 경우 바로 로비로 이동
            SceneManager.LoadScene("Lobby");
        }
    }

    // 방 퇴장 완료 시 호출
    private void HandleRoomLeft()
    {
        // 로비 씬으로 이동
        SceneManager.LoadScene("Lobby");
    }
}