using System.Drawing;
using UnityEngine;
using UnityEngine.Rendering;

public class UIPath : MonoBehaviour
{ 
    [SerializeField]
    private RectTransform lineRect;
    private LineRenderer lineRenderer;

    [SerializeField]
    public PathData pathData;

    /// <summary>
    /// 초기화 (UIPath_Layer에서 호출)
    /// </summary>
    public void Initialize(PathData data)
    {
        pathData = data;

        lineRect = transform.Find("image").GetComponent<RectTransform>();
        lineRenderer = GetComponent<LineRenderer>();

        // Vector2 middlePoint = Vector2.Lerp(data.FromPosition, data.ToPosition, 0.5f); // 중간점 계산
        // transform.position = middlePoint; // 중간점으로 이동

        // float distance = Vector2.Distance(data.FromPosition, data.ToPosition); // 두 점 사이의 거리 계산
        // lineSprite.sizeDelta = new Vector2(2, distance); // 두 점 사이의 거리만큼 스프라이트 길이 조절

        // // 각도 조절
        // float angle = Mathf.Atan2(data.FromPosition.x, data.FromPosition.y) * Mathf.Rad2Deg;
        // transform.rotation = Quaternion.Euler(0, 0, angle);

        // lineRenderer.positionCount = 2;
        // lineRenderer.SetPosition(0, new Vector3(data.FromPosition.x, data.FromPosition.y, 0));
        // lineRenderer.SetPosition(1, new Vector3(data.ToPosition.x, data.ToPosition.y, 0));
        
        // // 선 색상 설정
        // lineRenderer.startColor = data.LineColor;
        // lineRenderer.endColor = data.LineColor;
        
        // // 선 두께 설정
        // lineRenderer.startWidth = 2f;
        // lineRenderer.endWidth = 2f;

        DrawLine(data.FromPosition, data.ToPosition);
    }

    public void DrawLine(Vector2 p1, Vector2 p2)
    {
        // 1. 선의 길이 계산
        float distance = Vector2.Distance(p1, p2);

        // 2. 선의 시작점 설정
        lineRect.anchoredPosition = p1;

        // 3. 선의 길이(Width)와 두께(Height) 설정
        // Width가 선의 길이, Height가 선의 두께가 됩니다.
        lineRect.sizeDelta = new Vector2(distance, 2); // <--- 순서 변경

        // 4. 선의 회전 각도 계산 (보정값 제거)
        Vector2 direction = p2 - p1;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Image의 기본 축이 X축(오른쪽)을 바라보게 되므로, 보정 없이 바로 각도를 사용합니다.
        lineRect.rotation = Quaternion.Euler(0, 0, angle); // <--- -90f 보정값 제거
    }

    /// <summary>
    /// 데이터 업데이트 (색상 변경 등)
    /// </summary>
    public void SetData(PathData data)
    {
        pathData = data;
    }
}
