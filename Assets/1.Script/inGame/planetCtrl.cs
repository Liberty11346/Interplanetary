using System;
using TMPro;
using UnityEngine;

public class planetCtrl : MonoBehaviour
{
    public GameObject[] nearPlanet = new GameObject[5];
    public GameObject currentFleet;
    private playerGameCtrl playerManager;
    private enemyGameCtrl enemyManager;
    public TextMeshPro planetName, mineral, gas, supply, captureRateText;
    public int mineralAmount, gasAmount, supplyAmount, captureRate = 0;
    private LineRenderer liner;
    public bool isNetural, isPlayerGoal, isEnemyGoal;
    void Start()
    {
        playerManager = GameObject.Find("gameManager(player)").GetComponent<playerGameCtrl>();
        enemyManager = GameObject.Find("gameManager(computer)").GetComponent<enemyGameCtrl>();
        currentFleet = null;
        isNetural = true;
        isPlayerGoal = false;
        isEnemyGoal = false;
        liner = transform.GetComponent<LineRenderer>();
        DrawLine();
    }

    void Update()
    {
        // 일시정지 상태가 아닐 때에만 점령됨
        if( Time.timeScale > 0 )
        {
            CapturePlanet();
            DisaplyCaptureRate();   
        }
    }

    // 현재 행성의 점령도를 표시
    // 현재 우세한 세력의 점령도를 표시한다. ex) captureRate가 100일 경우 플레이어의 색상으로 33% 표시
    void DisaplyCaptureRate()
    {
        // 플레이어가 우세한 경우
        if( captureRate > 0 )
        {
            captureRateText.color = Color.cyan;
            captureRateText.text = Math.Abs(captureRate/5).ToString() + "%";
        }
        // 상대가 우세한 경우
        else if (captureRate < 0 )
        {
            captureRateText.color = Color.magenta;
            captureRateText.text = Math.Abs(captureRate/5).ToString() + "%";
        }
        // 중립인 경우
        else
        {
            captureRateText.color = Color.white;
            captureRateText.text = Math.Abs(captureRate/5).ToString() + "%";
        }
    }

    // 주변 행성 사이를 잇는 선을 그린다
    // + 행성의 자원 보유량을 표시
    void DrawLine()
    {  
        // 주변 행성 수를 센다. (배열에 실제로 들어간 오브젝트의 수를 센다.)
        int planetCount = 0;
        for( int i = 0 ; i < nearPlanet.Length ; i++ ) if( nearPlanet[i] != null ) planetCount++;

        // 주변 행성 수 * 2개 만큼의 꼭짓점을 준비
        liner.positionCount = planetCount*2;

        // 자기 자신과 주변 행성을 왔다갔다 하게끔 꼭짓점을 놓는다
        int temp = 0;
        for( int i = 0 ; i < liner.positionCount ; i += 2 )
        {
            liner.SetPosition(i, transform.position);
            liner.SetPosition(i+1, nearPlanet[temp].transform.position );
            temp++;
        }

        // 선을 그린다
        liner.startWidth = 0.05f;
        liner.startColor = Color.gray;
        liner.endColor = Color.gray;

        // 자원 보유량을 표시
        mineral.text = mineralAmount.ToString();
        gas.text = gasAmount.ToString();
        supply.text = supplyAmount.ToString();
    }

    // 행성 소유자에 따라 색깔 바꾸기
    public void SwitchColor( int ownPlayer )
    {
        switch ( ownPlayer )
        {
            case 1: planetName.color = Color.cyan; break; // 플레이어
            case 2: planetName.color = Color.magenta; break; // 상대
            default: planetName.color = Color.white; break; // 중립
        }
    }

    void OnTriggerStay2D( Collider2D other )
    {
        // 현재 행성에 상주 함대가 없을 경우
        if( currentFleet == null )
        {   
            // 상주 함대를 충돌한 함대로 설정
            if( other.tag == "playerFleet") currentFleet = other.gameObject;
            if( other.tag == "enemyFleet") currentFleet = other.gameObject;
        }
    }

    // 함대가 행성을 벗어남 감지
    void OnTriggerExit2D( Collider2D other )
    {
        // 벗어난 함대가 현재 함대라면
        if( other.gameObject == currentFleet )
        {
            // 상주 함대 없음으로 설정
            currentFleet = null;
        }
    }

    void CapturePlanet()
    {
        if( currentFleet != null )
        {
            // 플레이어 함선이 행성 내에 있을 경우 플레이어가 행성을 점령
            if( currentFleet.tag == "playerFleet" )
            {
                if( captureRate < 500 ) captureRate++;
                else if( captureRate == 500 )
                {
                    CaptureByPlayer();
                    captureRate++;
                }
            }
            // 상대 함선이 행성 내에 있을 경우 상대가 행성을 점령
            else if( currentFleet.tag == "enemyFleet" )
            {
                if( captureRate > -500 ) captureRate--;
                else if( captureRate == -500 )
                {
                    CaptureByEnemy();
                    captureRate--;
                }
            }
        }
    }

