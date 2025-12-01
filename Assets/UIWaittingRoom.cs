using TMPro;
using UnityEngine;

public class UIWaittingRoom : MonoBehaviour
{
    public TextMeshProUGUI Title;
    public class Data
    {
        public string titleText = "";
    }

    public void SetData(Data data)
    {
        Title.text = data.titleText;
    }
}
