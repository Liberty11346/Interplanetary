using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using System.Linq; // OrderBy를 사용하기 위해 추가

public class selectGameCtrl : MonoBehaviour
{
    [SerializeField] private int currentMapNumber;
    [SerializeField] private Button nextButton, preButton, menuButton, startButton;

    // 인스펙터에서 할당받는 대신 코드로 불러옵니다.
    private MapData[] mapList;

    [SerializeField] private GameObject miniMapViewer;
    [SerializeField] private TextMeshProUGUI mapNameText, mapExplainText, mapNumberText;
    private musicCtrl musicManager;
    private soundCtrl soundManager;
    void Start()
    {
        // Resources/MapData 폴더에서 모든 MapData 에셋을 불러옵니다.
        mapList = Resources.LoadAll<MapData>("MapData");

        // 불러온 맵들을 이름순으로 정렬하여 항상 일관된 순서를 유지합니다.
        if (mapList.Length > 0)
        {
            mapList = mapList.OrderBy(map => map.name).ToArray();
        }
        else
        {
            Debug.LogError("Resources/MapData 폴더에 MapData 에셋이 없습니다. 해당 경로에 에셋을 추가해주세요.");
        }

        // 맵 이름 및 설명 표시 텍스트에 접근
        mapNameText = GameObject.Find("mapNameText").GetComponent<TextMeshProUGUI>();
        mapExplainText = GameObject.Find("mapExplainText").GetComponent<TextMeshProUGUI>();
        mapNumberText = GameObject.Find("mapNumberText").GetComponent<TextMeshProUGUI>();

        // 뮤직 매니저와 사운드 매니저를 탐색하여 접근
        musicManager = GameObject.Find("musicManager").GetComponent<musicCtrl>();
        soundManager = GameObject.Find("soundManager").GetComponent<soundCtrl>();

        // 뮤직 매니저가 게임 음악을 재생중일 경우
        if (musicManager.isPlayingGame == true)
        {
            // 현재 재생중인 음악을 멈추고
            musicManager.musicPlayer.Stop();

            // 메인 화면 음악을 재생
            musicManager.PlayMainMusic();
            musicManager.isInGame = false;
        }

        // 기본적으로 첫번째 맵이 선택됨
        currentMapNumber = 0;

        // 첫 번째 맵의 UI를 표시
        UpdateMapUI();

        // 버튼 클릭 이벤트 연결
        nextButton = GameObject.Find("nextButton")?.GetComponent<Button>();
        preButton = GameObject.Find("preButton")?.GetComponent<Button>();
        menuButton = GameObject.Find("menuButton")?.GetComponent<Button>();
        startButton = GameObject.Find("startButton")?.GetComponent<Button>();

        if (nextButton != null) nextButton.onClick.AddListener(() => ChangeMap(1));
        if (preButton != null) preButton.onClick.AddListener(() => ChangeMap(-1));
        if (menuButton != null) menuButton.onClick.AddListener(() => { soundManager.PlaySound("command"); SceneManager.LoadScene("mainScreen"); });
        if (startButton != null) startButton.onClick.AddListener(() => { soundManager.PlaySound("command"); SceneManager.LoadScene(mapList[currentMapNumber].realMapName); });
    }

    private void ChangeMap(int direction)
    {
        // 맵 번호를 변경합니다.
        currentMapNumber += direction;
        if (currentMapNumber < 0)
        {
            currentMapNumber = mapList.Length - 1;
        }
        else if (currentMapNumber >= mapList.Length)
        {
            currentMapNumber = 0;
        }

        // 새로운 맵의 UI를 업데이트합니다.
        UpdateMapUI();
        soundManager.PlaySound("command"); // 버튼 눌림 사운드
    }

    private void UpdateMapUI()
    {
        if (mapList == null || mapList.Length == 0)
        {
            Debug.LogError("Map List가 비어있습니다. MapData 에셋을 할당해주세요.");
            return;
        }

        // MapData에서 정보를 가져와 UI를 업데이트합니다.
        MapData currentMap = mapList[currentMapNumber];
        miniMapViewer.GetComponent<RawImage>().texture = currentMap.mapPreviewImage;
        mapNameText.text = currentMap.displayMapName;
        mapExplainText.text = currentMap.displayExplain;
        mapNumberText.text = (currentMapNumber + 1).ToString(); // 0부터 시작하므로 +1
    }
}
