using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ChatInputField : MonoBehaviour
{
    private WaitingRoomManager waitingRoomManager;
    private TMP_InputField inputField;
    private void Start()
    {
        waitingRoomManager = WaitingRoomManager.Instance;
        
        inputField = transform.GetComponentInChildren<TMP_InputField>();
    }

    // 엔터 키 입력 시 호출
    // 메세지를 WaitingRoomManger를 통해 서버로 보낸다.
    // private void SendMessage()
    // {
    //     // 인풋 필드에 남겨져있는 텍스트를 가져와서 보낸다.
    //     string text = inputField.text;
    //     waitingRoomManager.ClientSendChat(text);

    //     // 보낸 후 필드를 비운다.
    //     inputField.text = "";
    // }
}
