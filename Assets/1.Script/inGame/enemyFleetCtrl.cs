using UnityEngine;
using System.Collections;

public class enemyFleetCtrl : MonoBehaviour
{
    public bool isSelectedPlayer, isSelectedEnemy, isMoving;
    public int mineralNeed, gasNeed, supplyNeed, timeNeed;
    public int attack, defence, maxDefence;
    public float maxMoveSpeed;
    private float moveSpeed;
    public GameObject currentPlanet, startPlanet, goalPlanet, selectedSign, fleetDeadEffect;
    private enemyGameCtrl enemyManager;
    private playerGameCtrl playerManager;
    public string fleetName;
    void Start()
    {
        playerManager = GameObject.Find("gameManager(player)").GetComponent<playerGameCtrl>();
        enemyManager = GameObject.Find("gameManager(computer)").GetComponent<enemyGameCtrl>();
        moveSpeed = maxMoveSpeed;
        defence = maxDefence;
        isMoving = true;
        
        startPlanet = enemyManager.enemyBasePlanet; 
        goalPlanet = enemyManager.enemyBasePlanet;
        currentPlanet = enemyManager.enemyBasePlanet;
    }

    // Update is called once per frame
    void Update()
    {
        // 게임매니저에서 자신이 선택된 상태인지 확인
        if( isSelectedPlayer == false ) if( playerManager.selectedFleet == gameObject ) isSelectedPlayer = true;
        if( isSelectedPlayer == true ) if( playerManager.selectedFleet != gameObject ) isSelectedPlayer = false;
        selectedSign.SetActive(isSelectedPlayer);

        // 이동중인 상태라면 목적지로 이동
        if( isMoving == true )
            transform.position = Vector2.MoveTowards(transform.position, goalPlanet.transform.position, moveSpeed * Time.deltaTime);
    }

    void OnTriggerEnter2D( Collider2D other )
    {
        // 이동 중에
        if( isMoving == true && moveSpeed > 0)
        {
            // 아군 함대를 만나면
            if( other.tag == "enemyFleet")
            {
                // 출발지로 되돌아감
                GameObject tempPlanet;
                tempPlanet = startPlanet;
                startPlanet = goalPlanet;
                goalPlanet = tempPlanet;

                // 새로운 목표로 방향 전환
                RotateFleet(goalPlanet);
            }
        }

        // 플레이어 함대를 만나면 전투 시작 (이동 중이 아니라도)
        if( other.tag == "playerFleet")
        {
            moveSpeed = 0;
            StartCoroutine(fleetBattle(other));
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // 이동중인 상태인데
        if( isMoving == true )
        {
            // 행성 내부에 있을 경우
            if( other.tag == "planet")
            {
                // 만난 행성이 목적지였다면
                if( other.gameObject == goalPlanet )
                {
                    // 행성에 완전히 도착했을 때
                    if( transform.position.x == other.transform.position.x 
                    && transform.position.y == other.transform.position.y ) 
                    {
                        // 지금 만난 행성을 현재 위치로 설정 후 이동 종료
                        currentPlanet = other.gameObject;
                        currentPlanet.GetComponent<planetCtrl>().isEnemyGoal = false;
                        isMoving = false;
                    }
                }
            }
        }
    }

    IEnumerator fleetBattle( Collider2D other )
    {
        yield return new WaitForSeconds(0.025f);
        
        // 상대가 살아있다면
        if( other != null )
        {
            RotateFleet(other.gameObject); // 전투 대상을 바라봄

            yield return new WaitForSeconds(1f);

            // 함대의 방어력을 상대의 공격력 만큼 깎고
            if( other != null ) defence -= other.GetComponent<playerFleetCtrl>().attack;
            
            if( defence < 1 )
            {
                enemyManager.enemyCurrentSupply -= supplyNeed;
                Instantiate(fleetDeadEffect, transform.position, Quaternion.Euler(0,0,0));
                
                // 이동중인 상태였다면, 목표 행성에 대한 isGoal 변수를 초기화
                // 정지한 상태였다면, 현재 행성에 대한 isGoal 변수를 초기화
                if( isMoving == true ) goalPlanet.GetComponent<planetCtrl>().isEnemyGoal = false;
                else currentPlanet.GetComponent<planetCtrl>().isEnemyGoal = false;

                Destroy(gameObject);
            }

            // 다시 한 번 더 교전
            StartCoroutine(fleetBattle(other));
        }
        // 현재 이동속도를 원래대로 돌려서, 이동중이었다면 계속 이동하게끔
        else
        {
            if( goalPlanet != null ) RotateFleet(goalPlanet); // 목적지를 바라봄
            moveSpeed = maxMoveSpeed;
        }
    }

    public void RotateFleet(GameObject Target)
    {
        // 새로운 목표로 방향 전환
        Transform moveTarget = Target.GetComponent<Transform>();
        Vector2 moveDirection = (moveTarget.position - transform.position).normalized;
        float moveAngle = Mathf.Atan2(moveDirection.x, -moveDirection.y) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0,0,moveAngle + 180);
    }
}
