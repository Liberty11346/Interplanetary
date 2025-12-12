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
        {
            Debug.LogWarning("[PlayerArea] UpdateUI: data is null");
            return;
        }

        // playerStates가 초기화되지 않았거나 비어있으면 다시 초기화
        if (playerStates == null || playerStates.Count == 0)
        {
            Debug.LogWarning("[PlayerArea] playerStates not initialized, initializing now");
            playerStates = new List<PlayerState>();
            for (int i = 0; i < 2; i++)
            {
                if (transform.childCount > i)
                {
                    var state = transform.GetChild(i).GetComponent<PlayerState>();
                    if (state != null)
                    {
                        playerStates.Add(state);
                    }
                }
            }
        }

        for (int i = 0; i < data.Length && i < playerStates.Count; i++)
        {
            if (playerStates[i] != null)
            {
                playerStates[i].UpdateUI(data[i].UserInfo, data[i].IsReady);
            }
        }

        Debug.Log($"[PlayerArea] Updated UI with {data.Length} players");
    }
}
