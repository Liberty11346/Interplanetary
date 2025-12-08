using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 생산 버튼들을 관리하는 컨테이너
/// 모든 UIProductionBtn의 이벤트를 수집하고 상위로 전달
/// </summary>
public class UIProductionButtons : MonoBehaviour
{
    [Header("Production Buttons")]
    [SerializeField] private UIProductionBtn[] productionButtons;

    /// <summary>
    /// 생산 요청 이벤트 (fleetTypeId 전달)
    /// </summary>
    public System.Action<int> OnProductionRequested;

    private Dictionary<int, UIProductionBtn> buttonsByFleetType = new Dictionary<int, UIProductionBtn>();

    private void Awake()
    {
        // 자동으로 자식 UIProductionBtn 찾기
        if (productionButtons == null || productionButtons.Length == 0)
        {
            productionButtons = GetComponentsInChildren<UIProductionBtn>();
        }

        // 각 버튼의 이벤트 구독
        foreach (var btn in productionButtons)
        {
            if (btn != null)
            {
                btn.OnProductionRequested += HandleProductionRequested;
                buttonsByFleetType[btn.FleetTypeId] = btn;
            }
        }

        Debug.Log($"[UIProductionButtons] Initialized with {productionButtons.Length} buttons");
    }

    /// <summary>
    /// 버튼 클릭 이벤트 핸들러
    /// </summary>
    private void HandleProductionRequested(int fleetTypeId)
    {
        Debug.Log($"[UIProductionButtons] Production requested for fleet type {fleetTypeId}");
        
        // 상위로 이벤트 전달
        OnProductionRequested?.Invoke(fleetTypeId);
    }

    /// <summary>
    /// 특정 함대 타입의 쿨다운 시작
    /// </summary>
    public void StartCooldown(int fleetTypeId, float duration)
    {
        if (buttonsByFleetType.TryGetValue(fleetTypeId, out UIProductionBtn btn))
        {
            btn.StartCooldown(duration);
            Debug.Log($"[UIProductionButtons] Started cooldown for fleet type {fleetTypeId}: {duration}s");
        }
        else
        {
            Debug.LogWarning($"[UIProductionButtons] Button for fleet type {fleetTypeId} not found");
        }
    }

    /// <summary>
    /// 모든 버튼의 쿨다운 초기화
    /// </summary>
    public void ResetAllCooldowns()
    {
        foreach (var btn in productionButtons)
        {
            if (btn != null)
            {
                btn.ResetCooldown();
            }
        }
        Debug.Log("[UIProductionButtons] All cooldowns reset");
    }

    /// <summary>
    /// 특정 함대 타입의 쿨다운 초기화
    /// </summary>
    public void ResetCooldown(int fleetTypeId)
    {
        if (buttonsByFleetType.TryGetValue(fleetTypeId, out UIProductionBtn btn))
        {
            btn.ResetCooldown();
            Debug.Log($"[UIProductionButtons] Reset cooldown for fleet type {fleetTypeId}");
        }
    }

    /// <summary>
    /// 특정 함대 타입의 버튼 가져오기
    /// </summary>
    public UIProductionBtn GetButton(int fleetTypeId)
    {
        buttonsByFleetType.TryGetValue(fleetTypeId, out UIProductionBtn btn);
        return btn;
    }

    /// <summary>
    /// 특정 함대 타입이 쿨다운 중인지 확인
    /// </summary>
    public bool IsOnCooldown(int fleetTypeId)
    {
        if (buttonsByFleetType.TryGetValue(fleetTypeId, out UIProductionBtn btn))
        {
            return btn.IsOnCooldown;
        }
        return false;
    }

    /// <summary>
    /// 특정 함대 타입의 남은 쿨다운 시간
    /// </summary>
    public float GetCooldownRemaining(int fleetTypeId)
    {
        if (buttonsByFleetType.TryGetValue(fleetTypeId, out UIProductionBtn btn))
        {
            return btn.CooldownRemaining;
        }
        return 0f;
    }

    /// <summary>
    /// 모든 버튼 활성화/비활성화
    /// </summary>
    public void SetAllInteractable(bool interactable)
    {
        foreach (var btn in productionButtons)
        {
            if (btn != null && btn.TryGetComponent<UnityEngine.UI.Button>(out var button))
            {
                button.interactable = interactable;
            }
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        foreach (var btn in productionButtons)
        {
            if (btn != null)
            {
                btn.OnProductionRequested -= HandleProductionRequested;
            }
        }
    }
}
