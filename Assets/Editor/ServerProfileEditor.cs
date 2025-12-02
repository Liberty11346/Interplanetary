using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(ServerProfile))]
public class ServerProfileEditor : Editor
{
    private ServerProfile profile;
    private ServerProfileData editingData;
    private bool hasUnsavedChanges = false;

    // 프로필 관리 UI 상태
    private int selectedProfileIndex = 0;
    private string newProfileName = "";
    private bool showNewProfileInput = false;

    private void OnEnable()
    {
        profile = (ServerProfile)target;
        LoadCurrentConfig();
        UpdateSelectedProfileIndex();
    }

    private void LoadCurrentConfig()
    {
        editingData = new ServerProfileData
        {
            profileName = profile.activeProfileName,
            serverAddress = profile.serverAddress,
            serverPort = profile.serverPort,
            connectionTimeout = profile.connectionTimeout,
            reconnectDelay = profile.reconnectDelay,
            autoReconnect = profile.autoReconnect
        };
        hasUnsavedChanges = false;
    }

    private void UpdateSelectedProfileIndex()
    {
        var profileNames = profile.GetProfileNames();
        selectedProfileIndex = Mathf.Max(0, profileNames.IndexOf(profile.activeProfileName));
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Server Profile Manager", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("여러 서버 프로필을 관리할 수 있습니다. 프로필을 선택하고 설정을 편집한 후 저장하세요.", MessageType.Info);
        EditorGUILayout.Space();

        // Profile Selection
        DrawProfileSelector();
        EditorGUILayout.Space();

        // Server Settings
        EditorGUILayout.LabelField("Server Settings", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();

        editingData.serverAddress = EditorGUILayout.TextField(
            new GUIContent("Server Address", "서버 IP 주소 (예: 127.0.0.1, 192.168.0.100)"),
            editingData.serverAddress
        );

        editingData.serverPort = EditorGUILayout.IntField(
            new GUIContent("Server Port", "서버 포트 번호"),
            editingData.serverPort
        );

        EditorGUILayout.Space();

        // Connection Settings
        EditorGUILayout.LabelField("Connection Settings", EditorStyles.boldLabel);

        editingData.connectionTimeout = EditorGUILayout.IntField(
            new GUIContent("Connection Timeout (ms)", "연결 타임아웃 (밀리초)"),
            editingData.connectionTimeout
        );

        editingData.reconnectDelay = EditorGUILayout.IntField(
            new GUIContent("Reconnect Delay (ms)", "재연결 대기 시간 (밀리초)"),
            editingData.reconnectDelay
        );

        editingData.autoReconnect = EditorGUILayout.Toggle(
            new GUIContent("Auto Reconnect", "자동 재연결 활성화"),
            editingData.autoReconnect
        );

        if (EditorGUI.EndChangeCheck())
        {
            hasUnsavedChanges = true;
        }

        EditorGUILayout.Space();

        // Config File Settings
        EditorGUILayout.LabelField("Configuration File", EditorStyles.boldLabel);

        SerializedProperty useConfigFileProp = serializedObject.FindProperty("useConfigFile");
        EditorGUILayout.PropertyField(useConfigFileProp, new GUIContent("Use Config File", "외부 설정 파일 사용"));

        string configPath = Path.Combine(Application.streamingAssetsPath, "server_profiles.json");
        bool fileExists = File.Exists(configPath);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Config File Path:", GUILayout.Width(120));
        EditorGUILayout.SelectableLabel(configPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("File Status:", GUILayout.Width(120));
        if (fileExists)
        {
            EditorGUILayout.LabelField("✓ Exists", EditorStyles.boldLabel);
        }
        else
        {
            EditorGUILayout.LabelField("✗ Not Found", EditorStyles.boldLabel);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Unsaved changes warning
        if (hasUnsavedChanges)
        {
            EditorGUILayout.HelpBox("⚠ 저장되지 않은 변경사항이 있습니다!", MessageType.Warning);
        }

        EditorGUILayout.Space();

        // Buttons
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("💾 Save to JSON", GUILayout.Height(30)))
        {
            SaveToJson();
        }

        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("🔄 Reload from JSON", GUILayout.Height(30)))
        {
            ReloadFromJson();
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Apply to Inspector (Runtime Only)", GUILayout.Height(25)))
        {
            ApplyToInspector();
        }

        if (GUILayout.Button("Reset to Current Inspector", GUILayout.Height(25)))
        {
            LoadCurrentConfig();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Quick connect test
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("Runtime Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("🔌 Reconnect to Server", GUILayout.Height(30)))
            {
                profile.ReconnectToServer();
            }

            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("📊 Show Connection Info", GUILayout.Height(30)))
            {
                ShowConnectionInfo();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void SaveToJson()
    {
        try
        {
            // Apply to inspector first
            ApplyToInspector();

            // Save to JSON
            profile.SaveConfig();

            hasUnsavedChanges = false;
            EditorUtility.DisplayDialog("Success", "설정이 JSON 파일에 저장되었습니다.", "OK");
        }
        catch (System.Exception ex)
        {
            EditorUtility.DisplayDialog("Error", $"저장 실패: {ex.Message}", "OK");
        }
    }

    private void ReloadFromJson()
    {
        if (hasUnsavedChanges)
        {
            if (!EditorUtility.DisplayDialog("Confirm", "저장되지 않은 변경사항이 있습니다. 계속하시겠습니까?", "Yes", "No"))
            {
                return;
            }
        }

        profile.ReloadConfig();
        LoadCurrentConfig();
        EditorUtility.DisplayDialog("Success", "JSON 파일에서 설정을 다시 로드했습니다.", "OK");
    }

    private void ApplyToInspector()
    {
        profile.activeProfileName = editingData.profileName;
        profile.serverAddress = editingData.serverAddress;
        profile.serverPort = editingData.serverPort;
        profile.connectionTimeout = editingData.connectionTimeout;
        profile.reconnectDelay = editingData.reconnectDelay;
        profile.autoReconnect = editingData.autoReconnect;

        EditorUtility.SetDirty(profile);
        hasUnsavedChanges = false;
    }

    private void ShowConnectionInfo()
    {
        var handler = CommonLib.ClientServerHandler.Instance;
        if (handler != null)
        {
            string status = handler.IsConnected ? "연결됨 ✓" : "연결 안됨 ✗";
            string message = $"현재 프로필: {profile.activeProfileName}\n" +
                           $"서버 주소: {profile.serverAddress}:{profile.serverPort}\n" +
                           $"연결 상태: {status}";

            EditorUtility.DisplayDialog("Connection Info", message, "OK");
            Debug.Log($"[ServerProfile] {message}");
        }
        else
        {
            EditorUtility.DisplayDialog("Connection Info", "ClientServerHandler를 찾을 수 없습니다.", "OK");
        }
    }

    private void DrawProfileSelector()
    {
        EditorGUILayout.LabelField("Profile Selection", EditorStyles.boldLabel);

        var profileNames = profile.GetProfileNames();
        if (profileNames == null || profileNames.Count == 0)
        {
            EditorGUILayout.HelpBox("프로필이 없습니다. 'Create Default Profiles' 버튼을 눌러주세요.", MessageType.Warning);
            if (GUILayout.Button("Create Default Profiles"))
            {
                profile.CreateDefaultConfigFile();
                UpdateSelectedProfileIndex();
                LoadCurrentConfig();
            }
            return;
        }

        EditorGUILayout.BeginHorizontal();

        // Profile dropdown
        EditorGUI.BeginChangeCheck();
        int newIndex = EditorGUILayout.Popup("Active Profile", selectedProfileIndex, profileNames.ToArray());
        if (EditorGUI.EndChangeCheck())
        {
            if (hasUnsavedChanges)
            {
                if (!EditorUtility.DisplayDialog("Confirm", "저장되지 않은 변경사항이 있습니다. 계속하시겠습니까?", "Yes", "No"))
                {
                    return;
                }
            }

            selectedProfileIndex = newIndex;
            string selectedProfileName = profileNames[selectedProfileIndex];

            // 런타임 중이면 재연결 여부 확인
            bool reconnect = false;
            if (Application.isPlaying)
            {
                reconnect = EditorUtility.DisplayDialog("재연결 확인",
                    $"프로필을 '{selectedProfileName}'(으)로 전환합니다.\n서버에 재연결하시겠습니까?",
                    "재연결", "나중에");
            }

            profile.LoadProfile(selectedProfileName, reconnect);
            LoadCurrentConfig();
            UpdateSelectedProfileIndex();
        }

        // Delete button
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("✕", GUILayout.Width(25)))
        {
            string profileToDelete = profileNames[selectedProfileIndex];
            if (EditorUtility.DisplayDialog("Confirm Delete",
                $"프로필 '{profileToDelete}'을(를) 삭제하시겠습니까?", "Delete", "Cancel"))
            {
                if (profile.DeleteProfile(profileToDelete))
                {
                    profile.SaveAllProfiles();
                    UpdateSelectedProfileIndex();
                    LoadCurrentConfig();
                    EditorUtility.DisplayDialog("Success", "프로필이 삭제되었습니다.", "OK");
                }
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        // New Profile Section
        EditorGUILayout.Space(5);

        if (!showNewProfileInput)
        {
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("➕ New Profile"))
            {
                showNewProfileInput = true;
                newProfileName = "New Profile";
            }
            GUI.backgroundColor = Color.white;
        }
        else
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Create New Profile", EditorStyles.boldLabel);

            newProfileName = EditorGUILayout.TextField("Profile Name", newProfileName);

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Create"))
            {
                if (string.IsNullOrWhiteSpace(newProfileName))
                {
                    EditorUtility.DisplayDialog("Error", "프로필 이름을 입력하세요.", "OK");
                }
                else if (profileNames.Contains(newProfileName))
                {
                    EditorUtility.DisplayDialog("Error", "이미 존재하는 프로필 이름입니다.", "OK");
                }
                else
                {
                    var newProfile = new ServerProfileData
                    {
                        profileName = newProfileName,
                        serverAddress = "127.0.0.1",
                        serverPort = 9000,
                        connectionTimeout = 5000,
                        reconnectDelay = 3000,
                        autoReconnect = false
                    };

                    profile.AddProfile(newProfile);
                    profile.SaveAllProfiles();
                    profile.LoadProfile(newProfileName);
                    LoadCurrentConfig();
                    UpdateSelectedProfileIndex();

                    showNewProfileInput = false;
                    EditorUtility.DisplayDialog("Success", $"프로필 '{newProfileName}'이(가) 생성되었습니다.", "OK");
                }
            }

            GUI.backgroundColor = Color.gray;
            if (GUILayout.Button("Cancel"))
            {
                showNewProfileInput = false;
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }
}
