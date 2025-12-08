using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FleetLibraryPopulator : EditorWindow
{
    [MenuItem("Tools/Populate Fleet Libraries (All Colors)")]
    public static void PopulateAllFleetLibraries()
    {
        PopulateFleetLibrary("Blue", new[] { "0", "1", "2", "3" });
        PopulateFleetLibrary("Green", new[] { "0", "1", "2", "3" });
        PopulateFleetLibrary("Orange", new[] { "0", "1", "2", "3" });
        PopulateFleetLibrary("Red", new[] { "0", "1", "2", "3" });
        
        AssetDatabase.Refresh();
        Debug.Log("모든 FleetLibrary 생성 완료!");
    }

    private static void PopulateFleetLibrary(string colorName, string[] spriteNames)
    {
        // FleetLibrary 에셋 경로 (색상별로 다른 파일)
        string assetPath = $"Assets/3.Prefab/UI/Resource/FleetLibrary_{colorName}.asset";
        FleetLibrary library = AssetDatabase.LoadAssetAtPath<FleetLibrary>(assetPath);

        if (library == null)
        {
            // 에셋이 없으면 새로 생성
            library = ScriptableObject.CreateInstance<FleetLibrary>();
            
            // 디렉토리가 없으면 생성
            string directory = System.IO.Path.GetDirectoryName(assetPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            AssetDatabase.CreateAsset(library, assetPath);
            Debug.Log($"FleetLibrary 에셋을 생성했습니다: {assetPath}");
        }

        // 함대 데이터 정의 (각 색상당 4가지 타입, 스프라이트는 1, 2, 3, 4번 사용)
        var fleetData = new[]
        {
            new { id = "0", name = "정찰기", sprite = spriteNames[0] },
            new { id = "1", name = "구축함", sprite = spriteNames[1] },
            new { id = "2", name = "순양함", sprite = spriteNames[2] },
            new { id = "3", name = "전투순양함", sprite = spriteNames[3] }
        };

        // SerializedObject를 사용하여 리스트에 접근
        SerializedObject serializedLibrary = new SerializedObject(library);
        SerializedProperty fleetListProp = serializedLibrary.FindProperty("fleetList");

        // 기존 리스트 클리어
        fleetListProp.ClearArray();

        // 각 함대 데이터를 추가
        for (int i = 0; i < fleetData.Length; i++)
        {
            var data = fleetData[i];

            // 새 요소 추가
            fleetListProp.InsertArrayElementAtIndex(i);
            SerializedProperty fleetProp = fleetListProp.GetArrayElementAtIndex(i);

            // 기존 필드 사용: id, displayName, icon, description
            fleetProp.FindPropertyRelative("id").stringValue = data.id;
            fleetProp.FindPropertyRelative("displayName").stringValue = data.name;

            // 스프라이트 로드
            Sprite sprite = FindFleetSprite(colorName, data.sprite);
            
            if (sprite != null)
            {
                fleetProp.FindPropertyRelative("icon").objectReferenceValue = sprite;
            }
            else
            {
                Debug.LogWarning($"스프라이트를 찾을 수 없습니다: {data.sprite} ({colorName})");
            }

            // description은 빈 문자열로 설정
            fleetProp.FindPropertyRelative("description").stringValue = "";
        }

        // 변경사항 적용
        serializedLibrary.ApplyModifiedProperties();

        // 에셋 저장
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();

        Debug.Log($"FleetLibrary_{colorName}에 {fleetData.Length}개의 함대 데이터가 성공적으로 추가되었습니다!");
    }

    private static Sprite FindFleetSprite(string colorName, string spriteIndex)
    {
        // Blue Fleet 스프라이트 시트에서 찾기
        if (colorName == "Blue")
        {
            string path = "Assets/2.Sprite/Starship/PixelArtStarshipFleetPackage_TerranFleet_Blue.png";
            string spriteName = $"PixelArtStarshipFleetPackage_TerranFleet_Blue_{spriteIndex}";
            Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var obj in sprites)
            {
                if (obj is Sprite sprite && sprite.name == spriteName)
                {
                    return sprite;
                }
            }
        }
        // Red Fleet 스프라이트 시트에서 찾기
        else if (colorName == "Red")
        {
            string path = "Assets/2.Sprite/Starship/PixelArtStarshipFleetPackage_TerranFleet_Red.png";
            string spriteName = $"PixelArtStarshipFleetPackage_TerranFleet_Red_{spriteIndex}";
            Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var obj in sprites)
            {
                if (obj is Sprite sprite && sprite.name == spriteName)
                {
                    return sprite;
                }
            }
        }
        // Green Fleet (단일 스프라이트)
        else if (colorName == "Green")
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/2.Sprite/Starship/PixelArtStarshipFleetPackage_TerranFleet_Green.png");
        }
        // Orange Fleet (단일 스프라이트)
        else if (colorName == "Orange")
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/2.Sprite/Starship/PixelArtStarshipFleetPackage_TerranFleet_Orange.png");
        }

        return null;
    }
}
