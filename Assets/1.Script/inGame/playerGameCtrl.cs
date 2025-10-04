using System.Collections;
using System.Data;
using UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class playerGameCtrl : MonoBehaviour
{
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
    // [SerializeField] private Button fleetIconQButton, fleetIconWButton, fleetIconEButton, fleetIconRButton;
    void Start()
    {
        // 게임매니저 정보에 접근
        gameManager = GameObject.Find("gameManager").GetComponent<gameCtrl>();

        // 사운드매니저 정보에 접근
        soundManager = GameObject.Find("soundManager").GetComponent<soundCtrl>();

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

        // 자원 채취 시작
        StartCoroutine(GetResource());

    }

    void Update()
    {
        // 게임 중일 때
        if (gameManager.isDone == false)
        {
            // 일시정지 상태가 아닌 경우
            if (Time.timeScale > 0)
            {
                // 마우스를 왼쪽 클릭해서
                if (Input.GetMouseButtonDown(0))
                {
                    FleetSelectMouse(); // 함대 선택
                    // 함대 생산은 버튼 onClick으로 처리
                }

                // 숫자 키를 누르면 부대지정된 함대를 선택
                if (Input.GetKey(KeyCode.LeftShift) == false) FleetSelectNumber();

                // Q W E R 버튼을 눌러서 함대 생산
                if (Input.GetKeyDown(KeyCode.Q)) StartCoroutine(ProductFleet(playerScout));
                if (Input.GetKeyDown(KeyCode.W)) StartCoroutine(ProductFleet(playerDestroyer));
                if (Input.GetKeyDown(KeyCode.E)) StartCoroutine(ProductFleet(playerCruiser));
                if (Input.GetKeyDown(KeyCode.R)) StartCoroutine(ProductFleet(playerBattleCruiser));
            }
        }
    }

    // 마우스를 왼쪽 클릭해서 함대를 선택 
    void FleetSelectMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D[] hit = Physics2D.RaycastAll(ray.origin, ray.direction);

        for (int i = 0; i < hit.Length; i++)
        {
            if (hit[i].collider != null)
            {
                // 함대를 클릭하면 해당 함대를 선택
                if (hit[i].collider.tag == "playerFleet")
                {
                    selectedFleet = hit[i].collider.gameObject;
                    soundManager.PlaySound("select"); // 함대 선택 사운드
                }
                if (hit[i].collider.tag == "enemyFleet") selectedFleet = hit[i].collider.gameObject;
            }
            DisplayCurrentFleetStatus();
        }

        // 빈 곳을 클릭하면 선택 해제
        if (hit.Length == 0)
        {
            selectedFleet = null;
            DisplayCurrentFleetStatus();
        }
    }

    // 숫자 키를 눌러서 부대지정된 함대를 선택
    void FleetSelectNumber()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            selectedFleet = fleetSlot[0];
            // 부대 지정된 함대가 선택되면 함대 선택 사운드 재생
            if (selectedFleet != null) soundManager.PlaySound("select");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            selectedFleet = fleetSlot[1];
            // 부대 지정된 함대가 선택되면 함대 선택 사운드 재생
            if (selectedFleet != null) soundManager.PlaySound("select");
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            selectedFleet = fleetSlot[2];
            // 부대 지정된 함대가 선택되면 함대 선택 사운드 재생
            if (selectedFleet != null) soundManager.PlaySound("select");
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            selectedFleet = fleetSlot[3];
            // 부대 지정된 함대가 선택되면 함대 선택 사운드 재생
            if (selectedFleet != null) soundManager.PlaySound("select");
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            selectedFleet = fleetSlot[4];
            // 부대 지정된 함대가 선택되면 함대 선택 사운드 재생
            if (selectedFleet != null) soundManager.PlaySound("select");
        }

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
    void DisplayCurrentFleetStatus()
    {
        InGameUIManager.Instance?.DisplayCurrentFleetStatus(selectedFleet, fleetSlot);
    }

    public IEnumerator ProductFleet(GameObject fleet)
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
