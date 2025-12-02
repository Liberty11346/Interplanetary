using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 런타임에서 서버 프로필을 전환할 수 있는 UI 컴포넌트
/// </summary>
public class RuntimeServerProfileSwitcher : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Dropdown profileDropdown;
    [SerializeField] private Button reconnectButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Settings")]
    [SerializeField] private bool autoReconnectOnSwitch = true;

    private ServerProfile serverProfile;
    private List<string> profileNames;

    private void Start()
    {
        serverProfile = ServerProfile.Instance;

        if (serverProfile == null)
        {
            Debug.LogError("[RuntimeServerProfileSwitcher] ServerProfile 인스턴스를 찾을 수 없습니다.");
            return;
        }

        InitializeUI();

        // 프로필 변경 이벤트 구독
        serverProfile.OnProfileChanged += OnProfileChanged;
    }

    private void OnDestroy()
    {
        if (serverProfile != null)
        {
            serverProfile.OnProfileChanged -= OnProfileChanged;
        }
    }

    private void InitializeUI()
    {
        // 프로필 목록 가져오기
        profileNames = serverProfile.GetProfileNames();

        if (profileDropdown != null)
        {
            // 드롭다운 옵션 설정
            profileDropdown.ClearOptions();
            profileDropdown.AddOptions(profileNames);

            // 현재 활성 프로필 선택
            int currentIndex = profileNames.IndexOf(serverProfile.activeProfileName);
            if (currentIndex >= 0)
            {
                profileDropdown.value = currentIndex;
            }

            // 드롭다운 변경 이벤트 리스너 추가
            profileDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }

        if (reconnectButton != null)
        {
            reconnectButton.onClick.AddListener(OnReconnectButtonClicked);
        }

        UpdateStatusText();
    }

    private void OnDropdownValueChanged(int index)
    {
        if (index < 0 || index >= profileNames.Count)
            return;

        string selectedProfile = profileNames[index];

        // 이미 선택된 프로필이면 무시
        if (selectedProfile == serverProfile.activeProfileName)
            return;

        // 프로필 전환
        bool reconnect = autoReconnectOnSwitch;
        serverProfile.LoadProfile(selectedProfile, reconnect);

        Debug.Log($"[RuntimeServerProfileSwitcher] 프로필 전환: {selectedProfile} (재연결: {reconnect})");
    }

    private void OnReconnectButtonClicked()
    {
        serverProfile.ReconnectToServer();
        Debug.Log("[RuntimeServerProfileSwitcher] 재연결 버튼 클릭");
    }

    private void OnProfileChanged(string profileName)
    {
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        if (statusText != null && serverProfile != null)
        {
            var config = serverProfile.GetConfig();
            statusText.text = $"서버: {config.serverAddress}:{config.serverPort}";
        }
    }

    /// <summary>
    /// 프로필 목록 새로고침 (프로필이 추가/삭제된 경우)
    /// </summary>
    public void RefreshProfileList()
    {
        profileNames = serverProfile.GetProfileNames();

        if (profileDropdown != null)
        {
            profileDropdown.ClearOptions();
            profileDropdown.AddOptions(profileNames);

            int currentIndex = profileNames.IndexOf(serverProfile.activeProfileName);
            if (currentIndex >= 0)
            {
                profileDropdown.value = currentIndex;
            }
        }

        UpdateStatusText();
    }
}
