using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using CommonLib;
using UnityEngine.Events;

public class FleetUIButton : MonoBehaviour
{
    [Header("Visual Settings")]
    public Sprite player1Sprite;
    public Sprite player2Sprite;
    public float moveSpeed = 200f; // UI 픽셀 단위

    [Header("UI Components")]
    public Button fleetButton;
    public Image fleetImage;
    public TextMeshProUGUI fleetIdText;
    public GameObject trailEffect;

    [Header("Events")]
    public UnityEvent<int> OnFleetClicked;

    private int _fleetId;
    private int _ownerId;
    private int _currentPlanetId;
    private bool _isMoving = false;
    private bool _isSelected = false;
    private RectTransform _rectTransform;

    public bool IsSelected => _isSelected;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        if (fleetButton != null)
        {
            fleetButton.onClick.AddListener(() => OnFleetButtonClicked());
        }
    }

    private void OnFleetButtonClicked()
    {
        Debug.Log($"Fleet {_fleetId} clicked!");
        // 직접 선택 상태로 설정
        SetSelected(true);
        // 이벤트 발생
        OnFleetClicked?.Invoke(_fleetId);
        Debug.Log($"Fleet {_fleetId} selection state: {_isSelected}");
    }

    public void Initialize(FleetSpawnData fleetData)
    {
        _fleetId = fleetData.FleetId;
        _ownerId = fleetData.OwnerId;
        _currentPlanetId = fleetData.PlanetId;

        if (fleetIdText != null)
            fleetIdText.text = $"F{_fleetId}";

        if (fleetImage != null)
        {
            fleetImage.sprite = _ownerId == 1 ? player1Sprite : player2Sprite;
        }

        // 행성 위치로 이동
        PlanetUIButton planet = FindPlanetById(_currentPlanetId);
        if (planet != null)
        {
            _rectTransform.position = planet.GetFleetSpawnPosition();
        }

        // 트레일 효과 초기화
        if (trailEffect != null)
            trailEffect.SetActive(false);
    }

    public void StartMovement(FleetMoveData moveData)
    {
        if (_isMoving) return;

        PlanetUIButton targetPlanet = FindPlanetById(moveData.ToPlanetId);
        if (targetPlanet != null)
        {
            StartCoroutine(MoveToPosition(targetPlanet.transform.position));
            _currentPlanetId = moveData.ToPlanetId;
        }
    }

    private IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        _isMoving = true;

        if (trailEffect != null)
            trailEffect.SetActive(true);

        Vector3 startPosition = _rectTransform.position;
        float distance = Vector3.Distance(startPosition, targetPosition);
        float duration = distance / moveSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            _rectTransform.position = Vector3.Lerp(startPosition, targetPosition, progress);

            yield return null;
        }

        _rectTransform.position = targetPosition;

        if (trailEffect != null)
            trailEffect.SetActive(false);

        _isMoving = false;
    }

    public void UpdatePosition()
    {
        PlanetUIButton planet = FindPlanetById(_currentPlanetId);
        if (planet != null && !_isMoving)
        {
            _rectTransform.position = planet.GetFleetSpawnPosition();
        }
    }

    private PlanetUIButton FindPlanetById(int planetId)
    {
        PlanetUIButton[] planets = FindObjectsByType<PlanetUIButton>(FindObjectsSortMode.None);
        foreach (var planet in planets)
        {
            if (planet.PlanetId == planetId)
                return planet;
        }
        return null;
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // 선택 상태에 따른 시각적 변화 적용
        if (fleetImage != null)
        {
            // 선택된 경우 더 밝게 표시
            fleetImage.color = _isSelected ? Color.white * 1.5f : Color.white;
        }
    }

    // Public getters
    public int FleetId => _fleetId;
    public int OwnerId => _ownerId;
    public int CurrentPlanetId => _currentPlanetId;
    public bool IsMoving => _isMoving;

    private void OnDestroy()
    {
        if (fleetButton != null)
        {
            fleetButton.onClick.RemoveAllListeners();
        }
    }
}
