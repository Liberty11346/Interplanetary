using TMPro;
using UnityEngine;
using CommonLib;
using System.Collections.Generic;
public class PlayerArea : MonoBehaviour
{
    [SerializeField]
    private List<PlayerState> playerStates = new List<PlayerState>();
    private void Start()
    {
        for( int i = 0 ; i < 2 ; i++ )
        {
            playerStates[i] = transform.GetChild(i).GetComponent<PlayerState>();
        }
    }

    public void UpdateUI(WaittingRoomUser[] data)
    {
        if (data == null)
            return;
        for( int i = 0 ; i < data.Length; i++ )
        {
            playerStates[i].UpdateUI(data[i].UserInfo, data[i].IsReady);
        }
    }
}
