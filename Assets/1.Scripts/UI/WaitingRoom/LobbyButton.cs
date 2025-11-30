using UnityEngine;
using UnityEngine.EventSystems;

public class LobbyButton : MonoBehaviour, IPointerClickHandler
{
    private WaitingRoomManager waitingRoomManager;

    private void Start()
    {
        waitingRoomManager = WaitingRoomManager.Instance;
    }
    
    // 버튼 클릭 시 호출
    public void OnPointerClick(PointerEventData eventData)
    {
        // 퇴장한다.
        waitingRoomManager.ClientLeftRoom();
    }
}