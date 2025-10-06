using System.Collections;
using System.Data;
using UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class playerGameCtrl : MonoBehaviour
{
    public static playerGameCtrl Instance;
    public GameObject selectedFleet;
    public GameObject[] fleetSlot = new GameObject[5];
    public GameObject[] playerPlanet;
    public GameObject playerScout, playerDestroyer, playerCruiser, playerBattleCruiser, playerBasePlanet; // UI 관련 요소 주석 처리
    public int playerMaxSupply, playerCurrentSupply;
    private int playerMineral, playerGas;
    public GameObject productingFleet;
    public playerFleetCtrl player; // playerFleetCtrl은 여전히 필요
    private enemyFleetCtrl enemy;
    public planetCtrl playerBase;
    private gameCtrl gameManager; // UI 관련 요소 주석 처리
    private soundCtrl soundManager;
    private PlayerInputManager playerInputManager;
    // [SerializeField] private Button fleetIconQButton, fleetIconWButton, fleetIconEButton, fleetIconRButton;

    private void Awake()
    {
        if( Instance != null && Instance != this )
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    private void Start()
    {
        // 각 매니저 클래스의 인스턴스에 접근
        gameManager = gameCtrl.Instance; // 게임 매니저
        soundManager = soundCtrl.Instance; // 사운드 매니저
        playerInputManager = PlayerInputManager.Instance; // 플레이어 입력 매니저

        // 플레이어 입력 매니저 구독
        playerInputManager.OnFleetSaveSlot += FleetSaveSlot; // 함선 부대지정
        playerInputManager.OnFleetSelectedByKeyboard += FleetSelectNumber; // 함선 선택 (키보드)
        playerInputManager.OnFleetSelectedByMouse += FleetSelectMouse; // 함선 선택 (마우스)
        playerInputManager.OnFleetProducted += ProductFleet; // 함선 생산

        // 플레이어가 점령 가능한 행성의 최대치는 맵에 있는 행성 수와 같다
        playerPlanet = new GameObject[GameObject.FindGameObjectsWithTag("planet").Length];

        // 플레이어 모성 데이터에 접근
        playerBase = playerBasePlanet.GetComponent<planetCtrl>();

        // 모성의 중립을 해제
        playerPlanet[0] = playerBasePlanet;
        playerBase.isNetural = false;
        playerBase.SwitchColor(1);

        // 현보유 자원 초기화 후 표시
        playerMineral = 0;
        playerGas = 0;
        playerMaxSupply = playerBase.supplyAmount;
        playerCurrentSupply = 0;
        DisplayResource();
        DisplayCurrentFleetStatus();

        // 자원 채취 시작
        StartCoroutine(GetResource());
    }

    // 부대지정
    private void FleetSaveSlot(int num)
    {
        fleetSlot[num] = selectedFleet;
    }

    // 마우스를 왼쪽 클릭해서 함대를 선택 
    private void FleetSelectMouse(GameObject fleet)
    {
        // 함선 선택
        selectedFleet = fleet;

        // 소리 재생
        if( selectedFleet != null ) soundManager.PlaySound("select");
        
        // 선택된 함선 정보 보여주기
        DisplayCurrentFleetStatus();
    }

    // 숫자 키를 눌러서 부대지정된 함대를 선택
    private void FleetSelectNumber(int num)
    {
        // 함선 선택
        selectedFleet = fleetSlot[num];
        
        // 소리 재생
        if( selectedFleet != null ) soundManager.PlaySound("select");

        // 선택된 함선 정보 보여주기
        DisplayCurrentFleetStatus();
    }

    // // 마우스를 왼쪽 클릭해서 함대를 생산
    // void FleetProductMouse()
    // {
    //     Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //     RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

    //     if( hit.collider != null )
    //     {
    //         if( hit.collider.gameObject.name == "fleetIconQ" ) StartCoroutine(ProductFleet(playerScout));
    //         if( hit.collider.gameObject.name == "fleetIconW" ) StartCoroutine(ProductFleet(playerDestroyer));
    //         if( hit.collider.gameObject.name == "fleetIconE" ) StartCoroutine(ProductFleet(playerCruiser));
    //         if( hit.collider.gameObject.name == "fleetIconR" ) StartCoroutine(ProductFleet(playerBattleCruiser));
    //     }
    // }

    // 2초마다 소유한 행성들에서 생산된 자원을 채취
    IEnumerator GetResource()
    {
        for (int i = 0; i < playerPlanet.Length; i++)
        {
            if (playerPlanet[i] != null)
            {
                planetCtrl planet = playerPlanet[i].GetComponent<planetCtrl>();
                playerMineral += planet.mineralAmount;
                playerGas += planet.gasAmount;
            }
        }
        DisplayResource();
        yield return new WaitForSeconds(1);
        if (gameManager.isDone == false) StartCoroutine(GetResource());
        // 인구수는 행성을 점령하거나 빼앗일 때 변동됨
    }

    // 플레이어의 현보유 자원을 표시
    public void DisplayResource()
    {
        InGameUIManager.Instance?.DisplayResource(playerMineral, playerGas, playerCurrentSupply, playerMaxSupply);
    }

    // 현재 선택된 함대의 정보를 보여준다
    public void DisplayCurrentFleetStatus()
    {
        InGameUIManager.Instance?.DisplayCurrentFleetStatus(selectedFleet, fleetSlot);
    }

    public void ProductFleet(int num)
    {
        switch( num )
        {
            case 0: StartCoroutine(ProductFleet(playerScout)); break;
            case 1: StartCoroutine(ProductFleet(playerDestroyer)); break;
            case 2: StartCoroutine(ProductFleet(playerCruiser)); break;
            case 3: StartCoroutine(ProductFleet(playerBattleCruiser)); break;
        }
    }

    private IEnumerator ProductFleet(GameObject fleet)
    {
        // 생산중인 함대가 없을 때에만 생산 가능
        if (productingFleet == null)
        {
            playerFleetCtrl fleetData = fleet.GetComponent<playerFleetCtrl>();

            // 해당 함대를 생산하기에 충분한 자원이 있는 경우 생산 시작
            if (playerMineral >= fleetData.mineralNeed
            && playerGas >= fleetData.gasNeed
            && (playerMaxSupply - playerCurrentSupply) >= fleetData.supplyNeed)
            {
                // 자원을 소모하고
                playerMineral -= fleetData.mineralNeed;
                playerGas -= fleetData.gasNeed;
                playerCurrentSupply += fleetData.supplyNeed;

                // 남은 자원을 표시
                DisplayResource();

                // 함대 생산 시작
                productingFleet = fleet;
                int productTime = fleetData.timeNeed;
                soundManager.PlaySound("command"); // 생산 사운드
                for (int i = 0; i < productTime; i++)
                {
                    InGameUIManager.Instance?.UpdateProductionProgress(fleetData.fleetName, productTime - i);
                    yield return new WaitForSeconds(1);
                }
                StartCoroutine(SpawnFleet(fleet));
            }
            // 무엇 하나라도 자원이 부족한 경우
            else
            {
                string message = "";
                if (playerMineral < fleetData.mineralNeed) message = "광물 보유량 부족";
                else if (playerGas < fleetData.gasNeed) message = "가스 보유량 부족";
                else if ((playerMaxSupply - playerCurrentSupply) < fleetData.supplyNeed) message = "보급품 보유량 부족";

                if (!string.IsNullOrEmpty(message))
                {
                    InGameUIManager.Instance?.ShowProductionMessage(message, Color.red);
                }
                soundManager.PlaySound("fleetError"); // 에러 사운드
            }
        }
        else
        {
            // 함대가 생산중인 경우
        }
    }

    IEnumerator ResetProductingFleetInfo()
    {
        yield return new WaitForSeconds(1);
        InGameUIManager.Instance?.ClearProductionInfo();
    }

    IEnumerator SpawnFleet(GameObject fleet)
    {
        yield return new WaitForSeconds(1.0f);
        // 생산 완료 했는데 모성에 함대가 없어야 함대가 생성됨
        soundManager.PlaySound("command"); // 생산 사운드
        if (playerBase.currentFleet == null)
        {
            Instantiate(fleet, playerBase.transform.position, Quaternion.Euler(0, 0, 0));
            productingFleet = null;
            InGameUIManager.Instance?.ClearProductionInfo();
        }
        // 모성에 함대가 있으면 함대 생성이 늦어진다.
        else
        {
            InGameUIManager.Instance?.ShowProductionMessage("함대 이륙 대기중", Color.yellow);
            StartCoroutine(SpawnFleet(fleet));
        }
    }

}
