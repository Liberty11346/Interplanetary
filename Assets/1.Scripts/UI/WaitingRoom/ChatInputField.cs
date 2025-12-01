using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using CommonLib;

public class ChatInputField : MonoBehaviour
{
    private TMP_InputField inputField;
    private ClientServerHandler networkClient;

    private void Start()
    {
        inputField = transform.GetComponentInChildren<TMP_InputField>();
        networkClient = ClientServerHandler.Instance;

        // 엔터 키 입력 시 SendMessage 호출 (TMP_InputField의 onSubmit 이벤트)
        if (inputField != null)
        {
            inputField.onSubmit.AddListener(OnSubmit);
        }
    }

    private void OnDestroy()
    {
        if (inputField != null)
        {
            inputField.onSubmit.RemoveListener(OnSubmit);
        }
    }

    // 엔터 키 입력 시 호출
    private void OnSubmit(string text)
    {
        SendMessage(text);
    }

    // 메세지를 서버로 보낸다.
    private async void SendMessage(string text)
    {
        // 빈 텍스트는 무시
        if (string.IsNullOrWhiteSpace(text))
        {
            inputField.text = "";
            return;
        }

        // 채팅 메시지 전송 (로비 채팅)
        var protocol = new Protocol(ProtocolType.CHAT_MESSAGE)
            .AddParam("message", text);

        try
        {
            await networkClient.AsyncSend(protocol);
            Debug.Log($"[ChatInputField] 채팅 전송: {text}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ChatInputField] 채팅 전송 실패: {ex.Message}");
        }

        // 보낸 후 필드를 비운다.
        inputField.text = "";

        // 포커스 다시 설정
        inputField.ActivateInputField();
    }
}
