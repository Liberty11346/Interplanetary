using System.Collections;
using TMPro;
using UnityEngine;

public class tutorialCtrl : MonoBehaviour
{
    [SerializeField] private GameObject blinder, textBox, highLightBox;
    [SerializeField] private TextMeshProUGUI boxMainText, boxSmallText;
    [SerializeField] private TextMeshPro pointText;
    private planetCtrl asus;
    private bool isStop;
    private bool[] stepCondition = new bool[10];
    private playerGameCtrl playerManager;
    void Start()
    {
        playerManager = GameObject.Find("gameManager(player)").GetComponent<playerGameCtrl>();
        asus = GameObject.Find("Asus").GetComponent<planetCtrl>();

        // 튜토리얼 도중 게임을 일시 정지 하기 위해 사용
        isStop = false;
        
        // 튜토리얼 도중 일정 조건 달성 시 다음 튜토리얼을 시작하기 위해 사용
        for( int i = 0 ; i < stepCondition.Length ; i++ ) stepCondition[i] = false;

        textBox.SetActive(false);
        blinder.SetActive(false);
        pointText.text = "";
        boxSmallText.text = "";

        StartCoroutine(Step1());
    }

    void Update()
    {
        // 일시 정지 상태일 때 아무곳이나 클릭하여 일시 정지 해제
        if( isStop == true )
        {
            Time.timeScale = 0;
            if( Input.GetMouseButtonDown(0) )
            {
                Time.timeScale = 1;
                isStop = false;
            }
        }
        
        // 첫 함대 생산 시 함대 설명
        if( stepCondition[0] == false )
        {
            if( GameObject.FindWithTag("playerFleet") != null )
            {
                stepCondition[0] = true;
                StartCoroutine(Step8());
            }
        }
        // 함대 선택 시 함대 조종 방법 설명
        else if( stepCondition[1] == false )
        {
            if(playerManager.selectedFleet != null )
            {
                stepCondition[1] = true;
                StartCoroutine(Step10());
            }
        }
        // 함대 이동 명령 시 다음 설명
        else if( stepCondition[2] == false )
        {
            if( Input.GetMouseButtonDown(1) )
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
                
                if( hit.collider != null )
                {
                    if( hit.collider.gameObject.name == "Asus" )
                    {
                        stepCondition[2] = true;
                        StartCoroutine(Step12());
                    }
                }
            }
        }
        // 함대 행성에 도착 시 다음 설명
        else if ( stepCondition[3] == false )
        {
            if( asus.currentFleet != null )
            {
                if( asus.currentFleet.tag == "playerFleet" )
                {
                    stepCondition[3] = true;
                    StartCoroutine(Step14());
                }
            }
        }
        // 행성 완전 점령 시 다음 설명
        else if ( stepCondition[4] == false )
        {
            if( asus.captureRate > 500 )
            {
                stepCondition[4] = true;
                StartCoroutine(Step16());
            }
        }


    }

    IEnumerator Step1()
    {
        yield return new WaitForSeconds(2);
        
        textBox.SetActive(true);
        blinder.SetActive(true);
        boxMainText.text = "인터플래니터리에 오신것을 환영합니다!\n이번 미션에서는 인터플래니터리의\n기본 게임방법에 대해 알아봅니다.";
        isStop = true;

        boxSmallText.text = "아무 곳이나 클릭하여 넘기기";
        yield return new WaitForSeconds(0.1f);
        StartCoroutine(Step2());
    }

    IEnumerator Step2()
    {
        boxMainText.text = "인터플래니터리는 AI를 상대로 행성을\n점령하고 자원을 채취하며 전투를 벌이는\n싱글플레이어 실시간 전략 게임입니다.\n다양한 전장에서 승리를 쟁취하세요!";

        isStop = true;
        boxSmallText.text = "아무 곳이나 클릭하여 넘기기";
        yield return new WaitForSeconds(0.1f);
        textBox.SetActive(false);
        StartCoroutine(Step3());
    }

    IEnumerator Step3()
    {
        pointText.transform.SetParent(GameObject.Find("Asus").transform);
        pointText.rectTransform.localPosition = new Vector3(57,0,-100);
        pointText.alignment = TextAlignmentOptions.TopLeft;
        pointText.text = "이것은 행성입니다!\n행성은 전장을 이루는 기본 단위이며,\n전투는 서로 행성을 뺏고 빼앗기는 쟁탈전의 연속입니다.";
        
        isStop = true;
        yield return new WaitForSeconds(0.1f);
        StartCoroutine(Step4());
    }

    IEnumerator Step4()
    {
        pointText.transform.SetParent(GameObject.Find("Earth").transform);
        pointText.rectTransform.localPosition = new Vector3(57,0,-100);
        pointText.text = "게임 시작부터 가지고 있는 행성은 모성입니다.\n모성에서 함대를 생산할 수 있으며,\n모성을 점령당할 경우 게임에서 패배합니다.";

        isStop = true;
        yield return new WaitForSeconds(0.1f);
        StartCoroutine(Step5());
    }

    IEnumerator Step5()
    {
        pointText.transform.SetParent(GameObject.Find("Zeus").transform);
        pointText.rectTransform.localPosition = new Vector3(-57,0,-100);
        pointText.alignment = TextAlignmentOptions.TopRight;
        pointText.text = "여기 적의 모성이 있습니다.\n아군의 모성을 지키면서 적의 모성을 점령하여\n게임에서 승리하세요!";

        isStop = true;
        yield return new WaitForSeconds(0.1f);
        StartCoroutine(Step6());
    }

    IEnumerator Step6()
    {
        pointText.transform.SetParent(GameObject.Find("fleetIcons").transform);
        pointText.rectTransform.localPosition = new Vector3(0,200,-10);
        pointText.alignment = TextAlignmentOptions.Center;
        pointText.alignment = TextAlignmentOptions.Midline;
        pointText.text = "행성을 점령하기 위해선 함대가 필요합니다.\n화면 하단에서 보유 자원을 확인하고 함대를 생산할 수 있습니다.\n버튼에 마우스를 가져가면 함대를 생산하는데 필요한\n자원과 시간의 정보도 확인할 수 있습니다.";
        isStop = true;
        yield return new WaitForSeconds(0.1f);
        StartCoroutine(Step7());
    }

    IEnumerator Step7()
    {
        yield return new WaitForSeconds(0);
        blinder.SetActive(false);

        pointText.alignment = TextAlignmentOptions.Center;
        pointText.alignment = TextAlignmentOptions.Midline;
        pointText.text = "자원을 모으고 함대를 생산해보세요!";
    }

    IEnumerator Step8()
    {
        yield return new WaitForSeconds(0.5f);
        isStop = true;

        blinder.SetActive(true);
        pointText.alignment = TextAlignmentOptions.TopLeft;
        pointText.transform.SetParent(GameObject.Find("Earth").transform);
        pointText.rectTransform.localPosition = new Vector3(57,0,-100);
        pointText.text = "생산된 함대는 항상 모성에서 등장하며,\n모성에 다른 함대가 정박해있는 상태라면\n해당 함대가 사라질 때까지 생산이 보류됩니다.";
        yield return new WaitForSeconds(0.1f);
        StartCoroutine(Step9());
    }

    IEnumerator Step9()
    {
        yield return new WaitForSeconds(0);
        blinder.SetActive(false);
        isStop = true;

        pointText.text = "먼저 명령을 내릴 함대를 선택해야 합니다.\n함대를 좌클릭하여 선택해보세요.";
    }

    IEnumerator Step10()
    {
        blinder.SetActive(true);
        isStop = true;

        pointText.text = "함대를 선택한 후, 함대의 현재 위치와\n직접 연결된 행성을 우클릭하여\n이동 명령을 내릴 수 있습니다.";
        yield return new WaitForSeconds(0.1f);
        StartCoroutine(Step11());
    }

    IEnumerator Step11()
    {
        yield return new WaitForSeconds(0);
        blinder.SetActive(false);
        isStop = true;

        pointText.transform.SetParent(GameObject.Find("Asus").transform);
        pointText.rectTransform.localPosition = new Vector3(57,0,-100);
        pointText.text = "ASUS 행성을 우클릭하여 이동 명령을 내려보세요.";
    }

    IEnumerator Step12()
    {
        pointText.text = "";
        yield return new WaitForSeconds(2);
        blinder.SetActive(true);
        isStop = true;

        
        pointText.rectTransform.localPosition = new Vector3(35,-20,-100);
        pointText.text = "함대는 이동중에 만난 적과 자동으로 전투하며,\n아군과 만날 경우 출발지로 복귀합니다.";
        yield return new WaitForSeconds(0.1f);
        StartCoroutine(Step13());
    }

    IEnumerator Step13()
    {
        isStop = true;
        
        pointText.text = "이동 중인 함대에는 명령을 내릴 수 없습니다.\n행성에 도착해야 새로운 명령을 내릴 수 있으니\n신중하고 정확하게 내리세요!";
        yield return new WaitForSeconds(0.1f);
        pointText.text = "";
        blinder.SetActive(false);
    }

    IEnumerator Step14()
    {
        // 함대가 행성에 도착할 경우 점령 설명
        isStop = true;
        blinder.SetActive(true);
        
        pointText.transform.SetParent(GameObject.Find("Asus").transform);
        pointText.rectTransform.localPosition = new Vector3(57,0,-100);
        pointText.text = "비어있는 행성에 함대가 도착하면 점령을 시작합니다.\n점령도가 100%에 도달하면 완전히 점령됩니다.\n완전히 점령되기 전까진 행성 소유자가 바뀌지 않습니다.";
        yield return new WaitForSeconds(0.1f);
        StartCoroutine(Step15());
    }

    IEnumerator Step15()
    {
        yield return new WaitForSeconds(0);
        blinder.SetActive(false);
        pointText.text = "ASUS 행성을 완전히 점령하세요.";
    }

    IEnumerator Step16()
    {
        // 행성 완전 점령 시 자원 설명
        isStop = true;
        blinder.SetActive(true);
        pointText.transform.SetParent(GameObject.Find("fleetIcons").transform);
        pointText.rectTransform.localPosition = new Vector3(0,200,-10);
        pointText.alignment = TextAlignmentOptions.Center;
        pointText.alignment = TextAlignmentOptions.Top;
        pointText.text = "행성을 점령하면 행성이 보유한 만큼의 자원을 얻습니다.\n광물과 가스의 생산량이 증가하고, 보급품 최대치가 증가합니다.\n많은 자원을 확보하면 더 강한 함대를 생산할 수 있습니다.";
        yield return new WaitForSeconds(0.1f);
        StartCoroutine(Step17());
    }

    IEnumerator Step17()
    {
        pointText.text = " ";
        
        textBox.SetActive(true);
        boxMainText.text = "인터플래니터리의 모든 것을 배웠습니다!\n이제 적의 모성을 점령하여 승리하고\n튜토리얼을 마무리하세요!";
        isStop = true;

        boxSmallText.text = "아무 곳이나 클릭하여 닫기";
        yield return new WaitForSeconds(0.1f);
        textBox.SetActive(false);
        blinder.SetActive(false);
    }
}
