using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using CommonLib;
using UnityEngine.Events;
using Vector3 = UnityEngine.Vector3;


public class FleetData
{
    public int FleetId;
    public int OwnerId;
    public int FleetType;
    public int CurrentPlanetId;
}
/// <summary>
/// 데이터를 받아서 초기화하는 함대 클래스
/// </summary>
public class FleetUIButton : MonoBehaviour
{
    [Header("Visual Settings")]
    public FleetData fleetData;
    public float moveSpeed = 200f; // UI 픽셀 단위
    public Sprite player1Sprite;
    public Sprite player2Sprite;

    [Header("UI Components")]
    public Button fleetButton;
    public Image fleetImage;
    public TextMeshProUGUI fleetIdText;

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
        GameManager.Instance.SelectFleet(_fleetId);
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
