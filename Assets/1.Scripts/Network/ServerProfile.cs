using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// 서버 프로필 데이터 구조
/// </summary>
[Serializable]
public class ServerProfileData
{
    public string serverAddress = "127.0.0.1";
    public int serverPort = 9000;
    public int connectionTimeout = 5000;
    public int reconnectDelay = 3000;
    public bool autoReconnect = false;
}

/// <summary>
/// 서버 프로필 관리 MonoBehaviour
/// </summary>
public class ServerProfile : MonoBehaviour
{
    private static ServerProfile _instance;

    /// <summary>
    /// 싱글톤 인스턴스
    /// </summary>
    public static ServerProfile Instance
    {
        get
        {
            if (_instance == null)
            {
                // 씬에서 찾기
                _instance = FindObjectOfType<ServerProfile>();

                // 없으면 새로 생성
                if (_instance == null)
                {
                    GameObject go = new GameObject("ServerProfile");
                    _instance = go.AddComponent<ServerProfile>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [Header("Server Settings")]
    [Tooltip("서버 IP 주소 (예: 127.0.0.1, 192.168.0.100)")]
    public string serverAddress = "127.0.0.1";

    [Tooltip("서버 포트 번호")]
    public int serverPort = 9000;

    [Header("Connection Settings")]
    [Tooltip("연결 타임아웃 (밀리초)")]
    public int connectionTimeout = 5000;

    [Tooltip("재연결 대기 시간 (밀리초)")]
    public int reconnectDelay = 3000;

    [Tooltip("자동 재연결 활성화")]
    public bool autoReconnect = false;

    [Header("Configuration File")]
    [Tooltip("외부 설정 파일 사용 (StreamingAssets/server_profile.json)")]
    public bool useConfigFile = true;

    private string ConfigFilePath => Path.Combine(Application.streamingAssetsPath, "server_profile.json");

    private void Awake()
    {
        // 싱글톤 패턴
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 설정 로드
        LoadConfig();
    }

    /// <summary>
    /// 설정 파일에서 로드 (우선순위: 파일 > Inspector)
    /// </summary>
    private void LoadConfig()
    {
        if (useConfigFile && File.Exists(ConfigFilePath))
        {
            try
            {
                string json = File.ReadAllText(ConfigFilePath);
                ServerProfileData config = JsonConvert.DeserializeObject<ServerProfileData>(json);

                if (config != null)
                {
                    serverAddress = config.serverAddress;
                    serverPort = config.serverPort;
                    connectionTimeout = config.connectionTimeout;
                    reconnectDelay = config.reconnectDelay;
                    autoReconnect = config.autoReconnect;

                    Debug.Log($"[ServerProfile] 설정 파일 로드 완료: {serverAddress}:{serverPort}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ServerProfile] 설정 파일 로드 실패: {ex.Message}. Inspector 설정 사용.");
            }
        }
        else
        {
            Debug.Log($"[ServerProfile] Inspector 설정 사용: {serverAddress}:{serverPort}");
        }
    }

    /// <summary>
    /// 현재 설정을 파일로 저장
    /// </summary>
    public void SaveConfig()
    {
        try
        {
            // StreamingAssets 폴더가 없으면 생성
            string directory = Path.GetDirectoryName(ConfigFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            ServerProfileData config = new ServerProfileData
            {
                serverAddress = this.serverAddress,
                serverPort = this.serverPort,
                connectionTimeout = this.connectionTimeout,
                reconnectDelay = this.reconnectDelay,
                autoReconnect = this.autoReconnect
            };

            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);

            Debug.Log($"[ServerProfile] 설정 저장 완료: {ConfigFilePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ServerProfile] 설정 저장 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 기본 설정 파일 생성
    /// </summary>
    [ContextMenu("Create Default Config File")]
    public void CreateDefaultConfigFile()
    {
        SaveConfig();
    }

    /// <summary>
    /// 설정 새로고침
    /// </summary>
    [ContextMenu("Reload Config")]
    public void ReloadConfig()
    {
        LoadConfig();
    }

    /// <summary>
    /// 현재 설정을 ServerProfileData로 반환
    /// </summary>
    public ServerProfileData GetConfig()
    {
        return new ServerProfileData
        {
            serverAddress = this.serverAddress,
            serverPort = this.serverPort,
            connectionTimeout = this.connectionTimeout,
            reconnectDelay = this.reconnectDelay,
            autoReconnect = this.autoReconnect
        };
    }
}
