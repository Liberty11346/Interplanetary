using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 시스템 메뉴 UI
/// 연결 상태, 방 정보, 나가기 버튼 관리
/// </summary>
public class UISystemMenu : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI connectionStatusText;
    [SerializeField] private Button leaveButton;

    // 나가기 버튼 클릭 이벤트
    public event Action OnLeaveRequested;

    private void Start()
    {
        if (leaveButton != null)
        {
            leaveButton.onClick.AddListener(() => OnLeaveRequested?.Invoke());
        }
    }

    /// <summary>
    /// 연결 상태 업데이트
    /// </summary>
    public void UpdateConnectionStatus(bool isConnected)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.text = isConnected ? "Connected" : "Disconnected";
            connectionStatusText.color = isConnected ? Color.green : Color.red;
        }
        
        if (leaveButton != null)
        {
            leaveButton.interactable = isConnected;
        }
    }
}
