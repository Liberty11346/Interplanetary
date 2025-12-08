using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 채팅 UI 컴포넌트
/// 메시지 표시 및 입력 처리
/// </summary>
public class UIChat : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private ScrollRect chatScrollRect;
    [SerializeField] private TextMeshProUGUI chatContentText;

    // 채팅 전송 이벤트 (메시지 내용 전달)
    public event Action<string> OnChatSendRequested;

    private void Start()
    {
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(SendChat);
        }

        if (chatInputField != null)
        {
            // 엔터키 입력 처리
            chatInputField.onSubmit.AddListener((text) => SendChat());
        }
    }

    /// <summary>
    /// 채팅 메시지 추가
    /// </summary>
    public void AddMessage(string sender, string message, DateTime? timestamp = null)
    {
        if (chatContentText == null) return;

        var time = timestamp ?? DateTime.Now;
        string formattedMessage = $"[{time:HH:mm:ss}] <b>{sender}</b>: {message}\n";
        
        // 텍스트 추가
        chatContentText.text += formattedMessage;

        // 스크롤을 맨 아래로 (한 프레임 뒤에 실행하여 UI 업데이트 반영)
        StartCoroutine(ScrollToBottom());
    }

    /// <summary>
    /// 시스템 메시지 추가 (색상 강조 등 가능)
    /// </summary>
    public void AddSystemMessage(string message)
    {
        AddMessage("System", $"<color=yellow>{message}</color>");
    }

    /// <summary>
    /// 에러 메시지 추가
    /// </summary>
    public void AddErrorMessage(string message)
    {
        AddMessage("Error", $"<color=red>{message}</color>");
    }

    private void SendChat()
    {
        if (chatInputField == null) return;

        string message = chatInputField.text.Trim();
        if (!string.IsNullOrEmpty(message))
        {
            // 이벤트 발생
            OnChatSendRequested?.Invoke(message);
            
            // 입력창 초기화 및 포커스 유지
            chatInputField.text = "";
            chatInputField.ActivateInputField();
        }
    }

    private System.Collections.IEnumerator ScrollToBottom()
    {
        yield return null; // 한 프레임 대기
        if (chatScrollRect != null)
        {
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
