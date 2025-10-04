using UnityEditor;
using UnityEngine;
using TMPro; // TextMeshPro를 사용하기 위해 추가

/// <summary>
/// planetCtrl의 커스텀 에디터입니다.
/// 인스펙터에 초기화 버튼을 추가하여 행성 간 연결선과 자원 텍스트를 바로 갱신할 수 있습니다.
/// </summary>
[CustomEditor(typeof(planetCtrl))]
[CanEditMultipleObjects] // 여러 오브젝트를 동시에 편집할 수 있도록 설정합니다.
public class planetCtrlEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 기본 인스펙터 UI를 그립니다.
        DrawDefaultInspector();

        // "행성 초기화 (연결선/자원 텍스트 갱신)" 버튼을 추가합니다.
        if (GUILayout.Button("행성 초기화 (연결선/자원 텍스트 갱신)"))
        {
            // 선택된 모든 타겟에 대해 초기화를 실행합니다.
            foreach (var t in targets)
            {
                planetCtrl planet = (planetCtrl)t;

                // 변경될 모든 컴포넌트를 Undo 스택에 기록하여 변경사항이 저장되도록 합니다.
                Undo.RecordObject(planet, "Init Planet");
                Undo.RecordObject(planet.transform, "Init Planet Transform");

                // 자식 오브젝트의 컴포넌트들도 모두 기록합니다.
                foreach (var component in planet.GetComponentsInChildren<Component>(true))
                {
                    Undo.RecordObject(component, "Init Planet Children");
                }

                planet.InitPlanet();
            }
            Debug.Log($"{targets.Length}개의 행성이 초기화되었습니다.");
        }
    }
}