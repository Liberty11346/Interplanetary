using TMPro;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// 플레이어의 자원 (광물, 가스, 보급품) UI를 관리합니다.
    /// </summary>
    public class PlayerResourceUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI playerMineralText, playerGasText, playerSupplyText;

        public void UpdateResourceDisplay(int mineral, int gas, int currentSupply, int maxSupply)
        {
            if (playerMineralText != null) playerMineralText.text = mineral.ToString();
            if (playerGasText != null) playerGasText.text = gas.ToString();
            if (playerSupplyText != null) playerSupplyText.text = $"{currentSupply}/{maxSupply}";
        }
    }
}