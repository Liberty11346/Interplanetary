using CommonLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// 클라이언트의 준비 상태를 서버로 전송하는 버튼
public class ReadyButton : MonoBehaviour, IPointerClickHandler
{
    private WaitingRoomManager waitingRoomManager;
    private TextMeshProUGUI textMeshPro;
    private void Start()
    {
        // 매니저 싱글톤 인스턴스 가져오기
        waitingRoomManager = WaitingRoomManager.Instance;
    
        // 자신이 가지고 있는 텍스트 메쉬 참조
        textMeshPro = transform.GetComponentInChildren<TextMeshProUGUI>();

        // 텍스트 초기화
        textMeshPro.text = "준비";
    }

    // 클릭 시 호출
    public void OnPointerClick(PointerEventData eventData)
    {
        // 클라이언트 준비 신호를 보낸다.
        bool isClientReady = waitingRoomManager.ClientReady();

        // 현재 상태를 업데이트하여 표시
        textMeshPro.text = isClientReady ? "준비\n해제" : "준비";
    }
}
