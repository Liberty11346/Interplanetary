using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapManager))]
public class MapManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MapManager manager = (MapManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("맵 관리 도구", EditorStyles.boldLabel);

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

        if (GUILayout.Button("맵 데이터에서 행성 불러오기"))
        {
            manager.LoadMapFromData();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("맵 정보 관리", EditorStyles.boldLabel);

        if (GUILayout.Button("맵 데이터 저장 (씬 이름 & 미리보기 & 행성 정보)"))
        {
            if (manager.mapData != null)
            {
                SavePlanetsToMapData(manager);
                if (manager.snapshotCamera != null)
                {
                    // MapData 에셋의 변경사항을 Undo 스택에 기록합니다.
                    Undo.RecordObject(manager.mapData, "Save Map Data");

                    const int fixedWidth = 1024;
                    int snapshotHeight = Mathf.RoundToInt(fixedWidth / manager.snapshotCamera.aspect);

                    SaveSceneName(manager);
                    CaptureMapSnapshot(manager, fixedWidth, snapshotHeight);
                }
                else
                {
                    Debug.LogError("Snapshot Camera가 할당되지 않았습니다. 카메라를 먼저 할당해주세요.");
                }
            }
        }
    }

    /// <summary>
    /// 현재 씬의 행성 데이터를 MapData에 저장합니다.
    /// </summary>
    private void SavePlanetsToMapData(MapManager mapManager)
    {
        if (mapManager.mapData == null)
        {
            Debug.LogError("MapData가 할당되지 않았습니다.");
            return;
        }

        // Undo 기록
        Undo.RecordObject(mapManager.mapData, "Save Planets to MapData");

        // 1. 기존에 저장된 PlanetData 하위 에셋들을 모두 제거합니다.
        //    이렇게 하지 않으면 저장할 때마다 데이터가 중복으로 쌓입니다.
        string mapDataPath = AssetDatabase.GetAssetPath(mapManager.mapData);
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(mapDataPath);
        foreach (var subAsset in subAssets)
        {
            if (subAsset is PlanetData)
            {
                Undo.DestroyObjectImmediate(subAsset); // Undo를 위해 DestroyImmediate 사용
            }
        }

        // 행성 데이터 리스트 초기화
        if (mapManager.mapData.planets == null)
        {
            mapManager.mapData.planets = new System.Collections.Generic.List<PlanetData>();
        }
        else
        {
            mapManager.mapData.planets.Clear();
        }

        // 행성 위치 리스트 초기화
        if (mapManager.mapData.planetPositions == null)
        {
            mapManager.mapData.planetPositions = new System.Collections.Generic.List<Vector2>();
        }
        else
        {
            mapManager.mapData.planetPositions.Clear();
        }

        // 행성 경로 리스트 초기화
        if (mapManager.mapData.planetPaths == null)
        {
            mapManager.mapData.planetPaths = new System.Collections.Generic.List<PlanetPath>();
        }
        else
        {
            mapManager.mapData.planetPaths.Clear();
        }

        // 씬의 모든 행성 정보를 MapData에 저장
        foreach (var planet in mapManager.scenePlanets)
        {
            // PlanetData 생성 및 설정
            PlanetData planetData = ScriptableObject.CreateInstance<PlanetData>();
            planetData.name = planet.planetName;
            planetData.planetName = planet.planetName;
            planetData.icon = planet.sprite;
            planetData.id = mapManager.scenePlanets.IndexOf(planet); // 인덱스를 ID로 사용
            planetData.mineralAmount = planet.mineralAmount;
            planetData.gasAmount = planet.gasAmount;
            planetData.supplyAmount = planet.supplyAmount;

            // 2. 생성된 PlanetData 인스턴스를 MapData 에셋의 '하위 에셋'으로 추가합니다.
            //    이 과정이 없으면 인스턴스가 파일로 저장되지 않아 Type Mismatch가 발생합니다.
            AssetDatabase.AddObjectToAsset(planetData, mapManager.mapData);

            // MapData에 행성 데이터 추가
            mapManager.mapData.planets.Add(planetData);
            // 행성 위치 저장
            mapManager.mapData.planetPositions.Add(planet.transform.position);

            // 행성 연결 정보 저장
            for (int i = 0; i < planet.nearPlanet.Length; i++)
            {
                if (planet.nearPlanet[i] != null)
                {
                    PlanetPath path = new PlanetPath();
                    path.fromID = mapManager.scenePlanets.IndexOf(planet);
                    path.toID = mapManager.scenePlanets.IndexOf(planet.nearPlanet[i].GetComponent<planetCtrl>()); // TODO: GetComponent가 null일 수 있음
                    mapManager.mapData.planetPaths.Add(path);
                }
            }
        }

        // 변경사항 저장
        EditorUtility.SetDirty(mapManager.mapData);
        AssetDatabase.SaveAssets(); // 모든 에셋 변경사항을 디스크에 씁니다.
        AssetDatabase.Refresh();    // 에셋 데이터베이스를 새로고침하여 변경사항을 즉시 반영합니다.
        Debug.Log($"현재 씬의 {mapManager.scenePlanets.Count}개 행성 데이터를 '{mapManager.mapData.name}' 에셋에 저장했습니다.");
    }

    /// <summary>
    /// 현재 씬의 이름을 MapData 에셋에 저장합니다.
    /// </summary>
    private void SaveSceneName(MapManager mapManager)
    {
        if (mapManager.mapData == null)
        {
            Debug.LogError("MapData 에셋이 MapManager에 할당되지 않았습니다.");
            return;
        }

        // 현재 GameObject가 속한 씬의 경로를 가져와 파일 이름만 추출합니다.
        string scenePath = mapManager.gameObject.scene.path;
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

        // 이미 같은 이름이면 변경하지 않음
        if (mapManager.mapData.realMapName == sceneName) return;

        // MapData에 씬 이름을 저장합니다.
        mapManager.mapData.realMapName = sceneName;

        // 변경사항을 디스크에 저장합니다.
        EditorUtility.SetDirty(mapManager.mapData);
        AssetDatabase.SaveAssets();

        Debug.Log($"'{mapManager.mapData.name}' 에셋에 현재 씬 이름 '{sceneName}'을(를) 저장했습니다.");
    }

    /// <summary>
    /// 할당된 카메라를 사용하여 캡처하고 MapData 에셋에 미리보기 이미지로 저장합니다.
    /// </summary>
    private void CaptureMapSnapshot(MapManager mapManager, int width = 512, int height = 512)
    {
        if (mapManager.mapData == null)
        {
            Debug.LogError("MapData 에셋이 PlanetsManager에 할당되지 않았습니다.");
            return;
        }

        if (mapManager.snapshotCamera == null)
        {
            Debug.LogError("Snapshot Camera가 할당되지 않았습니다. 인스펙터에서 카메라를 할당해주세요.");
            return;
        }

        // 카메라의 원래 타겟 텍스처를 저장합니다.
        var originalTargetTexture = mapManager.snapshotCamera.targetTexture;

        // 캡처를 위한 렌더 텍스처를 생성하고 카메라에 설정합니다.
        RenderTexture rt = new RenderTexture(width, height, 24);
        mapManager.snapshotCamera.targetTexture = rt;
        mapManager.snapshotCamera.Render();

        // 렌더 텍스처의 내용을 새로운 Texture2D로 복사합니다.
        RenderTexture.active = rt;
        Texture2D snapshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        snapshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        snapshot.Apply();

        // 카메라 설정을 원래대로 복원하고 사용한 리소스를 정리합니다.
        mapManager.snapshotCamera.targetTexture = originalTargetTexture;
        RenderTexture.active = null;
        DestroyImmediate(rt);

        // MapData 에셋에 포함된 기존 미리보기 이미지(하위 에셋)를 모두 찾아 삭제합니다.
        string assetPath = AssetDatabase.GetAssetPath(mapManager.mapData);
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (var subAsset in subAssets)
        {
            // Texture2D 타입이고, 이름이 "_Preview"로 끝나는 하위 에셋을 모두 삭제합니다.
            if (subAsset is Texture2D && subAsset.name.EndsWith("_Preview"))
            {
                DestroyImmediate(subAsset, true);
            }
        }

        // 캡처한 스냅샷을 MapData에 할당하고 에셋에 저장합니다.
        snapshot.name = $"{mapManager.mapData.name}_Preview";
        mapManager.mapData.mapPreviewImage = snapshot;

        AssetDatabase.AddObjectToAsset(snapshot, mapManager.mapData); // 텍스처를 MapData 에셋의 하위 에셋으로 저장
        EditorUtility.SetDirty(mapManager.mapData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"'{mapManager.mapData.name}' 에셋에 '{mapManager.snapshotCamera.name}' 카메라의 스냅샷이 저장되었습니다.");
    }
}
