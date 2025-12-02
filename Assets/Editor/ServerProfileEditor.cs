using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(ServerProfile))]
public class ServerProfileEditor : Editor
{
    private ServerProfile profile;
    private ServerProfileData editingData;
    private bool hasUnsavedChanges = false;

    private void OnEnable()
    {
        profile = (ServerProfile)target;
        LoadCurrentConfig();
    }

    private void LoadCurrentConfig()
    {
        editingData = new ServerProfileData
        {
            serverAddress = profile.serverAddress,
            serverPort = profile.serverPort,
            connectionTimeout = profile.connectionTimeout,
            reconnectDelay = profile.reconnectDelay,
            autoReconnect = profile.autoReconnect
        };
        hasUnsavedChanges = false;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Server Profile Editor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("이 설정은 JSON 파일에 저장됩니다. Inspector에서 편집하고 'Save to JSON' 버튼을 눌러 저장하세요.", MessageType.Info);
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

        string configPath = Path.Combine(Application.streamingAssetsPath, "server_profile.json");
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
            if (GUILayout.Button("🔌 Test Connection", GUILayout.Height(30)))
            {
                TestConnection();
            }
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
        profile.serverAddress = editingData.serverAddress;
        profile.serverPort = editingData.serverPort;
        profile.connectionTimeout = editingData.connectionTimeout;
        profile.reconnectDelay = editingData.reconnectDelay;
        profile.autoReconnect = editingData.autoReconnect;

        EditorUtility.SetDirty(profile);
        hasUnsavedChanges = false;
    }

    private void TestConnection()
    {
        Debug.Log($"[ServerProfile] Testing connection to {editingData.serverAddress}:{editingData.serverPort}");
        // Connection test logic can be added here
    }
}
