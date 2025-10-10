using System.Collections;
using UnityEngine;

public class playerFleetCtrl : MonoBehaviour
{
    public bool isSelected, isMoving, isBattle;
    public int mineralNeed, gasNeed, supplyNeed, timeNeed;
    public int attack, defence, maxDefence;
    public float maxMoveSpeed;
    private float moveSpeed;
    public GameObject currentPlanet, startPlanet, goalPlanet, selectedSign, fleetDeadEffect;
    private playerGameCtrl playerManager;
    public string fleetName;
    void Start()
    {
        playerManager = GameObject.Find("gameManager(player)").GetComponent<playerGameCtrl>();

        isSelected = false;
        isMoving = false;
        isBattle = false;
        defence = maxDefence;
        moveSpeed = maxMoveSpeed;
        selectedSign.SetActive(false);
        currentPlanet = playerManager.playerBasePlanet;
    }


    void Update()
    {
        // 게임매니저에서 자신이 선택된 상태인지 확인
        if (isSelected == false) if (playerManager.selectedFleet == gameObject) isSelected = true;
        if (isSelected == true) if (playerManager.selectedFleet != gameObject) isSelected = false;
        selectedSign.SetActive(isSelected);

        // 선택된 상태라면
        if (isSelected == true)
        {
            SetMoveTarget(); // 목적지 설정 가능
            if (Input.GetKey(KeyCode.LeftShift) == true) SaveSlot(); // 왼쪽 컨트롤 + 숫자 버튼을 눌러 부대지정 가능
        }

        // 이동중인 상태라면 목적지로 이동
        if (isMoving == true)
            transform.position = Vector2.MoveTowards(transform.position, goalPlanet.transform.position, moveSpeed * Time.deltaTime);

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 함대가 생산 되었을 때, 생산된 행성을 현재 위치로 설정
        if (currentPlanet == null) if (other.tag == "planet") currentPlanet = other.gameObject;

        // 이동 중에
        if (isMoving == true && moveSpeed > 0)
        {
            //아군 함대를 만나면
            if (other.tag == "playerFleet")
            {
                // 출발지로 되돌아감
                GameObject tempPlanet;
                tempPlanet = startPlanet;
                startPlanet = goalPlanet;
                goalPlanet = tempPlanet;

                // 새로운 목표로 방향 전환 (출발지를 바라봄)
                RotateFleet(goalPlanet);
            }
        }

        // 적 함대를 만나면 전투 시작 (이동 중이 아니라도)
        if (other.tag == "enemyFleet")
        {
            // 현재 이동속도를 0으로 만들어서, 이동을 멈춤
            moveSpeed = 0;
            StartCoroutine(fleetBattle(other));
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // 이동중인 상태인데
        if (isMoving == true)
        {
            // 행성 내부에 있을 경우
            if (other.tag == "planet")
            {
                // 만난 행성이 목적지였다면
                if (other.gameObject == goalPlanet)
                {
                    // 행성에 완전히 도착했을 때
                    if (transform.position.x == other.transform.position.x
                    && transform.position.y == other.transform.position.y)
                    {
                        // 지금 만난 행성을 현재 위치로 설정 후 이동 종료
                        currentPlanet = other.gameObject;
                        currentPlanet.GetComponent<planetCtrl>().isPlayerGoal = false;
                        isMoving = false;
                    }
                }
            }
        }
    }

    IEnumerator fleetBattle(Collider2D other)
    {
        yield return new WaitForSeconds(0.025f);

        // 상대가 살아있다면
        if (other != null)
        {
            RotateFleet(other.gameObject); // 전투 대상을 바라봄

            soundCtrl.Instance.PlaySound(soundCtrl.SoundType.Fight); // 전투 소리 재생
            yield return new WaitForSeconds(1f);

            if (other != null) defence -= other.GetComponent<enemyFleetCtrl>().attack; // 함대의 방어력을 상대의 공격력 만큼 깎고

            // 남은 체력을 확인하여 1 미만일 경우 사망
            if (defence < 1)
            {
                // 사망 시 차지하고 있던 인구수를 감소
                playerManager.playerCurrentSupply -= supplyNeed;
                playerManager.DisplayResource();
                Instantiate(fleetDeadEffect, transform.position, Quaternion.Euler(0, 0, 0));
                Destroy(gameObject);
            }

            // 살아남았다면 다시 한 번 더 교전
            StartCoroutine(fleetBattle(other));
        }
        // 상대가 파괴되면 현재 이동속도를 원래대로 돌려서, 이동중이었다면 계속 이동하게끔
        else
        {
            if (goalPlanet != null) RotateFleet(goalPlanet); // 목적지를 바라봄
            moveSpeed = maxMoveSpeed;
        }
    }

    void SetMoveTarget()
    {
        // 선택된 상태인데 현재 이동중이 아니라면
        if (isMoving == false)
        {
            // 우클릭한 행성을 목적지로 설정
            if (Input.GetMouseButtonDown(1))
            {
                // 우클릭 시 ray를 쏴서, ray에 맞은 모든 오브젝트를 가져옴
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit2D[] hit = Physics2D.RaycastAll(ray.origin, ray.direction);

                for (int i = 0; i < hit.Length; i++)
                {
                    // 우클릭 ray에 맞은 오브젝트 중 행성이 있다면
                    if (hit[i].collider.tag == "planet")
                    {
                        // 현재 위치한 행성과 인접한 행성인지 확인
                        for (int j = 0; j < currentPlanet.GetComponent<planetCtrl>().nearPlanet.Length; j++)
                        {
                            // 인접한 행성이라면 이동 가능
                            if (hit[i].collider.gameObject == currentPlanet.GetComponent<planetCtrl>().nearPlanet[j])
                            {
                                // 현재 위치한 곳을 출발지로 설정하고 Ray에 맞은 행성을 목적지로 설정
                                startPlanet = currentPlanet;
                                goalPlanet = hit[i].collider.gameObject;

                                // 목적지를 바라봄
                                Transform moveTarget = goalPlanet.GetComponent<Transform>();
                                Vector2 moveDirection = (moveTarget.position - transform.position).normalized;
                                float moveAngle = Mathf.Atan2(moveDirection.x, -moveDirection.y) * Mathf.Rad2Deg;
                                transform.rotation = Quaternion.Euler(0, 0, moveAngle + 180);

                                // 이동 시작   
                                isMoving = true;
                                goalPlanet.GetComponent<planetCtrl>().isPlayerGoal = true;

                                // 이동 시작 소리 재생
                                soundCtrl.Instance.PlaySound(soundCtrl.SoundType.Command);
                            }
                        }
                    }
                }
            }
        }
    }

    void SaveSlot()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) playerManager.fleetSlot[0] = gameObject;
        if (Input.GetKeyDown(KeyCode.Alpha2)) playerManager.fleetSlot[1] = gameObject;
        if (Input.GetKeyDown(KeyCode.Alpha3)) playerManager.fleetSlot[2] = gameObject;
        if (Input.GetKeyDown(KeyCode.Alpha4)) playerManager.fleetSlot[3] = gameObject;
        if (Input.GetKeyDown(KeyCode.Alpha5)) playerManager.fleetSlot[4] = gameObject;
    }

    void RotateFleet(GameObject Target)
    {
        // 새로운 목표로 방향 전환
        Transform moveTarget = Target.GetComponent<Transform>();
        Vector2 moveDirection = (moveTarget.position - transform.position).normalized;
        float moveAngle = Mathf.Atan2(moveDirection.x, -moveDirection.y) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, moveAngle + 180);
    }
}
