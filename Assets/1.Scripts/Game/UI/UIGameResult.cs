using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 게임 결과 UI
/// 승리/패배 결과 표시 및 확인 버튼
/// </summary>
public class UIGameResult : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject contentPanel;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button confirmButton;

    // 확인 버튼 클릭 이벤트
    public event Action OnConfirmClicked;

    private void Awake()
    {
        // 초기에는 숨김
        if (contentPanel != null)
        {
            contentPanel.SetActive(false);
        }
    }

    private void Start()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(() => OnConfirmClicked?.Invoke());
        }
    }

    /// <summary>
    /// 결과 표시
    /// </summary>
    public void ShowResult(bool isWinner, string message = "")
    {
        if (contentPanel != null)
        {
            contentPanel.SetActive(true);
        }

        if (resultText != null)
        {
            resultText.text = isWinner ? "VICTORY!" : "DEFEAT";
            resultText.color = isWinner ? Color.green : Color.red;

            if (!string.IsNullOrEmpty(message))
            {
                resultText.text += $"\n<size=70%>{message}</size>";
            }
        }
    }

    /// <summary>
    /// 숨기기
    /// </summary>
    public void Hide()
    {
        if (contentPanel != null)
        {
            contentPanel.SetActive(false);
        }
    }
}
