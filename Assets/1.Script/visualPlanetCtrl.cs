using UnityEngine;

public class visualPlanetCtrl
 : MonoBehaviour
{
    public GameObject[] nearPlanet = new GameObject[5];
    private LineRenderer liner;
    
    void Start()
    {
        liner = transform.GetComponent<LineRenderer>();
        DrawLine();
    }

    // Update is called once per frame
    void Update()
    {

    }

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
    }
}
