using TMPro;
using UnityEngine;

public class UIPlayerResource : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI supText;
    [SerializeField]
    TextMeshProUGUI gasText;
    [SerializeField]
    TextMeshProUGUI mineralText;

    public void UpdateResources(int gas, int mineral, int sup, int maxSup)
    {
        supText.text = $"{sup.ToString()}/{maxSup.ToString()}";
        gasText.text = gas.ToString();
        mineralText.text = mineral.ToString();
    }
}
