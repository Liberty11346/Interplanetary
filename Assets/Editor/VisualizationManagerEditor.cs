using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VisualizationManager))]
public class VisualizationManagerEditor : Editor
{
    private static int _planetIdCounter = 100;
    private static int _fleetIdCounter = 100;

    public override void OnInspectorGUI()
    {
        // 기본 인스펙터 UI를 그립니다.
        base.OnInspectorGUI();

        // 대상 스크립트에 대한 참조를 가져옵니다.
        VisualizationManager vizManager = (VisualizationManager)target;

        // 디버깅용 UI를 추가합니다.
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("디버그 생성기", EditorStyles.boldLabel);

        if (GUILayout.Button("행성 생성"))
        {
            // 행성을 랜덤 위치에 생성합니다.
            Vector3 randomPosition = new Vector3(Random.Range(-10, 10), Random.Range(-5, 5), 0);
            vizManager.CreatePlanet(_planetIdCounter++, randomPosition);
            Debug.Log("에디터에서 행성을 생성했습니다.");
        }

        if (GUILayout.Button("플레이어 함대 생성"))
        {
            // ownerId 1을 플레이어로 간주합니다.
            vizManager.CreateFleet(_fleetIdCounter++, 1, 1);
            Debug.Log("에디터에서 플레이어 함대를 생성했습니다.");
        }

        if (GUILayout.Button("적 함대 생성"))
        {
            // ownerId 2를 적으로 간주합니다.
            vizManager.CreateFleet(_fleetIdCounter++, 1, 2);
            Debug.Log("에디터에서 적 함대를 생성했습니다.");
        }
    }
}
