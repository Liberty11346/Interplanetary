using UnityEngine;
using System.Collections;

public class enemyGameCtrl : MonoBehaviour
{
    public GameObject[] enemyPlanet;
    private GameObject[] enemyFleet;
    public GameObject enemyBasePlanet;
    public int enemyMineral, enemyGas, enemyCurrentSupply, enemyMaxSupply;
    [System.Serializable] public struct enemyFleetStructure
    {
        public GameObject type;
        public enemyFleetCtrl data;
    }

    public enemyFleetStructure[] enemyFleetList = new enemyFleetStructure[4];
    public planetCtrl enemyBase;
    private gameCtrl gameManager;
    void Start()
    {
        // 게임매니저 정보에 접근
        gameManager = GameObject.Find("gameManager").GetComponent<gameCtrl>();

        // 상대가 점령 가능한 행성의 최대치는 맵에 있는 행성 수와 같다
        enemyPlanet = new GameObject[GameObject.FindGameObjectsWithTag("planet").Length];

        // 모성의 정보를 가져옴
        enemyBase = enemyBasePlanet.GetComponent<planetCtrl>();

        // 모성의 중립을 해제
        enemyPlanet[0] = enemyBasePlanet;
        enemyBase.isNetural = false;
        enemyBase.SwitchColor(2);
        
        // 각종 변수 초기화
        enemyMineral = 0;
        enemyGas = 0;
        enemyCurrentSupply = 0;
        enemyMaxSupply = enemyBase.supplyAmount;

        // 생성 가능한 함대들의 정보에 접근
        for( int i = 0 ; i < 4 ; i++ ) enemyFleetList[i].data = enemyFleetList[i].type.GetComponent<enemyFleetCtrl>();

        StartCoroutine(GetResource()); // 자원 채집 시작
        StartCoroutine(ProductFleet()); // 함대 생산 알고리즘 가동
        StartCoroutine(FindMyFleet()); // 함대 명령 알고리즘 가동
    }

    // 1초마다 보유한 행성으로부터 자원 획득
    IEnumerator GetResource()
    {
        for( int i = 0 ; i < enemyPlanet.Length ; i++ )
        {
            if( enemyPlanet[i] != null )
            {
                planetCtrl planet = enemyPlanet[i].GetComponent<planetCtrl>();
                enemyMineral += planet.mineralAmount;
                enemyGas += planet.gasAmount;
            }
        }
        yield return new WaitForSeconds(1);
        if( gameManager.isDone == false ) StartCoroutine(GetResource());
    }