    // 플레이어에 의해 점령된 상황
    void CaptureByPlayer()
    {
        // 이미 플레이어에게 점령된 상태라면 또 점령되지 않음
        bool isCapturedTwice = false;
        for( int i = 0 ; i < playerManager.playerPlanet.Length ; i++ )
        {
            if( playerManager.playerPlanet[i] == gameObject )
            {
                isCapturedTwice = true;
                break;
            }
        }
        
        // 중립이 아닌 상태에서 새롭게 점령된 상태라면
        if( isNetural == false )
        {
            if( isCapturedTwice == false )
            {
                // 상대 소유 행성 배열에서 스스로를 제거
                for( int i = 0 ; i < enemyManager.enemyPlanet.Length ; i++ )
                {
                    if( enemyManager.enemyPlanet[i] == gameObject )
                    {
                        enemyManager.enemyPlanet[i] = null;
                        break;
                    }
                }

                // 플레이어 소유 행성 배열에 들어감
                for( int i = 0 ; i < playerManager.playerPlanet.Length ; i++ )
                {
                    if( playerManager.playerPlanet[i] == null )
                    {
                        playerManager.playerPlanet[i] = gameObject;
                        break;
                    }
                }

                playerManager.playerMaxSupply += supplyAmount; // 플레이어 최대 인구수 증가
                enemyManager.enemyMaxSupply -= supplyAmount; // 상대 최대 인구수 감소
                playerManager.DisplayResource(); // 인구수 표기 업데이트
                SwitchColor(1); // 이름 색깔 바꾸기
            }
        }
        // 중립인 상태에서 새롭게 점령되었다면
        else
        {
            isNetural = false; // 중립 상태 해제

            // 플레이어 소유 행성 배열에 들어감
            for( int i = 0 ; i < playerManager.playerPlanet.Length ; i++ )
            {
                if( playerManager.playerPlanet[i] == null )
                {
                    playerManager.playerPlanet[i] = gameObject;
                    break;
                }
            }

            playerManager.playerMaxSupply += supplyAmount; // 플레이어 최대 인구수 증가
            playerManager.DisplayResource(); // 인구수 표기 업데이트
            SwitchColor(1); // 이름 색깔 바꾸기
        }
    }

    void CaptureByEnemy()
    {
        // 이미 상대에게 점령된 상태라면 또 점령되지 않음
        bool isCapturedTwice = false;
        for( int i = 0 ; i < enemyManager.enemyPlanet.Length ; i++ )
        {
            if( enemyManager.enemyPlanet[i] == gameObject )
            {
                isCapturedTwice = true;
                break;
            }
        }

        // 중립이 아닌 상태에서 새롭게 점령된 상태라면
        if( isNetural == false )
        {
            if( isCapturedTwice == false )
            {
                // 플레이어 소유 행성 배열에서 스스로를 제거
                for( int i = 0 ; i < playerManager.playerPlanet.Length ; i++ )
                {
                    if( playerManager.playerPlanet[i] == gameObject )
                    {
                        playerManager.playerPlanet[i] = null;
                        break;
                    }
                }

                // 상대 소유 행성 배열에 들어감
                for( int i = 0 ; i < enemyManager.enemyPlanet.Length ; i++ )
                {
                    if( enemyManager.enemyPlanet[i] == null )
                    {
                        enemyManager.enemyPlanet[i] = gameObject;
                        break;
                    }
                }

                enemyManager.enemyMaxSupply += supplyAmount; // 상대 최대 인구수 증가
                playerManager.playerMaxSupply -= supplyAmount; // 플레이어 최대 인구수 감소
                playerManager.DisplayResource(); // 인구수 표기 업데이트
                SwitchColor(2); // 이름 색깔 바꾸기
            }
        }
        // 중립인 상태에서 새롭게 점령되었다면
        else
        {
            isNetural = false; // 중립 상태 해제

            // 상대 소유 행성 배열에 들어감
            for( int i = 0 ; i < enemyManager.enemyPlanet.Length ; i++ )
            {
                if( enemyManager.enemyPlanet[i] == null )
                {
                    enemyManager.enemyPlanet[i] = gameObject;
                    break;
                }
            }

            enemyManager.enemyMaxSupply += supplyAmount; // 상대 최대 인구수 증가
            SwitchColor(2); // 이름 색깔 바꾸기
        }
    }
}
