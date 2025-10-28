using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
/// <summary>
/// 행성 UI 버튼 클래스
/// 행성의 자원은 생성 시점에 고정되며, 업데이트되지 않습니다.
/// </summary>
public class PlanetUIButton : MonoBehaviour
{
    [Header("Visual Settings")]
    public Color neutralColor = Color.white;
    public Color player1Color = Color.cyan;
    public Color player2Color = Color.magenta;

    [Header("UI Components")]
    public Button planetButton;
    public Image planetImage;
    public TextMeshProUGUI planetNameText;
    public TextMeshProUGUI mineralsText;
    public TextMeshProUGUI gasText;
    public TextMeshProUGUI supplyText;
    public TextMeshProUGUI conquestProgressText;

    [Header("Planet Settings")]
    public int planetId = 1;

    [Header("Events")]
    public UnityEvent<int> OnPlanetClicked;

    private int _ownerId = -1;
    private int _minerals = 0;
    private int _gas = 0;
    private int _supply = 0;
    private float _conquestProgress = 0f;
    private bool _isSelected = false;

    private void Start()
    {
        Initialize();
    }

    public void Initialize(int minerals = -1, int gas = -1, int supply = -1, string planetName = null)
    {
        if (planetButton != null)
        {
            planetButton.onClick.AddListener(OnPlanetButtonClicked);
        }

        if (planetNameText != null)
            planetNameText.text = !string.IsNullOrEmpty(planetName) ? planetName : $"Planet {planetId}";

        if (conquestProgressText != null)
            conquestProgressText.gameObject.SetActive(false); // 기본적으로 비활성화

        // 자원값 설정 (매개변수로 받거나 기본값 사용)
        _minerals = minerals >= 0 ? minerals : Random.Range(5, 15);
        _gas = gas >= 0 ? gas : Random.Range(3, 10);
        _supply = supply >= 0 ? supply : Random.Range(1, 5);

        UpdateVisuals();
    }

    private void OnPlanetButtonClicked()
    {
        Debug.Log($"Planet {planetId} clicked!");
        // 직접 선택 상태로 설정
        SetSelected(true);
        // 이벤트 발생
        OnPlanetClicked?.Invoke(planetId);
        Debug.Log($"Planet {planetId} selection state: {_isSelected}");
    }

    public void UpdateOwnership(int newOwnerId)
    {
        _ownerId = newOwnerId;
        UpdateVisuals();
    }

    public void UpdateConquestProgress(float progress)
    {
        _conquestProgress = progress;
        if (conquestProgressText != null)
        {
            conquestProgressText.gameObject.SetActive(progress > 0 && progress < 100);
        }
        UpdateVisuals();
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (planetNameText != null)
        {
            Color targetColor = neutralColor;

            switch (_ownerId)
            {
                case 1:
                    targetColor = player1Color;
                    break;
                case 2:
                    targetColor = player2Color;
                    break;
                default:
                    targetColor = neutralColor;
                    break;
            }

            planetNameText.color = targetColor;
        }

        if (mineralsText != null)
        {
            mineralsText.text = _minerals.ToString();
        }
        if (gasText != null)
        {
            gasText.text = _gas.ToString();
        }
        if (supplyText != null)
        {
            supplyText.text = _supply.ToString();
        }

        if (conquestProgressText != null && conquestProgressText.gameObject.activeSelf)
        {
            conquestProgressText.text = _conquestProgress.ToString("F0") + "%";
        }
    }

    // Public getters
    public int PlanetId => planetId;
    public int OwnerId => _ownerId;
    public int Minerals => _minerals;
    public int Gas => _gas;
    public int Supply => _supply;
    public bool IsSelected => _isSelected;

    // 함대 스폰 위치 (UI 좌표계)
    public Vector3 GetFleetSpawnPosition()
    {
        return transform.position + Vector3.right * 100f; // UI에서 오른쪽으로 100픽셀
    }
}
