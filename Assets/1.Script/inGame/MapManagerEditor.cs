using UnityEditor;
using UnityEngine;

/// <summary>
/// MapManager의 커스텀 에디터입니다.
/// </summary>
[CustomEditor(typeof(MapManager))]
public class MapManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 기본 인스펙터 UI를 그립니다.
        DrawDefaultInspector();

        // 대상 PlanetsManager 스크립트 인스턴스를 가져옵니다.
        MapManager manager = (MapManager)target;

        // "전체 행성 초기화" 버튼
        if (GUILayout.Button("전체 행성 초기화 (연결선/자원 텍스트 갱신)"))
        {
            // 변경될 모든 자식 행성 오브젝트들을 Undo 스택에 기록합니다.
            var planetsToModify = manager.GetComponentsInChildren<planetCtrl>(true);
            foreach (var planet in planetsToModify)
            {
                Undo.RecordObject(planet, "Init All Planets");
                Undo.RecordObject(planet.transform, "Init All Planets Transform");
                foreach (var component in planet.GetComponentsInChildren<Component>(true))
                {
                    Undo.RecordObject(component, "Init All Planets Children");
                }
            }

            manager.InitPlanets();
            Debug.Log($"{manager.name}의 모든 행성이 초기화되었습니다.");
        }

        // "맵 데이터 저장 (씬 이름 & 미리보기)" 버튼
        if (GUILayout.Button("맵 데이터 저장 (씬 이름 & 미리보기)"))
        {
            if (manager.mapData != null)
            {
                if (manager.snapshotCamera != null)
                {
                    // MapData 에셋의 변경사항을 Undo 스택에 기록합니다.
                    Undo.RecordObject(manager.mapData, "Save Map Data");



                    //------캡쳐
                    const int fixedWidth = 1028;
                    int snapshotHeight = Mathf.RoundToInt(fixedWidth / manager.snapshotCamera.aspect);

                    manager.SaveSceneName();
                    manager.CaptureMapSnapshot(fixedWidth, snapshotHeight);
                    //---------
                }
                else
                {
                    Debug.LogError("Snapshot Camera가 할당되지 않았습니다. 카메라를 먼저 할당해주세요.");
                }
            }
            else
            {
                Debug.LogError("MapData 에셋을 먼저 할당해주세요.");
            }
        }
    }
}