using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 함대 정보 표시 UI
/// 아이콘, 이름, 공격력, 이동속도, 체력, 피아 구분 표시
/// </summary>
public class UIFleetInfo : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image fleetIcon;
    [SerializeField] private TextMeshProUGUI fleetNameText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI hpText; // HP: 현재/최대
    [SerializeField] private TextMeshProUGUI teamText; // 피아 구분: 아군/적

    private GamePlayManager.GameState.FleetInfo currentFleetData;

    /// <summary>
    /// 함대 정보 설정
    /// </summary>
    public void SetFleetInfo(GamePlayManager.GameState.FleetInfo fleetInfo, bool isAlly)
    {
        currentFleetData = fleetInfo;

        // 아이콘 설정 (ResourceManager에서 가져오기)
        if (fleetIcon != null)
        {
            int colorIndex = fleetInfo.ownerId % 4;
            fleetIcon.sprite = ResourceManager.Instance.GetFleetSprite(colorIndex, fleetInfo.fleetType.ToString());
            fleetIcon.SetNativeSize(); // 스프라이트 크기에 맞게 RectTransform 사이즈 조정
        }

        // 함대 이름 설정
        if (fleetNameText != null)
        {
            fleetNameText.text = $"Fleet Type {fleetInfo.fleetType}";
        }

        // 공격력 설정 (임시 - 실제로는 FleetData에서 가져와야 함)
        if (attackText != null)
        {
            attackText.text = $"ATK: {GetFleetAttack(fleetInfo.fleetType)}";
        }

        // 이동속도 설정 (임시 - 실제로는 FleetData에서 가져와야 함)
        if (speedText != null)
        {
            speedText.text = $"SPD: {GetFleetSpeed(fleetInfo.fleetType)}";
        }

        // 체력 설정
        UpdateHP(fleetInfo.HP, fleetInfo.maxHP);

        // 피아 구분 텍스트 설정
        UpdateTeamText(isAlly);
    }

    /// <summary>
    /// HP 업데이트 (텍스트로만 표시)
    /// </summary>
    public void UpdateHP(float currentHP, float maxHP)
    {
        if (hpText != null)
        {
            hpText.text = $"HP: {currentHP:F0}/{maxHP:F0}";
        }
    }

    /// <summary>
    /// 피아 구분 텍스트 업데이트
    /// </summary>
    public void UpdateTeamText(bool isAlly)
    {
        if (teamText != null)
        {
            teamText.text = isAlly ? "아군" : "적";
            teamText.color = isAlly ? Color.blue : Color.red;
        }
    }

    /// <summary>
    /// UI 표시/숨김
    /// </summary>
    public void Show(bool show)
    {
        gameObject.SetActive(show);
    }

    /// <summary>
    /// 함대 공격력 가져오기 (임시 - 실제로는 테이블 데이터에서)
    /// </summary>
    private int GetFleetAttack(int fleetType)
    {
        // TODO: FleetLibrary 또는 테이블 데이터에서 가져오기
        return fleetType * 10; // 임시값
    }

    /// <summary>
    /// 함대 이동속도 가져오기 (임시 - 실제로는 테이블 데이터에서)
    /// </summary>
    private float GetFleetSpeed(int fleetType)
    {
        // TODO: FleetLibrary 또는 테이블 데이터에서 가져오기
        return 5f + fleetType * 0.5f; // 임시값
    }

    /// <summary>
    /// 현재 함대 데이터
    /// </summary>
    public GamePlayManager.GameState.FleetInfo CurrentFleetData => currentFleetData;
}
