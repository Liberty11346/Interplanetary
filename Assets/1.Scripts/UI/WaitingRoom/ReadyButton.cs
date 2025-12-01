using System.Threading.Tasks;
using CommonLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// 클라이언트의 준비 상태를 서버로 전송하는 버튼
public class ReadyButton : MonoBehaviour, IPointerClickHandler
{
    private TextMeshProUGUI textMeshPro;
    private bool isReady = false;

    private void Start()
    {
        // 자신이 가지고 있는 텍스트 메쉬 참조
        textMeshPro = transform.GetComponentInChildren<TextMeshProUGUI>();

        // 텍스트 초기화
        textMeshPro.text = "준비";
    }

    // 클릭 시 호출
    public void OnPointerClick(PointerEventData eventData)
    {
        

        // 현재 상태를 업데이트하여 표시
        textMeshPro.text = isReady ? "준비\n해제" : "준비";
    }
}
