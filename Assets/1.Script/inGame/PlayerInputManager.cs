using System;
using UI;
using UnityEngine;

public class PlayerInputManager : MonoBehaviour
{  
    public static PlayerInputManager Instance; // 싱글톤 인스턴스
    public Action<int> OnFleetProducted, // 함선 생산
                       OnFleetSelectedByKeyboard, // 함선 선택 (키보드)
                       OnFleetSaveSlot; // 함선 부대지정
    public Action<GameObject> OnFleetSelectedByMouse, // 함선 선택 (마우스)
                              OnPlanetSelected; // 행성 선택
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Update()
    {
        // 게임 중일 때
        if( gameCtrl.Instance.isDone == false)
        {
            // 일시정지 상태가 아닌 경우
            if( Time.timeScale > 0 )
            {
                // 숫자 키만 누르면 부대지정된 함대를 선택
                if( Input.GetKey(KeyCode.LeftShift) == false )
                {
                    for( int i = 0; i < 5; i++ )
                    {
                        if( Input.GetKeyDown(KeyCode.Alpha1 + i) )
                        {
                            OnFleetSelectedByKeyboard?.Invoke(i);
                        }
                    }
                }
                // 왼쪽 쉬프트 + 숫자 키를 누르면 함대 저장
                else
                {
                    for( int i = 0; i < 5; i++ )
                    {
                        if( Input.GetKeyDown(KeyCode.Alpha1 + i) )
                        {
                            OnFleetSaveSlot?.Invoke(i);
                        }
                    }
                }

                // Q W E R을 누르면 함선 생산
                if( Input.GetKeyDown(KeyCode.Q)) OnFleetProducted?.Invoke(0);
                if( Input.GetKeyDown(KeyCode.W)) OnFleetProducted?.Invoke(1);
                if( Input.GetKeyDown(KeyCode.E)) OnFleetProducted?.Invoke(2);
                if( Input.GetKeyDown(KeyCode.R)) OnFleetProducted?.Invoke(3);

                // 마우스를 좌클릭하여 함선 선택
                if( Input.GetMouseButtonDown(0)) OnMouseLeftDown();

                // 마우스를 우클릭하여 이동할 행성 선택
                if( Input.GetMouseButtonDown(1)) OnMouseRightDown();
            }
        }
    }

    // 마우스 왼쪽 클릭
    private void OnMouseLeftDown()
    {
        GameObject selectedFleet = null;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D[] hit = Physics2D.RaycastAll(ray.origin, ray.direction);
        
        // ray에 닿은 모든 오브젝트를 검사
        for( int i = 0; i < hit.Length; i++ )
        {
            if( hit[i].collider != null )
            {
                // ray 닿은 오브젝트에 함대가 포함된다면 해당 함대를 선택
                if (hit[i].collider.tag == "playerFleet" || hit[i].collider.tag == "enemyFleet" )
                {
                    selectedFleet = hit[i].collider.gameObject;
                }
            }
        }

        // 이렇게 선택된 함대 정보를 매개변수에 담아 액션 호출
        // 1. playerGameCtrl에서 구독
        OnFleetSelectedByMouse?.Invoke(selectedFleet);
    }

    // 마우스 오른쪽 클릭
    private void OnMouseRightDown()
    {
        GameObject selectedPlanet = null;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D[] hit = Physics2D.RaycastAll(ray.origin, ray.direction);
        
        // ray에 닿은 모든 오브젝트를 검사
        for( int i = 0; i < hit.Length; i++ )
        {
            if( hit[i].collider != null )
            {
                // ray 닿은 오브젝트에 행성이 포함된다면 해당 행성을 선택
                if( hit[i].collider.tag == "planet" )
                {
                    selectedPlanet = hit[i].collider.gameObject;
                }
            }
        }

        // 이렇게 선택된 행성 정보를 매개변수에 담아 액션 호출
        // 1. playerFleetCtrl에서 구독
        if( selectedPlanet != null ) OnPlanetSelected?.Invoke(selectedPlanet);
    }

    // 오브젝트 파괴 시 액션의 모든 구독 해제
    private void OnDestroy()
    {
        OnFleetProducted = null;
        OnFleetSelectedByKeyboard = null;
        OnFleetSelectedByMouse = null;
        OnPlanetSelected = null;
    }
}