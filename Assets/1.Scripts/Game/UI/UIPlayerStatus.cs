using UnityEngine;
using TMPro;

/// <summary>
/// 플레이어 상태 표시 UI
/// 이름, 자원(미네랄, 가스, 보급품), 게임 진행 시간 표시
/// </summary>
public class UIPlayerStatus : MonoBehaviour
{
    [Header("Player Info")]
    [SerializeField] private TextMeshProUGUI playerNameText;

    [Header("Game Time")]
    [SerializeField] private TextMeshProUGUI gameTimeText;

    /// <summary>
    /// 플레이어 이름 설정
    /// </summary>
    public void SetPlayerName(string name)
    {
        if (playerNameText != null)
        {
            playerNameText.text = name;
        }
    }

    /// <summary>
    /// 게임 진행 시간 업데이트 (초 단위)
    /// </summary>
    public void UpdateGameTime(float timeInSeconds)
    {
        if (gameTimeText != null)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60);
            gameTimeText.text = $"{minutes:00}:{seconds:00}";
        }
    }
}
