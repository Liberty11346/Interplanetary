using UnityEngine;
using CommonLib;
using ProtocolType = CommonLib.ProtocolType;
using System.Net.Sockets;
using System;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Threading.Tasks;

public class WaitingRoomManager : MonoBehaviour
{
    public static WaitingRoomManager Instance;
    private RoomManager roomManager;
    private NetworkStream _networkStream;
    private TcpClient _tcpClient;
    private Queue<Protocol> _protocolQueue = new Queue<Protocol>(); // 프로토콜을 처리하기 위해 저장한 큐
    private BottomArea bottomArea;
    public bool enableDebugLogs; // 디버그 모드라면 true
    private bool _isReceiving, // 서버로부터 메세지를 수신 대기하는 상태라면 true
                 _isClientReady; // 게임 할 준비가 된 상태라면 true

    private void Awake()
    {
        if( Instance == null ) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 룸 매니저 참조
        roomManager = RoomManager.Instance;

        // 방 정보가 바뀔 때 액션 구독
        roomManager.OnRoomInfoChanged += HandleRoomInfoChanged;

        _isClientReady = false; // 아직 게임 할 준비가 안된 상태
        
        // 방 정보를 표시할 BottomArea 인스턴스 참조
        bottomArea = GameObject.Find("BottomArea").GetComponent<BottomArea>();
    }

    // HandleProtocol()에서 호출됨
    // 변경된 방 정보를 받아서 클라이언트에 표시한다.
    private void HandleRoomInfoChanged()
    {   
        // 룸 매니저로부터 방 정보를 가져온다.
        RoomInfo? room = roomManager.CurrentRoom;

        // 방 정보를 BottomArea로 넘겨서 업데이트하게 한다.
        bottomArea.UpdateInfo(room);
    }

    // 클라이언트가 방을 나갈 때 호출
    public async Task ClientLeftRoom()
    {
        await roomManager.RequestLeaveRoomAsync();
    }

    // 클라이언트가 게임 할 준비가 되었을 때 호출
    public async Task<bool> ClientReady()
    {   
        // 현재 준비 상태가 false라면 true로, true라면 false로 뒤바꾼다. (기본값 false)
        _isClientReady = !_isClientReady;

        // 현재 준비 상태를 프로토콜에 담아서 보낸다.
        await roomManager.RequestReadyAsync(_isClientReady);

        // 클라이언트에서 현재 상태를 업데이트 할 수 있게 값을 리턴
        return _isClientReady;
    }

    // Destroy() 호출 또는 씬 전환 등의 이유로 오브젝트가 파괴될 때 호출
    private void OnDestroy()
    {
        roomManager.OnRoomInfoChanged -= HandleRoomInfoChanged;
    }
}
