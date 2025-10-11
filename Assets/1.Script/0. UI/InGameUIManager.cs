using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;



namespace UI
{
    /// <summary>
    /// 
    /// 
    /// </summary>
    public class InGameUIManager : MonoBehaviour
    {
        public static InGameUIManager Instance { get; private set; }


        [Header("Selected Fleet Info UI")]
        [SerializeField] private GameObject selectedFleetUI;
        [SerializeField] private Image selectedFleetImage;
        [SerializeField]
        private TextMeshProUGUI selectedFleetAttackText, selectedFleetDefenceText, selectedFleetSpeedText,
                                selectedFleetName, selectedFleetOwner, productingFleetInfo;



        public PlayerResourceUI playerResourceUI;


        // ---This must be a singleton...
        public playerGameCtrl playerGameController;

        [SerializeField] private GameObject endScreen, pauseScreen, victorySign, defeatSign, menuButton, endBlinder, pauseMenu, pauseResume;
        private RectTransform victoryClip, defeatClip;

        void Awake()
        {
            // --- 싱글톤 패턴 구현 ---
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // -----------------------

            // playerGameController가 인스펙터에서 할당되지 않았을 경우를 대비한 안전장치
            if (playerGameController == null)
            {

                playerGameController = GameObject.Find("gameManager(player)").GetComponent<playerGameCtrl>();
                if (playerGameController == null)
                {
                    Debug.Log("seems something wrong..");
                }
            }

            victoryClip = victorySign != null ? victorySign.GetComponent<RectTransform>() : null;
            defeatClip = defeatSign != null ? defeatSign.GetComponent<RectTransform>() : null;
        }

        void Start()
        {
            // 게임 시작 시 UI 초기화
            ResetEndScreen();
            if (pauseScreen != null) pauseScreen.SetActive(false);
            // lazy thing.. 
            StartCoroutine(newGameReset());
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // 스크립트가 비활성화될 때마다 호출됩니다.
        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 게임 시작 시 UI 초기화
            ResetEndScreen();
            if (pauseScreen != null) pauseScreen.SetActive(false);
            // lazy thing.. 
            StartCoroutine(newGameReset());
        }

        public void DisplayCurrentFleetStatus(GameObject selectedFleet, GameObject[] fleetSlot)
        {
            if (selectedFleet != null)
            {
                if (selectedFleetUI != null) selectedFleetUI.SetActive(true);
                if (selectedFleet.tag == "playerFleet")
                {
                    playerFleetCtrl player = selectedFleet.GetComponent<playerFleetCtrl>();
                    if (selectedFleetName != null) selectedFleetName.text = player.fleetName;

                    int i = 0; bool isSloted = false;
                    for (i = 0; i < fleetSlot.Length; i++)
                    {
                        if (fleetSlot[i] == selectedFleet) { isSloted = true; break; }
                    }

                    if (selectedFleetOwner != null)
                    {
                        if (isSloted)
                            selectedFleetOwner.text = "<color=#00FFFF>아군</color> <color=#FFFFFF>(" + (i + 1).ToString() + "번 함대)</color>";
                        else
                            selectedFleetOwner.text = "<color=#00FFFF>아군</color> <color=#FF0000> (번호미지정)</color>";
                    }

                    if (selectedFleetAttackText != null) selectedFleetAttackText.text = player.attack.ToString();
                    if (selectedFleetSpeedText != null) selectedFleetSpeedText.text = player.maxMoveSpeed.ToString();
                    if (selectedFleetDefenceText != null)
                    {
                        selectedFleetDefenceText.text = player.defence.ToString() + "/" + player.maxDefence.ToString();
                        selectedFleetDefenceText.color = (player.defence < player.maxDefence) ? Color.red : Color.white;
                    }
                    if (selectedFleetImage != null)
                    {
                        selectedFleetImage.sprite = selectedFleet.GetComponent<SpriteRenderer>().sprite;
                        RectTransform imageRect = selectedFleetImage.GetComponent<RectTransform>();
                        if (imageRect != null)
                        {
                            switch (player.fleetName)
                            {
                                case "정찰기": imageRect.sizeDelta = new Vector2(100, 100); break;
                                case "구축함": imageRect.sizeDelta = new Vector2(100, 110); break;
                                case "순양함": imageRect.sizeDelta = new Vector2(100, 150); break;
                                case "전투순양함": imageRect.sizeDelta = new Vector2(100, 150); break;
                            }
                        }
                    }
                }
                else if (selectedFleet.tag == "enemyFleet")
                {
                    enemyFleetCtrl enemy = selectedFleet.GetComponent<enemyFleetCtrl>();
                    if (selectedFleetName != null) selectedFleetName.text = enemy.fleetName;
                    if (selectedFleetOwner != null)
                    {
                        selectedFleetOwner.text = "적군";
                        selectedFleetOwner.color = Color.magenta;
                    }
                    if (selectedFleetAttackText != null) selectedFleetAttackText.text = enemy.attack.ToString();
                    if (selectedFleetSpeedText != null) selectedFleetSpeedText.text = enemy.maxMoveSpeed.ToString();
                    if (selectedFleetDefenceText != null)
                    {
                        selectedFleetDefenceText.text = enemy.defence.ToString() + "/" + enemy.maxDefence.ToString();
                        selectedFleetDefenceText.color = (enemy.defence < enemy.maxDefence) ? Color.red : Color.white;
                    }
                    if (selectedFleetImage != null)
                    {
                        selectedFleetImage.sprite = selectedFleet.GetComponent<SpriteRenderer>().sprite;
                        RectTransform imageRect = selectedFleetImage.GetComponent<RectTransform>();
                        if (imageRect != null)
                        {
                            switch (enemy.fleetName)
                            {
                                case "정찰기": imageRect.sizeDelta = new Vector2(100, 100); break;
                                case "구축함": imageRect.sizeDelta = new Vector2(100, 110); break;
                                case "순양함": imageRect.sizeDelta = new Vector2(100, 150); break;
                                case "전투순양함": imageRect.sizeDelta = new Vector2(100, 150); break;
                            }
                        }
                    }
                }
            }
            else
            {
                if (selectedFleetUI != null) selectedFleetUI.SetActive(false);
            }
        }

