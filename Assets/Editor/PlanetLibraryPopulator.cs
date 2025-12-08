using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PlanetLibraryPopulator : EditorWindow
{
    [MenuItem("Tools/Populate Planet Library")]
    public static void PopulatePlanetLibrary()
    {
        // PlanetLibrary 에셋 찾기
        string assetPath = "Assets/3.Prefab/UI/Resource/PlanetLibrary.asset";
        PlanetLibrary library = AssetDatabase.LoadAssetAtPath<PlanetLibrary>(assetPath);

        if (library == null)
        {
            Debug.LogError($"PlanetLibrary를 찾을 수 없습니다: {assetPath}");
            return;
        }

        // 행성 데이터 정의 (id, name, resource만 사용)
        var planetData = new[]
        {
            new { id = 0, name = "Zeus", resource = "Orange-Planet" },
            new { id = 1, name = "Earth", resource = "earth-like" },
            new { id = 2, name = "Zephyros", resource = "Storm-Planet" },
            new { id = 3, name = "Draconis", resource = "Lava-PLanet" },
            new { id = 4, name = "Terrarosa", resource = "Sand-Planet" },
            new { id = 5, name = "Asus", resource = "Ice-Planet" },
            new { id = 6, name = "Intelli", resource = "Water-Planet-wWih-Small-Islands" },
            new { id = 7, name = "Aiur", resource = "Orange-Planet" },
            new { id = 8, name = "Auriga", resource = "Storm-Planet" },
            new { id = 9, name = "Kanterbury", resource = "Red-Planet-With-Ice" },
            new { id = 10, name = "Runeterra", resource = "cyan-planet" },
            new { id = 11, name = "Dunwall", resource = "Ice-Planet" },
            new { id = 12, name = "Kevin", resource = "Purple-Planet" },
            new { id = 13, name = "Azeroth", resource = "Sand-Planet" },
            new { id = 14, name = "Korhal", resource = "Dark-PLanet" },
            new { id = 15, name = "Torterine", resource = "blue-planet" },
            new { id = 16, name = "Char", resource = "red-planet-sputnik" },
            new { id = 17, name = "Maru", resource = "earth-like" },
            new { id = 18, name = "Elysion", resource = "cyan-planet" },
            new { id = 19, name = "Charon", resource = "Storm-Planet" },
            new { id = 20, name = "Xentaris", resource = "Sand-Planet" },
            new { id = 21, name = "Titan", resource = "earth-like" },
            new { id = 22, name = "Novares", resource = "Storm-Planet" },
            new { id = 23, name = "Golder", resource = "Orange-Planet" },
            new { id = 24, name = "Persephon", resource = "Lava-PLanet" },
            new { id = 25, name = "Lumina", resource = "red-planet-sputnik" },
            new { id = 26, name = "Astra", resource = "Red-Planet-With-Ice" },
            new { id = 27, name = "Althea", resource = "Purple-Planet" },
            new { id = 28, name = "Seraphis", resource = "Ice-Planet" }
        };

        // SerializedObject를 사용하여 리스트에 접근
        SerializedObject serializedLibrary = new SerializedObject(library);
        SerializedProperty planetListProp = serializedLibrary.FindProperty("planetList");

        // 기존 리스트 클리어
        planetListProp.ClearArray();

        // 각 행성 데이터를 추가
        for (int i = 0; i < planetData.Length; i++)
        {
            var data = planetData[i];

            // 새 요소 추가
            planetListProp.InsertArrayElementAtIndex(i);
            SerializedProperty planetProp = planetListProp.GetArrayElementAtIndex(i);

            // 기존 필드만 사용: id, displayName, icon, description
            planetProp.FindPropertyRelative("id").stringValue = data.id.ToString();
            planetProp.FindPropertyRelative("displayName").stringValue = data.name;

            // 스프라이트 로드
            string spritePath = $"Assets/2.Sprite/Planet/{data.resource}.png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            
            if (sprite != null)
            {
                planetProp.FindPropertyRelative("icon").objectReferenceValue = sprite;
            }
            else
            {
                Debug.LogWarning($"스프라이트를 찾을 수 없습니다: {spritePath}");
            }

            // description은 빈 문자열로 설정
            planetProp.FindPropertyRelative("description").stringValue = "";
        }

        // 변경사항 적용
        serializedLibrary.ApplyModifiedProperties();

        // 에셋 저장
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"PlanetLibrary에 {planetData.Length}개의 행성 데이터가 성공적으로 추가되었습니다!");
    }
}
