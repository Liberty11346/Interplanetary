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

        // 행성 생성 - 플레이어 진영
        if (GUILayout.Button("플레이어 행성 생성"))
        {
            int ownerId = 1; // 플레이어
            Vector3 randomPosition = new Vector3(Random.Range(-10, 10), Random.Range(-5, 5), 0);
            vizManager.CreatePlanetWithOwner(_planetIdCounter++, randomPosition, ownerId);
            Debug.Log("에디터에서 플레이어 행성을 생성했습니다.");
        }

        // 행성 생성 - 적 진영
        if (GUILayout.Button("적 행성 생성"))
        {
            int ownerId = 2; // 적
            Vector3 randomPosition = new Vector3(Random.Range(-10, 10), Random.Range(-5, 5), 0);
            vizManager.CreatePlanetWithOwner(_planetIdCounter++, randomPosition, ownerId);
            Debug.Log("에디터에서 적 행성을 생성했습니다.");
        }

        if (GUILayout.Button("플레이어 함대 생성"))
        {
            // ownerId 1을 플레이어로 간주합니다.
            int ownerId = 1;
            int planetId = GameManager.Instance != null ? GameManager.Instance.GetHomePlanetId(ownerId) : 1;
            vizManager.CreateFleet(_fleetIdCounter++, 1, ownerId, planetId);
            Debug.Log("에디터에서 플레이어 함대를 생성했습니다.");
        }

        if (GUILayout.Button("적 함대 생성"))
        {
            // ownerId 2를 적으로 간주합니다.
            int ownerId = 2;
            int planetId = GameManager.Instance != null ? GameManager.Instance.GetHomePlanetId(ownerId) : 2;
            vizManager.CreateFleet(_fleetIdCounter++, 1, ownerId, planetId);
            Debug.Log("에디터에서 적 함대를 생성했습니다.");
        }
    }
}
