using CommonLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPlanet : MonoBehaviour
{
    [SerializeField]
    RectTransform rectTransform;
    [SerializeField]
    private Button button;
    [SerializeField]
    private Image image;
    [SerializeField]
    private TextMeshProUGUI progress;
    [SerializeField]
    private TextMeshProUGUI txtName;
    [SerializeField]
    private TextMeshProUGUI txtGas;
    [SerializeField]
    private TextMeshProUGUI txtMinerals;
    [SerializeField]
    private TextMeshProUGUI txtSup;
    [SerializeField]
    private GameObject goGas;
    [SerializeField]
    private GameObject goMinerals;
    [SerializeField]
    private GameObject goSup;
    [SerializeField]
    private Image conquestProgressBar; // 점령 진행도 바

    [SerializeField]
    public PlanetData planetData;

    public System.Action<int, int> OnClickPlanet;

    private void Awake()
    {
        if(rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        if (button == null)
            button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnClicked);
        }
    }

    /// <summary>
    /// 초기화 (UIPlanet_Layer에서 호출)
    /// </summary>
    public void Initialize(PlanetData data)
    {
        planetData = data;
        txtName.text = data.Name;
        txtGas.text = data.Gas.ToString("0");
        txtMinerals.text = data.Minerals.ToString("0");
        txtSup.text = data.Supply.ToString();
        image.sprite = ResourceManager.Instance.GetPlanetSprite(data.PlanetId.ToString());

        // 행성은 위치가 고정
        rectTransform.anchoredPosition = ConvertCVectorToRect(data.Position);
    }

    private UnityEngine.Vector2 ConvertCVectorToRect(CommonLib.Vector2 vector2)
    {
        const float uiScale = 40f; // 맵 스케일에 맞게 조정 필요
        return new UnityEngine.Vector2(vector2.X * uiScale, vector2.Y * uiScale);
    }

    /// <summary>
    /// 데이터 업데이트 (자원 등)
    /// </summary>
    public void SetData(PlanetData data)
    {
        planetData = data;
        txtName.text = data.Name;
        txtGas.text = data.Gas.ToString("0");
        txtMinerals.text = data.Minerals.ToString("0");
        txtSup.text = data.Supply.ToString();
        
        // 스프라이트나 위치는 변경하지 않음 (초기화 시에만)
    }

    /// <summary>
    /// 점령 진행도 업데이트
    /// </summary>
    public void UpdateConquestProgress(float normalizedProgress)
    {
        if (conquestProgressBar != null)
        {
            conquestProgressBar.fillAmount = normalizedProgress;
            conquestProgressBar.gameObject.SetActive(normalizedProgress > 0f && normalizedProgress < 1f);
        }
    }

    private void OnClicked()
    {
        OnClickPlanet?.Invoke(planetData.PlanetId, planetData.OwnerId);
    }
}
