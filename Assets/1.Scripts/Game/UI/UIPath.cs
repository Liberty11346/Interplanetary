using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class UIPath : MonoBehaviour
{ 
    [SerializeField]
    private LineRenderer lineRenderer;

    [SerializeField]
    public PathData pathData;

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
    }

    /// <summary>
    /// 초기화 (UIPath_Layer에서 호출)
    /// </summary>
    public void Initialize(PathData data)
    {
        pathData = data;
        
        // LineRenderer 설정
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, new Vector3(data.FromPosition.x, data.FromPosition.y, 0));
        lineRenderer.SetPosition(1, new Vector3(data.ToPosition.x, data.ToPosition.y, 0));
        
        // 선 색상 설정
        lineRenderer.startColor = data.LineColor;
        lineRenderer.endColor = data.LineColor;
        
        // 선 두께 설정
        lineRenderer.startWidth = 2f;
        lineRenderer.endWidth = 2f;
    }

    /// <summary>
    /// 데이터 업데이트 (색상 변경 등)
    /// </summary>
    public void SetData(PathData data)
    {
        pathData = data;
        
        // 색상만 업데이트 (위치는 고정)
        lineRenderer.startColor = data.LineColor;
        lineRenderer.endColor = data.LineColor;
    }
}
