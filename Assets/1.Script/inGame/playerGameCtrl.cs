using System.Collections;
using System.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class playerGameCtrl : MonoBehaviour
{
    public GameObject selectedFleet;
    public GameObject[] fleetSlot = new GameObject[5];
    public GameObject[] playerPlanet;
    public GameObject playerScout, playerDestroyer, playerCruiser, playerBattleCruiser, playerBasePlanet;
    public int playerMaxSupply, playerCurrentSupply;
    private int playerMineral, playerGas;
    private GameObject selectedFleetUI;
    public GameObject productingFleet;
    private Image selectedFleetImage;
    private TextMeshProUGUI selectedFleetAttackText, selectedFleetDefenceText, selectedFleetSpeedText,
                            selectedFleetName, selectedFleetOwner, productingFleetInfo;
    private TextMeshProUGUI playerMineralText, playerGasText, playerSupplyText;
    private playerFleetCtrl player;
    private enemyFleetCtrl enemy;
    public planetCtrl playerBase;
    private gameCtrl gameManager;
    private soundCtrl soundManager;
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

        // 정보를 표시할 이미지와 텍스트 컴포넌트에 접근
        selectedFleetUI = GameObject.Find("selectedFleetUI");
        selectedFleetAttackText = GameObject.Find("selectedFleetAttackText").GetComponent<TextMeshProUGUI>();
        selectedFleetDefenceText = GameObject.Find("selectedFleetDefenceText").GetComponent<TextMeshProUGUI>();
        selectedFleetSpeedText = GameObject.Find("selectedFleetSpeedText").GetComponent<TextMeshProUGUI>();
        selectedFleetName = GameObject.Find("selectedFleetName").GetComponent<TextMeshProUGUI>();
        selectedFleetOwner = GameObject.Find("selectedFleetOwner").GetComponent<TextMeshProUGUI>();
        productingFleetInfo = GameObject.Find("productingFleetInfo").GetComponent<TextMeshProUGUI>();
        selectedFleetImage = GameObject.Find("selectedFleetImage").GetComponent<Image>();

        // 플레이어의 현재 자원 상태를 보여주는 텍스트에 접근
        playerMineralText = GameObject.Find("playerMineralText").GetComponent<TextMeshProUGUI>();
        playerGasText = GameObject.Find("playerGasText").GetComponent<TextMeshProUGUI>();
        playerSupplyText = GameObject.Find("playerSupplyText").GetComponent<TextMeshProUGUI>();

        // 현보유 자원 초기화 후 표시
        playerMineral = 0;
        playerGas = 0;
        playerMaxSupply = playerBase.supplyAmount;
        playerCurrentSupply = 0;
        DisplayResource();

        // 자원 채취 시작
        StartCoroutine(GetResource());

        // 비활성화된 상태로 시작해야하는데 그렇게 하면 null 에러 일으키는 오브젝트를 게임 시작 후 1프레임 뒤에 비활성화시켜요
        StartCoroutine(newGameReset());
    }

    void Update()
    {
        // 게임 중일 때
        if( gameManager.isDone == false )
        {
            // 일시정지 상태가 아닌 경우
            if( Time.timeScale > 0 )
            {
                // 마우스를 왼쪽 클릭해서
                if( Input.GetMouseButtonDown(0) )
                {
                    FleetSelectMouse(); // 함대 선택
                    FleetProductMouse(); // 함대 생산
                }

                // 숫자 키를 누르면 부대지정된 함대를 선택
                if( Input.GetKey(KeyCode.LeftShift) == false ) FleetSelectNumber();

                // Q W E R 버튼을 눌러서 함대 생산
                if( Input.GetKeyDown(KeyCode.Q)) StartCoroutine(ProductFleet(playerScout));
                if( Input.GetKeyDown(KeyCode.W)) StartCoroutine(ProductFleet(playerDestroyer));
                if( Input.GetKeyDown(KeyCode.E)) StartCoroutine(ProductFleet(playerCruiser));
                if( Input.GetKeyDown(KeyCode.R)) StartCoroutine(ProductFleet(playerBattleCruiser));
            }
        }
    }

    // 마우스를 왼쪽 클릭해서 함대를 선택 
    void FleetSelectMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D[] hit = Physics2D.RaycastAll(ray.origin, ray.direction);
            
        for( int i = 0 ; i < hit.Length ; i++ )
        {
            if( hit[i].collider != null )
            {
                // 함대를 클릭하면 해당 함대를 선택
                if( hit[i].collider.tag == "playerFleet")
                {
                    selectedFleet = hit[i].collider.gameObject;
                    soundManager.PlaySound("select"); // 함대 선택 사운드
                }
                if( hit[i].collider.tag == "enemyFleet") selectedFleet = hit[i].collider.gameObject;
            }
            DisplayCurrentFleetStatus();
        }

        // 빈 곳을 클릭하면 선택 해제
        if( hit.Length == 0 )
        {
            selectedFleet = null;
            DisplayCurrentFleetStatus();
        }
    }

    // 숫자 키를 눌러서 부대지정된 함대를 선택
    void FleetSelectNumber()
    {
        if( Input.GetKeyDown(KeyCode.Alpha1) )
        {
            selectedFleet = fleetSlot[0];
            // 부대 지정된 함대가 선택되면 함대 선택 사운드 재생
            if( selectedFleet != null ) soundManager.PlaySound("select");
        }

        if( Input.GetKeyDown(KeyCode.Alpha2) )
        {
            selectedFleet = fleetSlot[1];
            // 부대 지정된 함대가 선택되면 함대 선택 사운드 재생
            if( selectedFleet != null ) soundManager.PlaySound("select");
        }

        if( Input.GetKeyDown(KeyCode.Alpha3) )
        {
            selectedFleet = fleetSlot[2];
            // 부대 지정된 함대가 선택되면 함대 선택 사운드 재생
            if( selectedFleet != null ) soundManager.PlaySound("select");
        }

        if( Input.GetKeyDown(KeyCode.Alpha4) )
        {
            selectedFleet = fleetSlot[3];
            // 부대 지정된 함대가 선택되면 함대 선택 사운드 재생
            if( selectedFleet != null ) soundManager.PlaySound("select");
        }

        if( Input.GetKeyDown(KeyCode.Alpha5) )
        {
            selectedFleet = fleetSlot[4];
            // 부대 지정된 함대가 선택되면 함대 선택 사운드 재생
            if( selectedFleet != null ) soundManager.PlaySound("select");
        }

        DisplayCurrentFleetStatus();
    }

    // 마우스를 왼쪽 클릭해서 함대를 생산
    void FleetProductMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
            
        if( hit.collider != null )
        {
            if( hit.collider.gameObject.name == "fleetIconQ" ) StartCoroutine(ProductFleet(playerScout));
            if( hit.collider.gameObject.name == "fleetIconW" ) StartCoroutine(ProductFleet(playerDestroyer));
            if( hit.collider.gameObject.name == "fleetIconE" ) StartCoroutine(ProductFleet(playerCruiser));
            if( hit.collider.gameObject.name == "fleetIconR" ) StartCoroutine(ProductFleet(playerBattleCruiser));
        }
    }

    // 2초마다 소유한 행성들에서 생산된 자원을 채취
    IEnumerator GetResource()
    {
        for( int i = 0 ; i < playerPlanet.Length ; i++ )
        {
            if( playerPlanet[i] != null )
            {
                planetCtrl planet = playerPlanet[i].GetComponent<planetCtrl>();
                playerMineral += planet.mineralAmount;
                playerGas += planet.gasAmount;
            }
        }
        DisplayResource();
        yield return new WaitForSeconds(1);
        if( gameManager.isDone == false ) StartCoroutine(GetResource());
        // 인구수는 행성을 점령하거나 빼앗일 때 변동됨
    }

    // 플레이어의 현보유 자원을 표시
    public void DisplayResource()
    {
        playerMineralText.text = playerMineral.ToString();
        playerGasText.text = playerGas.ToString();
        playerSupplyText.text = playerCurrentSupply.ToString() + "/" + playerMaxSupply.ToString();
    }

    // 현재 선택된 함대의 정보를 보여준다
    void DisplayCurrentFleetStatus()
    {   
        // 선택된 함대가 있다면
        if( selectedFleet != null )
        {   
            // UI가 눈에 보이게 활성화하고
            selectedFleetUI.SetActive(true);
            // 아군인지 적군인지 확인하여 정보를 보여준다
            if( selectedFleet.tag == "playerFleet")
            {
                player = selectedFleet.GetComponent<playerFleetCtrl>(); // 해당 함대의 변수에 접근하기 위해 스크립트를 가져옴
                selectedFleetName.text = player.fleetName; // 함대의 이름을 표시

                // 함대가 부대지정 되어있을 경우 부대 번호도 알려줌
                // 같은 함대가 여러 부대로 지정되어 있을 경우 번호가 빠른 순으로 알려줌
                int i = 0; bool isSloted = false;
                for( i = 0 ; i < fleetSlot.Length ; i++ )
                {
                    if( fleetSlot[i] == selectedFleet )
                    {
                        isSloted = true;
                        break;
                    }
                }
                    
                if( isSloted == true )
                    selectedFleetOwner.text = "<color=#00FFFF>아군</color> <color=#FFFFFF>(" + (i+1).ToString() + "번 함대)</color>";
                else
                    selectedFleetOwner.text = "<color=#00FFFF>아군</color> <color=#FF0000> (번호미지정)</color>";
                
                // 함대의 능력치 정보를 표시
                selectedFleetAttackText.text = player.attack.ToString();
                selectedFleetSpeedText.text = player.maxMoveSpeed.ToString();

                // 피해를 입은 상태라면 방어력을 빨간색으로 표시, 그렇지 않다면 초록색으로 표시
                selectedFleetDefenceText.text = player.defence.ToString() + "/" + player.maxDefence.ToString();
                if( player.defence < player.maxDefence ) selectedFleetDefenceText.color = Color.red;
                else selectedFleetDefenceText.color = Color.white;

                // 함대의 이미지를 표시
                selectedFleetImage.sprite = selectedFleet.GetComponent<SpriteRenderer>().sprite;

                // 함종에 따라 이미지 크기도 다르게
                RectTransform imageRect = selectedFleetImage.GetComponent<RectTransform>();
                switch( player.fleetName )
                {
                    case "정찰기": imageRect.sizeDelta = new Vector2(100,100); break;
                    case "구축함": imageRect.sizeDelta = new Vector2(100,110); break;
                    case "순양함": imageRect.sizeDelta = new Vector2(100,150); break;
                    case "전투순양함": imageRect.sizeDelta = new Vector2(100,150); break;
                }
            }
            else if( selectedFleet.tag == "enemyFleet")
            {
                enemy = selectedFleet.GetComponent<enemyFleetCtrl>(); // 해당 함대의 변수에 접근하기 위해 스크립트를 가져옴

                // 함대의 이름과 소유주를 표시
                selectedFleetName.text = enemy.fleetName;
                selectedFleetOwner.text = "적군";
                selectedFleetOwner.color = Color.magenta;

                // 함대의 능력치 정보를 표시
                selectedFleetAttackText.text = enemy.attack.ToString();
                selectedFleetSpeedText.text = enemy.maxMoveSpeed.ToString();
                
                // 피해를 입은 상태라면 방어력을 빨간색으로 표시, 그렇지 않다면 초록색으로 표시
                selectedFleetDefenceText.text = enemy.defence.ToString() + "/" + enemy.maxDefence.ToString();
                if( enemy.defence < enemy.maxDefence ) selectedFleetDefenceText.color = Color.red;
                else selectedFleetDefenceText.color = Color.white;

                // 함대의 이미지를 표시
                selectedFleetImage.sprite = selectedFleet.GetComponent<SpriteRenderer>().sprite;

                // 함종에 따라 이미지 크기도 다르게
                RectTransform imageRect = selectedFleetImage.GetComponent<RectTransform>();
                switch( enemy.fleetName )
                {
                    case "정찰기": imageRect.sizeDelta = new Vector2(100,100); break;
                    case "구축함": imageRect.sizeDelta = new Vector2(100,110); break;
                    case "순양함": imageRect.sizeDelta = new Vector2(100,150); break;
                    case "전투순양함": imageRect.sizeDelta = new Vector2(100,150); break;
                }
            }
        }
        // 선택된 함대가 없다면
        else
        {
            // 아무것도 보여주지 않는다
            selectedFleetUI.SetActive(false);
        }
    }

    IEnumerator ProductFleet(GameObject fleet)
    {
        // 생산중인 함대가 없을 때에만 생산 가능
        if( productingFleet == null )
        {
            playerFleetCtrl fleetData = fleet.GetComponent<playerFleetCtrl>();

            // 해당 함대를 생산하기에 충분한 자원이 있는 경우 생산 시작
            if( playerMineral >= fleetData.mineralNeed 
            && playerGas >= fleetData.gasNeed 
            && (playerMaxSupply - playerCurrentSupply) >= fleetData.supplyNeed )
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
                for( int i = 0 ; i < productTime ; i++ )
                {
                    productingFleetInfo.text = fleetData.fleetName + " 생산중.. (" + (productTime - i).ToString() + ")";
                    productingFleetInfo.color = Color.green;
                    yield return new WaitForSeconds(1);
                }
                StartCoroutine(SpawnFleet(fleet));
            }
            // 무엇 하나라도 자원이 부족한 경우
            else
            {   
                // 불가능 메세지를 띄우고
                productingFleetInfo.color = Color.red;
                if( playerMineral < fleetData.mineralNeed ) productingFleetInfo.text = "광물 보유량 부족";
                else if( playerGas < fleetData.gasNeed ) productingFleetInfo.text = "가스 보유량 부족";
                else if( ( playerMaxSupply - playerCurrentSupply) < fleetData.supplyNeed ) productingFleetInfo.text = "보급품 보유량 부족";
                soundManager.PlaySound("fleetError"); // 에러 사운드
                // 메세지 초기화
                StartCoroutine(ResetProductingFleetInfo());
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
        productingFleetInfo.text = "";
    }

    IEnumerator SpawnFleet(GameObject fleet)
    {
        yield return new WaitForSeconds(1.0f);
        // 생산 완료 했는데 모성에 함대가 없어야 함대가 생성됨
        soundManager.PlaySound("command"); // 생산 사운드
        if( playerBase.currentFleet == null )
        {
            Instantiate(fleet, playerBase.transform.position, Quaternion.Euler(0,0,0) );
            productingFleet = null;
            productingFleetInfo.text = "";
        }
        // 모성에 함대가 있으면 함대 생성이 늦어진다.
        else 
        {
            productingFleetInfo.text = "함대 이륙 대기중";
            productingFleetInfo.color = Color.yellow;
            StartCoroutine(SpawnFleet(fleet));
        }
    }

    IEnumerator newGameReset()
    {
        yield return new WaitForEndOfFrame();
        GameObject.Find("infoFleetUI").SetActive(false);
    }
}