        public void DisplayResource(int mineral, int gas, int currentSupply, int maxSupply)
        {
            playerResourceUI?.UpdateResourceDisplay(mineral, gas, currentSupply, maxSupply);
        }

        public void UpdateProductionProgress(string fleetName, int remainingTime)
        {
            if (productingFleetInfo == null) return;
            productingFleetInfo.text = $"{fleetName} 생산중.. ({remainingTime})";
            productingFleetInfo.color = Color.green;
        }

        public void ShowProductionMessage(string message, Color color)
        {
            if (productingFleetInfo == null) return;
            productingFleetInfo.text = message;
            productingFleetInfo.color = color;
            StartCoroutine(ResetProductingFleetInfo());
        }

        public void ClearProductionInfo()
        {
            if (productingFleetInfo != null) productingFleetInfo.text = "";
        }

        // ====== playerGameCtrl에서 복사: 생산 정보 리셋 ======
        public IEnumerator ResetProductingFleetInfo()
        {
            yield return new WaitForSeconds(1);
            if (productingFleetInfo != null) productingFleetInfo.text = "";
        }

        IEnumerator newGameReset()
        {
            yield return new WaitForEndOfFrame();
            if (GameObject.Find("infoFleetUI") != null) GameObject.Find("infoFleetUI").SetActive(false);
        }

        public void ShowPauseScreen(bool show)
        {
            if (pauseScreen != null)
            {
                pauseScreen.SetActive(show);
            }
        }

        public void ToTitle()
        {
            SceneManager.LoadScene("MainScreen");
        }

        public void TriggerEndGame(int winnerId)
        {
            if (endBlinder != null) endBlinder.SetActive(true);



            switch (winnerId)
            {
                case 1: StartCoroutine(DisplayVictory()); break;
                case 2: StartCoroutine(DisplayDefeat()); break;
            }
        }

        public void ResetEndScreen()
        {
            if (endBlinder != null) endBlinder.SetActive(false);
            if (victorySign != null) victorySign.SetActive(false);
            if (defeatSign != null) defeatSign.SetActive(false);
            if (menuButton != null) menuButton.SetActive(false);

            if (victoryClip != null)
                victoryClip.sizeDelta = new Vector2(0, 500); // 초기 너비값(50)으로 설정
            if (defeatClip != null)
                defeatClip.sizeDelta = new Vector2(0, 500); // 초기 너비값(50)으로 설정
        }

        // ====== gameCtrl에서 복사: 승리/패배 연출 ======
        public IEnumerator DisplayVictory()
        {
            if (victorySign != null) victorySign.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            if (victoryClip != null)
            {
                for (float i = 50; i < 1500; i = 25 + i * 1.25f)
                {
                    victoryClip.sizeDelta = new Vector2(i, 500);
                    yield return new WaitForSeconds(0.025f);
                }
            }
            yield return new WaitForSeconds(0.5f);
            if (menuButton != null) menuButton.SetActive(true);
        }

        public IEnumerator DisplayDefeat()
        {
            if (defeatSign != null) defeatSign.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            if (defeatClip != null)
            {
                for (float i = 50; i < 1500; i = 25 + i * 1.25f)
                {
                    defeatClip.sizeDelta = new Vector2(i, 500);
                    yield return new WaitForSeconds(0.025f);
                }
            }
            yield return new WaitForSeconds(0.5f);
            if (menuButton != null) menuButton.SetActive(true);
        }

        // ====== fleetIconCtrl에서 참고용: UI 표시 로직 (주석으로 보존) ======
        /*
        void OnPointerEnter(PointerEventData eventData)
        {
            infoFleetUI.SetActive(true);
            infoFleetMineralText.text = myFleetData.mineralNeed.ToString();
            infoFleetGasText.text = myFleetData.gasNeed.ToString();
            infoFleetSupplyText.text = myFleetData.supplyNeed.ToString();
            infoFleetTimeText.text = myFleetData.timeNeed.ToString();
            infoFleetExplain.text = myFleetExplain;
            infoFleetName.text = myFleetData.fleetName;
            infoFleetImage.sprite = myfleetImage.GetComponent<Image>().sprite;
            infoFleetImage.GetComponent<RectTransform>().sizeDelta = myfleetImage.GetComponent<RectTransform>().sizeDelta;
        }

        void OnPointerExit(PointerEventData eventData)
        {
            infoFleetUI.SetActive(false);
        }
        */

        // ====== tutorialCtrl에서 참고용: UI 관련 필드 스냅샷 (주석으로 보존) ======
        /*
        [SerializeField] private GameObject blinder, textBox, highLightBox;
        [SerializeField] private TextMeshProUGUI boxMainText, boxSmallText;
        [SerializeField] private TextMeshPro pointText;
        */
    }
}
