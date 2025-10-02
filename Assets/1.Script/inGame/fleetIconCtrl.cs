using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class fleetIconCtrl : MonoBehaviour
{
    private GameObject infoFleetUI;
    private Image infoFleetImage;
    private TextMeshProUGUI infoFleetName, infoFleetExplain, infoFleetMineralText, infoFleetGasText, infoFleetSupplyText, infoFleetTimeText;
    public GameObject myfleetImage, myFleet;
    playerFleetCtrl myFleetData;
    [TextArea] public string myFleetExplain = " ";
    // Start is called before the first frame update
    void Start()
    {
        // 함선 자원 정보를 표시할 컴포넌트와 오브젝트를 가져온다.
        infoFleetUI = GameObject.Find("infoFleetUI");
        infoFleetMineralText = GameObject.Find("infoFleetMineralText").GetComponent<TextMeshProUGUI>();
        infoFleetGasText = GameObject.Find("infoFleetGasText").GetComponent<TextMeshProUGUI>();
        infoFleetSupplyText = GameObject.Find("infoFleetSupplyText").GetComponent<TextMeshProUGUI>();
        infoFleetTimeText = GameObject.Find("infoFleetTimeText").GetComponent<TextMeshProUGUI>();

        // 함선 이름과 설명을 표시할 컴포넌트를 가져온다.
        infoFleetImage = GameObject.Find("infoFleetImage").GetComponent<Image>();
        infoFleetName = GameObject.Find("infoFleetName").GetComponent<TextMeshProUGUI>();
        infoFleetExplain = GameObject.Find("infoFleetExplain").GetComponent<TextMeshProUGUI>();

        // 표시할 함선의 정보를 가져온다.
        myFleetData = myFleet.GetComponent<playerFleetCtrl>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 마우스를 올리면 화면 좌측 하단에 함선 정보를 표시 (UI 버튼과 함께 동작)
    void OnMouseEnter()
    {
        // 함선 능력치 표시
        infoFleetUI.SetActive(true);
        infoFleetMineralText.text = myFleetData.mineralNeed.ToString();
        infoFleetGasText.text = myFleetData.gasNeed.ToString();
        infoFleetSupplyText.text = myFleetData.supplyNeed.ToString();
        infoFleetTimeText.text = myFleetData.timeNeed.ToString();
        infoFleetGasText.text = myFleetData.gasNeed.ToString();

        // 함선 이름과 설명 표시
        infoFleetExplain.text = myFleetExplain;
        infoFleetName.text = myFleetData.fleetName;
        infoFleetImage.sprite = myfleetImage.GetComponent<Image>().sprite;
        infoFleetImage.GetComponent<RectTransform>().sizeDelta = myfleetImage.GetComponent<RectTransform>().sizeDelta;
    }

    // 마우스를 치우면 아무것도 안보이게
    void OnMouseExit()
    {
        infoFleetUI.SetActive(false);
    }
}
