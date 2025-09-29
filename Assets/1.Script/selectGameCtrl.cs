using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class selectGameCtrl : MonoBehaviour
{
    private const int mapCount = 2;
    [SerializeField] private int currentMapNumber;

    [System.Serializable]
    struct mapInfo
    {
        public RenderTexture texture;
        public string copyMapName; // 미니맵 씬의 이름
        public string realMapName; // 실제 게임할 씬의 이름
        public string displayMapName; // 플레이어에게 보이는 맵 이름
        [TextArea] public string displayExplain; // 플레이어에게 보여지는 맵 설명
    }
    [SerializeField] private mapInfo[] mapList = new mapInfo[mapCount];
    [SerializeField] private GameObject miniMapViewer;
    [SerializeField] private TextMeshProUGUI mapNameText, mapExplainText, mapNumberText;
    private musicCtrl musicManager;
    private soundCtrl soundManager;
    void Start()
    {
        // 맵 이름 및 설명 표시 텍스트에 접근
        mapNameText = GameObject.Find("mapNameText").GetComponent<TextMeshProUGUI>();
        mapExplainText = GameObject.Find("mapExplainText").GetComponent<TextMeshProUGUI>();
        mapNumberText = GameObject.Find("mapNumberText").GetComponent<TextMeshProUGUI>();

        // 뮤직 매니저와 사운드 매니저를 탐색하여 접근
        musicManager = GameObject.Find("musicManager").GetComponent<musicCtrl>();
        soundManager = GameObject.Find("soundManager").GetComponent<soundCtrl>();

        // 뮤직 매니저가 게임 음악을 재생중일 경우
        if( musicManager.isPlayingGame == true )
        {
            // 현재 재생중인 음악을 멈추고
            musicManager.musicPlayer.Stop();

            // 메인 화면 음악을 재생
            musicManager.PlayMainMusic();
            musicManager.isInGame = false;
        }

        // 기본적으로 첫번째 맵이 선택됨
        currentMapNumber = 0;

        // 기본적으로 첫번째 맵의 미리보기 씬을 비동기 로드함
        StartCoroutine(LoadScene(mapList[currentMapNumber].copyMapName));
        // 현재 비동기 로드된 씬의 카메라가 보고 있는 모습을 표시(미니맵 표시)
        miniMapViewer.GetComponent<RawImage>().texture = mapList[currentMapNumber].texture;
        mapNameText.text = mapList[currentMapNumber].displayMapName; // 맵 이름 표시
        mapExplainText.text = mapList[currentMapNumber].displayExplain; // 맵 설명 표시
        mapNumberText.text = currentMapNumber.ToString(); // 맵 번호 표시
    }

    void Update()
    {   
        if( Input.GetMouseButtonDown(0) )
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
            Debug.DrawRay(ray.origin, ray.direction, Color.yellow);

            if( hit.collider != null )
            {   
                // 다음 버튼을 누르면
                if( hit.collider.gameObject.name == "nextButton" )
                {
                    // 현재 비동기적 로드된 씬을 해제하고 다음 씬을 비동기적 로드한다.
                    SceneManager.UnloadSceneAsync(mapList[currentMapNumber].copyMapName);
                    currentMapNumber = currentMapNumber < mapList.Length-1 ? currentMapNumber + 1 : 0;
                    StartCoroutine(LoadScene(mapList[currentMapNumber].copyMapName));
                    soundManager.PlaySound("command"); // 버튼 눌림 사운드
                }

                // 이전 버튼을 누르면
                if( hit.collider.gameObject.name == "preButton" )
                {
                    // 현재 비동기적 로드된 씬을 해제하고 이전 씬을 비동기적 로드한다.
                    SceneManager.UnloadSceneAsync(mapList[currentMapNumber].copyMapName);
                    currentMapNumber = currentMapNumber > 0 ? currentMapNumber - 1 : mapList.Length-1;
                    StartCoroutine(LoadScene(mapList[currentMapNumber].copyMapName));
                    soundManager.PlaySound("command"); // 버튼 눌림 사운드
                }

                // 메인 메뉴 버튼을 누르면 메인 화면으로 간다
                if( hit.collider.gameObject.name == "menuButton" )
                {
                    soundManager.PlaySound("command"); // 버튼 눌림 사운드
                    SceneManager.LoadScene("mainScreen");
                }

                // 게임 시작 버튼을 누르면 현재 선택된 게임을 한다
                if( hit.collider.gameObject.name == "startButton" )
                {
                    soundManager.PlaySound("command"); // 버튼 눌림 사운드
                    SceneManager.LoadScene(mapList[currentMapNumber].realMapName);
                }
            }
            // 현재 비동기 로드된 씬의 카메라가 보고 있는 모습을 표시(미니맵 표시)
            miniMapViewer.GetComponent<RawImage>().texture = mapList[currentMapNumber].texture;
            mapNameText.text = mapList[currentMapNumber].displayMapName; // 맵 이름 표시
            mapExplainText.text = mapList[currentMapNumber].displayExplain; // 맵 설명 표시
            mapNumberText.text = currentMapNumber.ToString(); // 맵 번호 표시
        }
    }

    IEnumerator LoadScene(string sceneName)
    {
        // 비동기적으로 씬 로드 시작
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        // 씬 로드가 완료될 때까지 대기
        while (!asyncLoad.isDone) yield return null;
    }
}
