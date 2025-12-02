using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// 서버 프로필 데이터 구조
/// </summary>
[Serializable]
public class ServerProfileData
{
    public string profileName = "Default";
    public string serverAddress = "127.0.0.1";
    public int serverPort = 9000;
    public int connectionTimeout = 5000;
    public int reconnectDelay = 3000;
    public bool autoReconnect = false;
}

/// <summary>
/// 여러 프로필을 관리하는 컨테이너
/// </summary>
[Serializable]
public class ServerProfileContainer
{
    public List<ServerProfileData> profiles = new List<ServerProfileData>();
    public string activeProfileName = "Default";
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

    [Header("Current Profile")]
    [Tooltip("현재 활성화된 프로필 이름")]
    public string activeProfileName = "Default";

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
    [Tooltip("외부 설정 파일 사용 (StreamingAssets/server_profiles.json)")]
    public bool useConfigFile = true;

    // 프로필 컨테이너
    private ServerProfileContainer _profileContainer;

    // 파일 경로
    private string ProfilesFilePath => Path.Combine(Application.streamingAssetsPath, "server_profiles.json");

    // 이벤트
    public event Action<string> OnProfileChanged;

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
        LoadAllProfiles();
    }

    /// <summary>
    /// 모든 프로필 로드
    /// </summary>
    private void LoadAllProfiles()
    {
        if (useConfigFile && File.Exists(ProfilesFilePath))
        {
            try
            {
                string json = File.ReadAllText(ProfilesFilePath);
                _profileContainer = JsonConvert.DeserializeObject<ServerProfileContainer>(json);

                if (_profileContainer == null || _profileContainer.profiles == null || _profileContainer.profiles.Count == 0)
                {
                    Debug.LogWarning("[ServerProfile] 프로필 파일이 비어있음. 기본 프로필 생성.");
                    CreateDefaultProfiles();
                }
                else
                {
                    // 활성 프로필 로드
                    LoadProfile(_profileContainer.activeProfileName);
                    Debug.Log($"[ServerProfile] {_profileContainer.profiles.Count}개 프로필 로드 완료. 활성 프로필: {activeProfileName}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ServerProfile] 프로필 파일 로드 실패: {ex.Message}. 기본 프로필 생성.");
                CreateDefaultProfiles();
            }
        }
        else
        {
            Debug.Log($"[ServerProfile] 프로필 파일 없음. 기본 프로필 생성.");
            CreateDefaultProfiles();
        }
    }

    /// <summary>
    /// 기본 프로필 생성
    /// </summary>
    private void CreateDefaultProfiles()
    {
        _profileContainer = new ServerProfileContainer
        {
            activeProfileName = "Default",
            profiles = new List<ServerProfileData>
            {
                new ServerProfileData
                {
                    profileName = "Default",
                    serverAddress = "127.0.0.1",
                    serverPort = 9000,
                    connectionTimeout = 5000,
                    reconnectDelay = 3000,
                    autoReconnect = false
                },
                new ServerProfileData
                {
                    profileName = "Production",
                    serverAddress = "125.137.73.37",
                    serverPort = 9000,
                    connectionTimeout = 5000,
                    reconnectDelay = 3000,
                    autoReconnect = false
                }
            }
        };

        LoadProfile("Default");
        SaveAllProfiles();
    }

    /// <summary>
    /// 특정 프로필 로드
    /// </summary>
    /// <param name="profileName">로드할 프로필 이름</param>
    /// <param name="reconnect">런타임 중이면 자동으로 재연결할지 여부</param>
    public bool LoadProfile(string profileName, bool reconnect = false)
    {
        if (_profileContainer == null || _profileContainer.profiles == null)
        {
            Debug.LogError("[ServerProfile] 프로필 컨테이너가 초기화되지 않음.");
            return false;
        }

        var profile = _profileContainer.profiles.FirstOrDefault(p => p.profileName == profileName);
        if (profile != null)
        {
            activeProfileName = profile.profileName;
            serverAddress = profile.serverAddress;
            serverPort = profile.serverPort;
            connectionTimeout = profile.connectionTimeout;
            reconnectDelay = profile.reconnectDelay;
            autoReconnect = profile.autoReconnect;

            _profileContainer.activeProfileName = profileName;

            Debug.Log($"[ServerProfile] 프로필 로드: {profileName} ({serverAddress}:{serverPort})");
            OnProfileChanged?.Invoke(profileName);

            // 런타임 중이고 재연결 플래그가 true면 재연결 시도
            if (reconnect && Application.isPlaying)
            {
                ReconnectToServer();
            }

            return true;
        }
        else
        {
            Debug.LogWarning($"[ServerProfile] 프로필을 찾을 수 없음: {profileName}");
            return false;
        }
    }

    /// <summary>
    /// 현재 프로필로 서버에 재연결
    /// </summary>
    public async void ReconnectToServer()
    {
        try
        {
            Debug.Log($"[ServerProfile] 재연결 시도: {serverAddress}:{serverPort}");

            // ClientServerHandler가 있는지 확인
            var handler = CommonLib.ClientServerHandler.Instance;
            if (handler != null)
            {
                // 기존 연결 종료
                if (handler.IsConnected)
                {
                    Debug.Log("[ServerProfile] 기존 연결 종료 중...");
                    handler.Disconnect();
                    await System.Threading.Tasks.Task.Delay(500); // 잠시 대기
                }

                // 새 프로필로 연결
                Debug.Log($"[ServerProfile] 새 서버로 연결 중: {serverAddress}:{serverPort}");
                await handler.ConnectAsync(serverAddress, serverPort);
                Debug.Log("[ServerProfile] 재연결 완료!");
            }
            else
            {
                Debug.LogWarning("[ServerProfile] ClientServerHandler 인스턴스를 찾을 수 없습니다.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ServerProfile] 재연결 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 현재 Inspector 값으로 활성 프로필 업데이트
    /// </summary>
    public void UpdateActiveProfile()
    {
        if (_profileContainer == null || _profileContainer.profiles == null)
        {
            Debug.LogError("[ServerProfile] 프로필 컨테이너가 초기화되지 않음.");
            return;
        }

        var profile = _profileContainer.profiles.FirstOrDefault(p => p.profileName == activeProfileName);
        if (profile != null)
        {
            profile.serverAddress = serverAddress;
            profile.serverPort = serverPort;
            profile.connectionTimeout = connectionTimeout;
            profile.reconnectDelay = reconnectDelay;
            profile.autoReconnect = autoReconnect;

            Debug.Log($"[ServerProfile] 프로필 업데이트: {activeProfileName}");
        }
        else
        {
            Debug.LogWarning($"[ServerProfile] 활성 프로필을 찾을 수 없음: {activeProfileName}");
        }
    }

    /// <summary>
    /// 새 프로필 추가
    /// </summary>
    public void AddProfile(ServerProfileData newProfile)
    {
        if (_profileContainer == null)
            _profileContainer = new ServerProfileContainer();

        if (_profileContainer.profiles == null)
            _profileContainer.profiles = new List<ServerProfileData>();

        // 중복 체크
        if (_profileContainer.profiles.Any(p => p.profileName == newProfile.profileName))
        {
            Debug.LogWarning($"[ServerProfile] 이미 존재하는 프로필 이름: {newProfile.profileName}");
            return;
        }

        _profileContainer.profiles.Add(newProfile);
        Debug.Log($"[ServerProfile] 프로필 추가: {newProfile.profileName}");
    }

    /// <summary>
    /// 프로필 삭제
    /// </summary>
    public bool DeleteProfile(string profileName)
    {
        if (_profileContainer == null || _profileContainer.profiles == null)
            return false;

        // 최소 1개 프로필은 유지
        if (_profileContainer.profiles.Count <= 1)
        {
            Debug.LogWarning("[ServerProfile] 최소 1개의 프로필은 유지되어야 합니다.");
            return false;
        }

        var profile = _profileContainer.profiles.FirstOrDefault(p => p.profileName == profileName);
        if (profile != null)
        {
            _profileContainer.profiles.Remove(profile);

            // 삭제된 프로필이 활성 프로필이었다면 첫 번째 프로필로 전환
            if (_profileContainer.activeProfileName == profileName)
            {
                LoadProfile(_profileContainer.profiles[0].profileName);
            }

            Debug.Log($"[ServerProfile] 프로필 삭제: {profileName}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// 모든 프로필 저장
    /// </summary>
    public void SaveAllProfiles()
    {
        try
        {
            // 현재 Inspector 값으로 활성 프로필 업데이트
            UpdateActiveProfile();

            // StreamingAssets 폴더가 없으면 생성
            string directory = Path.GetDirectoryName(ProfilesFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonConvert.SerializeObject(_profileContainer, Formatting.Indented);
            File.WriteAllText(ProfilesFilePath, json);

            Debug.Log($"[ServerProfile] 모든 프로필 저장 완료: {ProfilesFilePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ServerProfile] 프로필 저장 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 현재 활성 프로필만 저장 (하위 호환성)
    /// </summary>
    public void SaveConfig()
    {
        SaveAllProfiles();
    }

    /// <summary>
    /// 프로필 목록 가져오기
    /// </summary>
    public List<ServerProfileData> GetAllProfiles()
    {
        return _profileContainer?.profiles ?? new List<ServerProfileData>();
    }

    /// <summary>
    /// 활성 프로필 이름 목록 가져오기
    /// </summary>
    public List<string> GetProfileNames()
    {
        return _profileContainer?.profiles?.Select(p => p.profileName).ToList() ?? new List<string>();
    }

    /// <summary>
    /// 현재 설정을 ServerProfileData로 반환
    /// </summary>
    public ServerProfileData GetConfig()
    {
        return new ServerProfileData
        {
            profileName = this.activeProfileName,
            serverAddress = this.serverAddress,
            serverPort = this.serverPort,
            connectionTimeout = this.connectionTimeout,
            reconnectDelay = this.reconnectDelay,
            autoReconnect = this.autoReconnect
        };
    }

    /// <summary>
    /// 설정 새로고침
    /// </summary>
    [ContextMenu("Reload Config")]
    public void ReloadConfig()
    {
        LoadAllProfiles();
    }

    /// <summary>
    /// 기본 설정 파일 생성
    /// </summary>
    [ContextMenu("Create Default Config File")]
    public void CreateDefaultConfigFile()
    {
        CreateDefaultProfiles();
        SaveAllProfiles();
    }
}
