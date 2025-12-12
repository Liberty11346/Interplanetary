using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIFleet : MonoBehaviour
{
    [SerializeField]
    RectTransform rectTransform;
    [SerializeField]
    private Button button;
    [SerializeField]
    private Image image;
    [SerializeField]
    private Animator animator;

    [SerializeField]
    public GamePlayManager.GameState.FleetInfo fleetData;

    public System.Action<int, int> OnClickFleet;

    // 위치 보간용
    private Vector2 currentPosition;
    private Vector2 targetPosition;
    private float interpolationSpeed = 10f; // 보간 속도

    private void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        if (button == null)
            button = GetComponent<Button>();
        if (animator == null)
            animator = GetComponent<Animator>();
        if (button != null)
        {
            button.onClick.AddListener(OnClicked);
        }
    }

    private void Update()
    {
        // 부드러운 위치 보간
        if (Vector2.Distance(currentPosition, targetPosition) > 0.1f)
        {
            currentPosition = Vector2.Lerp(currentPosition, targetPosition, Time.deltaTime * interpolationSpeed);
            rectTransform.anchoredPosition = currentPosition;
        }
    }

    public void SetData(GamePlayManager.GameState.FleetInfo data)
    {
        if (data.fleetType != fleetData.fleetType)
        {
            // 함대 스프라이트 설정 (OwnerId를 색상 인덱스로 사용)
            int colorIndex = data.ownerId % 4; // 0=Blue, 1=Green, 2=Orange, 3=Red
            image.sprite = ResourceManager.Instance.GetFleetSprite(colorIndex, data.fleetType.ToString());
        }
        fleetData = data;

        // 목표 위치 설정 (Update에서 보간 처리)
        targetPosition = new Vector2(data.position.X, data.position.Y);

        // 이동 중이면 이동 방향으로 회전
        if (data.state == 2) // 2 = move
        {
            UpdateRotation(currentPosition, targetPosition);
        }
    }

    /// <summary>
    /// 초기화 (UIFleet_Layer에서 호출)
    /// 생성 시 즉시 위치 설정 (보간 없이)
    /// </summary>
    public void Initialize(GamePlayManager.GameState.FleetInfo data)
    {
        fleetData = data;

        // 스프라이트 설정
        int colorIndex = data.ownerId % 4;
        image.sprite = ResourceManager.Instance.GetFleetSprite(colorIndex, data.fleetType.ToString());

        // 초기 위치 즉시 설정 (보간 없이)
        Vector2 initialPos = ConvertCVectorToRect(data.position);


        currentPosition = initialPos;
        targetPosition = initialPos;
        rectTransform.anchoredPosition = initialPos;
    }

    public void SetPosition(Vector2 position)
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = position;
        }
    }

    private UnityEngine.Vector2 ConvertCVectorToRect(CommonLib.Vector2 vector2)
    {
        const float uiScale = 40f; // 맵 스케일에 맞게 조정 필요
        return new UnityEngine.Vector2(vector2.X * uiScale, vector2.Y * uiScale);
    }

    /// <summary>
    /// 이동 방향으로 회전 (z축)
    /// </summary>
    public void UpdateRotation(Vector2 currentPos, Vector2 targetPos)
    {
        if (rectTransform != null)
        {
            // 방향 벡터 계산
            Vector2 direction = targetPos - currentPos;

            // 각도 계산 (라디안 -> 도)
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // z축 회전 적용 (2D UI는 z축 회전 사용)
            rectTransform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    /// <summary>
    /// 파괴 애니메이션 재생
    /// </summary>
    public void PlayDestroyAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Destroy");
        }

        // 애니메이션 후 오브젝트 파괴 (애니메이션 길이에 맞춰 조정)
        Destroy(gameObject, 1f);
    }

    private void OnClicked()
    {
        OnClickFleet?.Invoke((int)fleetData.fleetId, fleetData.ownerId);
    }
}
