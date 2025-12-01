using TMPro;
using UnityEngine;
using CommonLib;

public class PlayerState : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _playerNameTMP,
                            _playerReadyTMP;
            
    public void UpdateUI(UserInfo playerInfo, bool IsPlayerReady)
    {
        if (playerInfo.UserId != -1)
        {
            _playerNameTMP.text = playerInfo.UserName;
            _playerReadyTMP.text = IsPlayerReady ? "준비 상태" : "대기 상태";
        }
        else
        {
            _playerNameTMP.text = " ";
            _playerReadyTMP.text = "플레이어를 기다리는 중..";
        }
    }
}
