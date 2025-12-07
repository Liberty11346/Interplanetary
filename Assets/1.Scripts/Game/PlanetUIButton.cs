using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using CommonLib;

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
    public Button produceFleetButton;
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
    private int _myPlayerId = -1;

    private void Awake()
    {
        if (planetButton == null)
            planetButton = GetComponent<Button>();
    }

    private void Start()
    {
        if (planetButton != null)
        {
            planetButton.onClick.AddListener(() => OnPlanetButtonClicked());
        }

        if (produceFleetButton != null)
        {
            produceFleetButton.onClick.AddListener(() => OnProduceFleetClicked());
            produceFleetButton.gameObject.SetActive(false); // 초기에는 비활성화
        }

        // GameManager 이벤트 구독: 행성 선택 상태 변화 감지
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlanetSelected += OnPlanetSelectionChanged;
            _myPlayerId = GameManager.Instance.MyPlayerId;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlanetSelected -= OnPlanetSelectionChanged;
        }

        if (produceFleetButton != null)
        {
            produceFleetButton.onClick.RemoveAllListeners();
        }
    }

    private void OnPlanetButtonClicked()
    {
        Debug.Log($"Planet {planetId} clicked!");
        GameManager.Instance.SelectPlanet(planetId);
    }

    private void OnProduceFleetClicked()
    {
        Debug.Log($"Produce fleet on planet {planetId}!");
        // 함대 생산 요청 (플릿 타입 0으로 통일)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CommandFleetSpawn(0);
        }
    }

    private void OnPlanetSelectionChanged(int selectedPlanetId)
    {
        // 이 행성이 선택되었는지 확인
        bool isThisPlanetSelected = (selectedPlanetId == planetId);
        _isSelected = isThisPlanetSelected;

        // 선택된 행성이고, 내 소유이며, 모성인 경우에만 함대 생산 버튼 표시
        if (produceFleetButton != null)
        {
            bool shouldShowButton = isThisPlanetSelected && _ownerId == _myPlayerId && IsHomePlanet;
            produceFleetButton.gameObject.SetActive(shouldShowButton);
        }

        Debug.Log($"Planet {planetId} selection changed: selected={_isSelected}, owner={_ownerId}, myId={_myPlayerId}, isHome={IsHomePlanet}");
    }

    public bool IsHomePlanet { get; private set; }

    /// <summary>
    /// 서버로부터 받은 행성 데이터로 설정
    /// </summary>
    public void SetPlanetData(PlanetData planetData)
    {
        planetId = planetData.PlanetId;
        _ownerId = planetData.OwnerId;
        _minerals = (int)planetData.Minerals;
        _gas = (int)planetData.Gas;
        _supply = planetData.Supply;
        //IsHomePlanet = planetData.IsHomePlanet;

        // MyPlayerId 업데이트 (게임 시작 후 처음 호출될 때 설정)
        if (_myPlayerId == -1 && GameManager.Instance != null)
        {
            _myPlayerId = GameManager.Instance.MyPlayerId;
        }

        Debug.Log($"[PlanetUIButton] Setting planet data - ID: {planetId}, Name: {planetData.Name}, IsHomePlanet: {IsHomePlanet}, planetNameText: {(planetNameText != null ? "assigned" : "NULL")}");

        if (planetNameText != null)
        {
            planetNameText.text = planetData.Name;
            Debug.Log($"[PlanetUIButton] Planet name set to: {planetNameText.text}");
        }
        else
        {
            Debug.LogWarning($"[PlanetUIButton] planetNameText is NULL for planet {planetId}!");
        }

        if (conquestProgressText != null)
            conquestProgressText.gameObject.SetActive(false);

        UpdateVisuals();
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
    public UnityEngine.Vector3 GetFleetSpawnPosition()
    {
        return transform.position + UnityEngine.Vector3.right * 100f; // UI에서 오른쪽으로 100픽셀
    }
}
