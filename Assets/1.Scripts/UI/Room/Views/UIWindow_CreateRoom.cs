using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using CommonLib;

public class UIWindow_CreateRoom : MonoBehaviour
{
    public class Data
    {
        public string defaultRoomName;
        public List<string> maps;
    }

    public TMP_InputField roomnameInput;
    public TMP_Dropdown mapDropdown;
    public Toggle isPrivateBtn;
    public Button roomCreateBtn;

    bool isPrivate = false;

    public System.Action<string, int, bool> OnClickedCreate;

    public void Awake()
    {
        roomCreateBtn.onClick.AddListener(HandleOnCLickPrivate);
        isPrivateBtn.isOn = false;
        // 단순하게 값 받아오는 용도라 람다 사용
        isPrivateBtn.onValueChanged.AddListener((state) => { isPrivate = state; });
    }

    private void HandleOnCLickPrivate()
    {
        OnClickedCreate?.Invoke(roomnameInput.text, mapDropdown.value, isPrivate);
    }

    public void SetData(Data data)
    {
        roomnameInput.text = data.defaultRoomName;
        mapDropdown.ClearOptions();
        foreach (var map in data.maps)
        {
            mapDropdown.options.Add(new TMP_Dropdown.OptionData(map));
        }
        mapDropdown.RefreshShownValue();
    }
}
