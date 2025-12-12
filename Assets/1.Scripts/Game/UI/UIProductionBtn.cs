using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 생산 버튼 UI - 함대 생산용
/// 쿨다운과 클릭 이벤트만 처리
/// </summary>
public class UIProductionBtn : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Button button;
    [SerializeField] private Image progressOverlay; // 생산 진행도 표시 (Filled)
    [SerializeField] private TMPro.TextMeshProUGUI progressText; // 남은 시간 표시 (옵션)

    [Header("Settings")]
    [SerializeField] private int fleetTypeId; // 생산할 함대 타입 ID
    [SerializeField] private KeyCode shortcutKey = KeyCode.None; // 단축키

    // 생산 진행 상태
    private bool isProducing = false;
    private float productionProgress = 0f; // 0.0 ~ 1.0
    private int remainingTicks = 0;

    // 쿨다운
    private float cooldownRemaining = 0f;
    private bool isOnCooldown = false;

    /// <summary>
    /// 버튼 클릭 이벤트 (fleetTypeId 전달)
    /// </summary>
    public System.Action<int> OnProductionRequested;

    private void Awake()
    {
        // 버튼 클릭 이벤트 연결
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }

        // 진행도 오버레이 초기화
        if (progressOverlay != null)
        {
            progressOverlay.fillAmount = 0f;
        }

        // 진행도 텍스트 초기화
        if (progressText != null)
        {
            progressText.text = "";
        }
    }

    private void Update()
    {
        // 단축키 입력 처리
        if (shortcutKey != KeyCode.None && Input.GetKeyDown(shortcutKey))
        {
            OnButtonClicked();
        }

        // 쿨다운 업데이트
        UpdateCooldown();

        // 생산 진행도 UI 업데이트
        UpdateProductionUI();
    }

    /// <summary>
    /// 버튼 클릭 처리
    /// </summary>
    private void OnButtonClicked()
    {
        // 쿨다운 중이거나 생산 중이면 무시
        if (isOnCooldown || isProducing)
        {
            return;
        }

        // 이벤트 발생
        OnProductionRequested?.Invoke(fleetTypeId);
    }

    /// <summary>
    /// 쿨다운 시작 (외부에서 호출)
    /// </summary>
    public void StartCooldown(float duration)
    {
        isOnCooldown = true;
        cooldownRemaining = duration;

        // 버튼 비활성화
        if (button != null)
        {
            button.interactable = false;
        }
    }

    /// <summary>
    /// 쿨다운 업데이트
    /// </summary>
    private void UpdateCooldown()
    {
        if (!isOnCooldown) return;

        cooldownRemaining -= Time.deltaTime;

        // 쿨다운 진행도 업데이트
        //if (cooldownOverlay != null)
        //{
        //    float progress = cooldownRemaining / cooldownRemaining; // 남은 시간 비율
        //    cooldownOverlay.fillAmount = Mathf.Clamp01(cooldownRemaining);
        //}

        // 쿨다운 완료
        if (cooldownRemaining <= 0f)
        {
            EndCooldown();
        }
    }

    /// <summary>
    /// 쿨다운 종료
    /// </summary>
    private void EndCooldown()
    {
        isOnCooldown = false;
        cooldownRemaining = 0f;

        // 버튼 활성화
        if (button != null)
        {
            button.interactable = true;
        }

        // 오버레이 숨김
        //if (cooldownOverlay != null)
        //{
        //    cooldownOverlay.fillAmount = 0f;
        //}
    }

    /// <summary>
    /// 쿨다운 강제 종료
    /// </summary>
    public void ResetCooldown()
    {
        EndCooldown();
    }

    /// <summary>
    /// 생산 진행도 UI 업데이트
    /// </summary>
    private void UpdateProductionUI()
    {
        if (!isProducing) return;

        // 진행도 오버레이 업데이트
        if (progressOverlay != null)
        {
            progressOverlay.fillAmount = productionProgress;
        }

        // 남은 시간 텍스트 업데이트 (틱 단위 -> 초 단위 변환: 1틱 = 50ms)
        if (progressText != null)
        {
            float remainingSeconds = remainingTicks * 0.05f;
            progressText.text = $"{remainingSeconds:F1}s";
        }
    }

    /// <summary>
    /// 생산 진행 상태 업데이트 (외부에서 호출)
    /// </summary>
    public void UpdateProduction(float progress, int remainingTicks)
    {
        isProducing = true;
        productionProgress = Mathf.Clamp01(progress);
        this.remainingTicks = remainingTicks;

        // 생산 중에는 버튼 비활성화
        if (button != null)
        {
            button.interactable = false;
        }
    }

    /// <summary>
    /// 생산 완료 (외부에서 호출)
    /// </summary>
    public void CompleteProduction()
    {
        isProducing = false;
        productionProgress = 0f;
        remainingTicks = 0;

        // 진행도 UI 초기화
        if (progressOverlay != null)
        {
            progressOverlay.fillAmount = 0f;
        }

        if (progressText != null)
        {
            progressText.text = "";
        }

        // 버튼 활성화
        if (button != null && !isOnCooldown)
        {
            button.interactable = true;
        }
    }

    public int FleetTypeId => fleetTypeId;
    public bool IsOnCooldown => isOnCooldown;
    public float CooldownRemaining => cooldownRemaining;
    public bool IsProducing => isProducing;
    public float ProductionProgress => productionProgress;
}
