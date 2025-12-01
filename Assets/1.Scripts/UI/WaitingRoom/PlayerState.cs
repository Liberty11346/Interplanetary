using TMPro;
using UnityEngine;
using CommonLib;

public class PlayerState : MonoBehaviour
{
    private TextMeshProUGUI _playerNameTMP,
                            _playerReadyTMP;
            
    private void Start()
    {
        _playerNameTMP = transform.Find("PlayerName").GetComponent<TextMeshProUGUI>();
        _playerReadyTMP = transform.Find("PlayerReady").GetComponent<TextMeshProUGUI>();
    }

    public void UpdateUI(PlayerInfo playerInfo)
    {
        if( playerInfo.IsPlayerOnline )
        {
            _playerNameTMP.text = playerInfo.PlayerName;
            _playerReadyTMP.text = playerInfo.IsPlayerReady ? "준비 상태" : "대기 상태";   
        }
        else
        {
            _playerNameTMP.text = " ";
            _playerReadyTMP.text = "플레이어를 기다리는 중..";
        }
    }
}
