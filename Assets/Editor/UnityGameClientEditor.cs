using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UnityGameClient))]
public class UnityGameClientEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 기본 인스펙터 렌더링
        DrawDefaultInspector();

        var client = (UnityGameClient)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Controls", EditorStyles.boldLabel);

        // 접속 버튼
        if (GUILayout.Button("Connect to My Server (127.0.0.1:7777)"))
        {
            if (Application.isPlaying)
            {
                client.DebugConnectToMyServer();
            }
            else
            {
                EditorUtility.DisplayDialog("Play Mode Required", "플레이 모드에서만 접속할 수 있습니다.", "OK");
            }
        }

        // 연결 종료 버튼
        if (GUILayout.Button("Disconnect"))
        {
            if (Application.isPlaying)
            {
                client.DebugDisconnect();
            }
            else
            {
                EditorUtility.DisplayDialog("Play Mode Required", "플레이 모드에서만 종료할 수 있습니다.", "OK");
            }
        }
    }
}