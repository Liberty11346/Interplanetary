using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor; // 에디터 관련 API 사용을 위해 추가
#endif

public class MapManager : MonoBehaviour
{
    public List<planetCtrl> scenePlanets = new List<planetCtrl>();

    [Tooltip("행성 정보를 저장하거나 불러올 MapData ScriptableObject를 할당해주세요.")]
    public MapData mapData;

    [Tooltip("스냅샷을 캡처할 카메라를 할당해주세요.")]
    public Camera snapshotCamera;

    /// <summary>
    /// 씬에 있는 모든 행성을 찾아 초기화합니다. (연결선, 텍스트 등)
    /// </summary>
    public void InitPlanets()
    {
        // 자식 오브젝트에 있는 모든 planetCtrl 컴포넌트를 가져옵니다.
        scenePlanets = new List<planetCtrl>(GetComponentsInChildren<planetCtrl>());

        // 가져온 모든 행성에 대해 초기화를 실행합니다.
        foreach (planetCtrl scenePlanet in scenePlanets)
        {
            scenePlanet.InitPlanet(); // InitPlanet을 호출하여 텍스트, 라인 등을 갱신합니다.
        }
    }

    /// <summary>
    /// 현재 씬의 이름을 MapData 에셋에 저장합니다.
    /// </summary>
    public void SaveSceneName()
    {
#if UNITY_EDITOR
        if (mapData == null)
        {
            Debug.LogError("MapData 에셋이 MapManager에 할당되지 않았습니다.");
            return;
        }

        // 현재 GameObject가 속한 씬의 경로를 가져와 파일 이름만 추출합니다.
        string scenePath = gameObject.scene.path;
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

        // MapData에 씬 이름을 저장합니다.
        mapData.realMapName = sceneName;

        // 변경사항을 디스크에 저장합니다.
        EditorUtility.SetDirty(mapData);
        AssetDatabase.SaveAssets();

        Debug.Log($"'{mapData.name}' 에셋에 현재 씬 이름 '{sceneName}'이(가) 저장되었습니다.");
#else
        Debug.LogWarning("씬 이름 저장 기능은 Unity 에디터에서만 사용할 수 있습니다.");
#endif
    }

    /// <summary>
    /// 할당된 카메라를 사용하여 캡처하고 MapData 에셋에 미리보기 이미지로 저장합니다.
    /// </summary>
    /// <param name="width">캡처할 이미지의 너비</param>
    /// <param name="height">캡처할 이미지의 높이</param>
    public void CaptureMapSnapshot(int width = 512, int height = 512)
    {
#if UNITY_EDITOR
        if (mapData == null)
        {
            Debug.LogError("MapData 에셋이 PlanetsManager에 할당되지 않았습니다.");
            return;
        }

        if (snapshotCamera == null)
        {
            Debug.LogError("Snapshot Camera가 할당되지 않았습니다. 인스펙터에서 카메라를 할당해주세요.");
            return;
        }

        // 카메라의 원래 타겟 텍스처를 저장합니다.
        var originalTargetTexture = snapshotCamera.targetTexture;

        // 캡처를 위한 렌더 텍스처를 생성하고 카메라에 설정합니다.
        RenderTexture rt = new RenderTexture(width, height, 24);
        snapshotCamera.targetTexture = rt;
        snapshotCamera.Render();

        // 렌더 텍스처의 내용을 새로운 Texture2D로 복사합니다.
        RenderTexture.active = rt;
        Texture2D snapshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        snapshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        snapshot.Apply();

        // 카메라 설정을 원래대로 복원하고 사용한 리소스를 정리합니다.
        snapshotCamera.targetTexture = originalTargetTexture;
        RenderTexture.active = null;
        DestroyImmediate(rt);

        // MapData 에셋에 포함된 기존 미리보기 이미지(하위 에셋)를 모두 찾아 삭제합니다.
        string assetPath = AssetDatabase.GetAssetPath(mapData);
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
        snapshot.name = $"{mapData.name}_Preview";
        mapData.mapPreviewImage = snapshot;

        AssetDatabase.AddObjectToAsset(snapshot, mapData); // 텍스처를 MapData 에셋의 하위 에셋으로 저장
        EditorUtility.SetDirty(mapData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"'{mapData.name}' 에셋에 '{snapshotCamera.name}' 카메라의 스냅샷이 저장되었습니다.");
#else
        Debug.LogWarning("맵 스냅샷 캡처 기능은 Unity 에디터에서만 사용할 수 있습니다.");
#endif
    }
}
