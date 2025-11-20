using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIRoomListMenu : MonoBehaviour
{
    public class Data
    {
        public List<string> dropdownText;
    }

    [SerializeField]
    TMP_Dropdown filterDropdown;
    [SerializeField]
    Button refreshButton;

    public System.Action<int> OnFilterValueChanged;
    public System.Action OnClickedRefresh;

    private void Awake()
    {
        filterDropdown.onValueChanged.AddListener(HandleOnvalueUpdate);
        refreshButton.onClick.AddListener(HandleOnClickRefresh);
    }

    private void HandleOnvalueUpdate(int index)
    {
        OnFilterValueChanged?.Invoke(index);
    }

    private void HandleOnClickRefresh()
    {
        OnClickedRefresh?.Invoke();
    }

    public void SetData(Data data)
    {
        filterDropdown.options.Clear();
        foreach (var item in data.dropdownText)
        {
            var dOption = new TMP_Dropdown.OptionData(item);
            filterDropdown.options.Add(dOption);
        }
        // 드롭다운 리프레시
        filterDropdown.RefreshShownValue();
    }
}
