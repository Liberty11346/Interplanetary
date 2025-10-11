using UnityEngine;

public static class PlanetFactory
{
    private const string PlanetPrefabPath = "Prefabs/Planet";
    private static GameObject _planetPrefab;
    /// <summary>
    /// 기본 행성을 생성합니다.
    /// </summary>
    /// <param name="position">행성의 위치</param>
    /// <param name="planetName">행성의 이름</param>
    /// <returns>생성된 행성의 planetCtrl 컴포넌트</returns>
    public static planetCtrl CreatePlanet(Vector2 position, string planetName)
    {
        return CreatePlanet(position, planetName, 1, 1, 1);
    }

    /// <summary>
    /// 자원량이 지정된 행성을 생성합니다.
    /// </summary>
    /// <param name="position">행성의 위치</param>
    /// <param name="planetName">행성의 이름</param>
    /// <param name="mineralAmount">광물 자원량</param>
    /// <param name="gasAmount">가스 자원량</param>
    /// <param name="supplyAmount">보급 자원량</param>
    /// <param name="parent">행성을 부모로 할 Transform</param> 
    /// <returns>생성된 행성의 planetCtrl 컴포넌트</returns>
    public static planetCtrl CreatePlanet(Vector2 position, string planetName, int mineralAmount, int gasAmount, int supplyAmount, Transform parent = null)
    {
        if (_planetPrefab == null)
        {
            _planetPrefab = Resources.Load<GameObject>(PlanetPrefabPath);
            if (_planetPrefab == null)
            {
                Debug.LogError($"행성 프리팹을 'Resources/{PlanetPrefabPath}' 경로에서 찾을 수 없습니다. 프리팹을 해당 경로로 옮겨주세요.");
                return null;
            }
        }

        // 행성 게임오브젝트 생성
        GameObject planetObj = Object.Instantiate(
            _planetPrefab,
            new Vector3(position.x, position.y, 0),
            Quaternion.identity,
            parent ?? null
        );

        // 행성 이름 설정
        planetObj.name = planetName;

        // planetCtrl 컴포넌트 가져오기
        planetCtrl planet = planetObj.GetComponent<planetCtrl>();
        if (planet != null)
        {
            // 행성 속성 설정
            planet.planetName = planetName;
            planet.mineralAmount = mineralAmount;
            planet.gasAmount = gasAmount;
            planet.supplyAmount = supplyAmount;
        }
        else
        {
            Debug.LogError($"생성된 행성 '{planetName}'에 planetCtrl 컴포넌트가 없습니다.");
        }

        return planet;
    }

    /// <summary>
    /// PlanetData를 기반으로 행성을 생성합니다.
    /// </summary>
    /// <param name="data">행성 데이터</param>
    /// <param name="position">행성의 위치</param>
    /// <returns>생성된 행성의 planetCtrl 컴포넌트</returns>
    public static planetCtrl CreatePlanetFromData(PlanetData data, Vector2 position)
    {
        if (data == null)
        {
            Debug.LogError("행성 데이터가 null입니다.");
            return null;
        }

        planetCtrl planet = CreatePlanet(
            position,
            data.planetName,
            data.mineralAmount,
            data.gasAmount,
            data.supplyAmount
        );

        if (planet != null && data.Icon != null)
        {
            // 스프라이트 설정
            planet.sprite = data.Icon;

            // 스프라이트 렌더러에 적용
            SpriteRenderer spriteRenderer = planet.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = data.Icon;
            }
        }

        return planet;
    }
}
