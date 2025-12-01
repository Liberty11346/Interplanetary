using TMPro;
using UnityEngine;
using CommonLib;
public class PlayerArea : MonoBehaviour
{
    private PlayerState[] playerStates = new PlayerState[2];
    private void Start()
    {
        for( int i = 0 ; i < 2 ; i++ )
        {
            playerStates[i] = transform.GetChild(i).GetComponent<PlayerState>();
        }
    }

    public void UpdateUI(PlayerInfo[] playerInfo)
    {
        for( int i = 0 ; i < 2 ; i++ )
        {
            playerStates[i].UpdateUI(playerInfo[i]);
        }
    }
}