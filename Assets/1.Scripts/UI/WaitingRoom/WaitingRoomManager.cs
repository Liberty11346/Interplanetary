using UnityEngine;
using CommonLib;
using ProtocolType = CommonLib.ProtocolType;
using System.Net.Sockets;
using System;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class WaitingRoomManager : MonoBehaviour
{
    public static WaitingRoomManager Instance;
    private NetworkStream _networkStream;
    private TcpClient _tcpClient;
    private Queue<Protocol> _protocolQueue = new Queue<Protocol>(); // 프로토콜을 처리하기 위해 저장한 큐
    private BottomArea bottomArea;
    public bool enableDebugLogs; // 디버그 모드라면 true
    private bool _isConnected, // 서버와 연결된 상태라면 true
                 _isReceiving, // 서버로부터 메세지를 수신 대기하는 상태라면 true
                 _isClientReady, // 게임 할 준비가 된 상태라면 true
                 _isProcessing; // 프로토콜을 처리하는 상태라면 true

    private void Awake()
    {
        if( Instance == null ) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        // 서버로부터 프로토콜 수신
        ReceiveProtocol();

        // 수신 받은 프로토콜 처리
        ProcessProtocol();
    }

    // 초기화
    private void Init()
    {   
        // 기본적으로서버에 연결 안된 상태
        _isConnected = false;
        _isReceiving = false;
        
        _isClientReady = false; // 아직 게임 할 준비가 안된 상태
        
        // 방 정보를 표시할 BottomArea 인스턴스 참조
        bottomArea = GameObject.Find("bottomArea").GetComponent<BottomArea>();
    }

    // 서버로부터 프로토콜을 받는다.
    private void ReceiveProtocol()
    {
        if( !_isReceiving ) return;

        if (_networkStream != null && _networkStream.DataAvailable)
        {
            try
            {
                // 프로토콜 크기 읽기
                byte[] sizeBuffer = new byte[4];
                int bytesRead = _networkStream.Read(sizeBuffer, 0, 4);
                if (bytesRead != 4) return;

                int payloadSize = BitConverter.ToInt32(sizeBuffer, 0);

                // 전체 메시지 읽기: [4바이트 길이] + [payload]
                byte[] messageBuffer = new byte[payloadSize + 4];
                Array.Copy(sizeBuffer, 0, messageBuffer, 0, 4);

                int totalRead = 0;
                while (totalRead < payloadSize)
                {
                    bytesRead = _networkStream.Read(messageBuffer, 4 + totalRead, payloadSize - totalRead);
                    if (bytesRead == 0) break;
                    totalRead += bytesRead;
                }

                // 프로토콜 역직렬화
                Protocol protocol = Protocol.Deserialize(messageBuffer);

                // 프로토콜 처리 큐에 추가
                _protocolQueue.Enqueue(protocol);
            }
            catch (Exception ex)
            {
                LogDebug($"Receive error: {ex.Message}");
                return;
            }
        }
    }

    // 프로토콜 큐에 있는 프로토콜을 하나씩 꺼내어 HandleProtocol()로 넘긴다.
    // Update()에서 호출됨
    private void ProcessProtocol()
    {   
        // 큐에 저장된 프로토콜이 하나 이상일 때 처리
        if( _protocolQueue.Count > 0 )
        {
            Protocol protocol = _protocolQueue.Dequeue();
            HandleProtocol(protocol);
        }
    }

    // ProcessProtocol()을 통해 수신 받은 프로토콜을 처리하기 위해 분류한다.
    private void HandleProtocol(Protocol protocol)
    {
        LogDebug($"Received protocol: {protocol.Type}");

        switch( protocol.Type )
        {   
            case ProtocolType.ROOM_INFO_CHANGED:
                HandleRoomInfoChanged(protocol);
                break;
        }
    }

    // HandleProtocol()에서 호출됨
    // 변경된 방 정보를 받아서 클라이언트에 표시한다.
    private void HandleRoomInfoChanged(Protocol protocol)
    {   
        // 프로토콜로부터 방 정보를 받는다
        RoomInfo room = protocol.GetStruct<RoomInfo>("roomInfo");

        // 방 정보를 BottomArea로 넘겨서 업데이트하게 한다.
        bottomArea.UpdateInfo(room);
    }

    // 클라이언트가 방을 나갈 때 호출
    public void ClientLeftRoom()
    {
        // 퇴장 요청 프로토콜을 보낸다.
        Protocol protocolLeftRoom = new Protocol(ProtocolType.REQUEST_LEFT_ROOM);
        SendProtocol(protocolLeftRoom);

        // 방 리스트 씬으로 이동
        // 씬 이동 시 OnDestroy() 가 호출되어서 자동으로 서버와 연결이 끊긴다.        
        SceneManager.LoadScene("Lobby");
    }

    // 클라이언트가 게임 할 준비가 되었을 때 호출
    public bool ClientReady()
    {   
        // 현재 준비 상태가 false라면 true로, true라면 false로 뒤바꾼다. (기본값 false)
        _isClientReady = !_isClientReady;

        // 현재 준비 상태를 담아서 프로토콜을 보낸다.
        Protocol protocolReady = new Protocol(ProtocolType.REQUEST_READY);
        protocolReady.AddParam("isReady", _isClientReady);
        SendProtocol(protocolReady);

        // 클라이언트에서 현재 상태를 업데이트 할 수 있게 값을 리턴
        return _isClientReady;
    }
    public void ClientSendChat(string message)
    {
        
    }

    // 프로토콜을 보낸다.
    // 보내려고 하는 프로토콜이 있으면 일단 여기 매개변수로 넣고 호출
    private void SendProtocol(Protocol protocol)
    {
        if (!_isConnected || _networkStream == null) return;

        // 연결 상태 및 스트림 쓰기 가능 여부 점검
        try
        {
            if (_tcpClient == null || _tcpClient.Client == null)
            {
                return;
            }
            if (!_networkStream.CanWrite)
            {
                Debug.Log("Stream not writable. Disconnecting.");
                Disconnect();
                return;
            }
        }
        catch (Exception checkEx)
        {
            Debug.Log($"Send pre-check error: {checkEx.Message}");
            Disconnect();
            return;
        }

        // 보낸다
        try
        {
            byte[] data = protocol.Serialize(); // 받은 프로토콜을 직렬화
            _networkStream.Write(data, 0, data.Length); // 네트워크 스트림을 통해 보낸다.
            LogDebug($"Sent protocol: {protocol.Type}"); // 보낸 프로토콜을 기록
        }
        catch (Exception ex)
        {
            LogDebug($"Send error: {ex.Message}");
            // ErrorOccurred?.Invoke($"Send error: {ex.Message}");
            // 연결 오류 발생 시 안전 종료
            Disconnect();
        }
    }

    // 디버그 모드가 켜져 있을 때에만 디버그 로그를 찍는다.
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[WaitingRoomManager] {message}");
        }
    }
    
    // 서버와 연결을 끊는다.
    public void Disconnect()
    {
        _isConnected = false; // 서버와 연결 해제
        _isReceiving = false; // 서버로부터 메세지 수신 대기 해제

        //StopHeartbeat(); // 하트비트를 그만 보낸다.

        try
        {
            _networkStream?.Close(); // 네트워크 스트림 닫기
            _tcpClient?.Close(); // TcpClient 닫기
        }
        catch (Exception ex)
        {
            LogDebug($"Disconnect error: {ex.Message}");
        }

        // ConnectionChanged?.Invoke(false, "Disconnected"); // 연결 상태 변경 시 액션 호출
        LogDebug("Disconnected from server");
    }

    // Destroy() 호출 또는 씬 전환 등의 이유로 오브젝트가 파괴될 때 호출
    private void OnDestroy()
    {
        Disconnect();
    }
}