    // 명령 가능한 함대를 찾아서 명령
    IEnumerator FindMyFleet()
    {
        // 전체 함대 목록을 업데이트
        enemyFleet = GameObject.FindGameObjectsWithTag("enemyFleet");
        enemyFleetCtrl selectedFleet; // 선택된 함대의 정보
        planetCtrl fleetCurrentPlanet = enemyBase.GetComponent<planetCtrl>(); // 선택된 함대가 위치한 행성의 정보 (기본값 모성)

        // 명령 가능한 함대를 찾는다
        for( int i = 0 ; i < enemyFleet.Length ; i++ )
        {
            selectedFleet = enemyFleet[i].GetComponent<enemyFleetCtrl>();
            if( selectedFleet.currentPlanet != null ) fleetCurrentPlanet = selectedFleet.currentPlanet.GetComponent<planetCtrl>();

            // 명령 가능한 함대가 있을 경우
            if( selectedFleet.isMoving == false )
            {
                // 함대가 자신 소유의 행성 위에 있을 경우
                if( fleetCurrentPlanet.captureRate <= -500 )
                {
                    // 해당 함대의 위치에서 이동 가능한 행성이 있는지 찾는다 (뒤에서부터 찾는다)
                    for( int j = 4 ; j >= 0 ; j-- )
                    {
                        // 행성이 있다면
                        if( fleetCurrentPlanet.nearPlanet[j] != null )
                        {
                            // 해당 행성이 자신의 소유인지 찾는다
                            bool isOwned = false;
                            for( int k = 0 ; k < enemyPlanet.Length ; k++ )
                            {   
                                // 자기 행성 소유가 맞다면 넘어가고
                                if( enemyPlanet[k] == fleetCurrentPlanet.nearPlanet[j] )
                                {
                                    isOwned = true;
                                    break;
                                }
                            }

                            // 이미 해당 행성에 자기 함대가 이동중인 경우에도 넘어간다.
                            //if( fleetCurrentPlanet.nearPlanet[j].GetComponent<planetCtrl>().isEnemyGoal == true ) isOwned = true;

                            // 자기 소유가 아닌 행성이 있는데
                            if( isOwned == false )
                            {
                                // 상주 함대가 없거나 자기 함대가 아니라면 이동
                                if( fleetCurrentPlanet.nearPlanet[j].GetComponent<planetCtrl>().currentFleet == null )
                                {
                                    selectedFleet.startPlanet = selectedFleet.currentPlanet;
                                    selectedFleet.goalPlanet = fleetCurrentPlanet.nearPlanet[j].gameObject;
                                    selectedFleet.isMoving = true;
                                    selectedFleet.RotateFleet(selectedFleet.goalPlanet);
                                    selectedFleet.goalPlanet.GetComponent<planetCtrl>().isEnemyGoal = true;
                                    break;
                                }
                                else if( fleetCurrentPlanet.nearPlanet[j].GetComponent<planetCtrl>().currentFleet != null )
                                {
                                    if( fleetCurrentPlanet.nearPlanet[j].GetComponent<planetCtrl>().currentFleet.tag != "enemyFleet" )
                                    {
                                        selectedFleet.startPlanet = selectedFleet.currentPlanet;
                                        selectedFleet.goalPlanet = fleetCurrentPlanet.nearPlanet[j].gameObject;
                                        selectedFleet.isMoving = true;
                                        selectedFleet.RotateFleet(selectedFleet.goalPlanet);
                                        selectedFleet.goalPlanet.GetComponent<planetCtrl>().isEnemyGoal = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                        
                    // for문을 나왔는데도 여전히 이동중이 아니라면 (이동할 행성을 찾지 못했다면)
                    if( selectedFleet.isMoving == false )
                    {
                        // 상주 함대가 없는 행성을 찾아간다
                        for( int j = 0 ; j < 5 ; j++ )
                        {
                            if( fleetCurrentPlanet.nearPlanet[j] != null )
                            {
                                if( fleetCurrentPlanet.nearPlanet[j].GetComponent<planetCtrl>().currentFleet == null )
                                {
                                    selectedFleet.startPlanet = selectedFleet.currentPlanet;
                                    selectedFleet.goalPlanet = fleetCurrentPlanet.nearPlanet[j].gameObject;
                                    selectedFleet.isMoving = true;
                                    selectedFleet.RotateFleet(selectedFleet.goalPlanet);
                                    selectedFleet.goalPlanet.GetComponent<planetCtrl>().isEnemyGoal = true;
                                    break;
                                }
                            }
                        }

                        // for문을 나왔는도 여전히 이동중이 아니라면 (이동할 행성을 찾지 못했다면)
                        if( selectedFleet.isMoving == false )
                        {
                            // 그냥 첫번째 행성으로 간다
                            selectedFleet.startPlanet = selectedFleet.currentPlanet;
                            selectedFleet.goalPlanet = fleetCurrentPlanet.nearPlanet[0].gameObject;
                            selectedFleet.isMoving = true;
                            selectedFleet.RotateFleet(selectedFleet.goalPlanet);
                            selectedFleet.goalPlanet.GetComponent<planetCtrl>().isEnemyGoal = true;
                        }
                    }
                }
            }
        }
        
        // 1초마다 반복
        yield return new WaitForSeconds(1);
        if( gameManager.isDone == false ) StartCoroutine(FindMyFleet());
    }

    // 보유한 자원으로 생산 가능한 가장 강한 함대를 생산
    IEnumerator ProductFleet()
    {
        // 강한 함대 정보부터 찾아본다
        for( int i = 3 ; i >= 0 ; i-- )
        {   
            // 현재 자원으로 생산 가능할 경우
            if( enemyMineral >= enemyFleetList[i].data.mineralNeed
            && enemyGas >= enemyFleetList[i].data.gasNeed 
            && (enemyMaxSupply - enemyCurrentSupply) >= enemyFleetList[i].data.supplyNeed )
            {
                // 자원을 소모하고
                enemyMineral -= enemyFleetList[i].data.mineralNeed;
                enemyGas -= enemyFleetList[i].data.gasNeed;
                enemyCurrentSupply += enemyFleetList[i].data.supplyNeed;

                // 일정 시간 대기 후 모성에 함대 생성
                yield return new WaitForSeconds( enemyFleetList[i].data.timeNeed );

                // 모성에 함대가 없어야 생산된다.
                // 모성에 함대가 있을 경우 모성에 함대가 없을 때까지 생성 시도
                while ( true )
                {
                    if( enemyBase.currentFleet == null )
                    {
                        Instantiate(enemyFleetList[i].type, enemyBase.transform.position, Quaternion.Euler(0,0,0) );
                        break;
                    }
                    yield return new WaitForSeconds(0.5f);
                }
                break;
            }
        }

        // 보유 함대가 많을 수록 생산 주기가 느려짐 (더 많은 자원을 모았다가 강한 함대를 생산)
        // yield return new WaitForSeconds( enemyFleet != null ? 2 + enemyFleet.Length : 2 );
        
        // 2초마다 함대 생산
        yield return new WaitForSeconds(2);
        if( gameManager.isDone == false ) StartCoroutine(ProductFleet());
    }
}
