using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

public class UIManager : MonoBehaviour
{
    [Header("UI Documents")]
    public GameObject inventoryUI;
    public GameObject accessoryBagUI;
    public GameObject expeditionBagUI;

    void Update()
    {
        // I키: 전체 인벤토리
        if (Input.GetKeyDown(KeyCode.I)) ToggleUI(inventoryUI);
        // B키: 탐사 인벤토리
        if (Input.GetKeyDown(KeyCode.B)) ToggleUI(expeditionBagUI);
        // G키: 장신구 가방
        if (Input.GetKeyDown(KeyCode.G)) ToggleUI(accessoryBagUI);
    }

    public void ToggleUI(GameObject uiObject)
    {
        if (uiObject == null) return;
        bool isActive = uiObject.activeSelf;
        uiObject.SetActive(!isActive);
        
        // UI가 켜질 때마다 마우스 커서 상태 조절 (필요 시)
        if (!isActive) Cursor.lockState = CursorLockMode.None;
    }
}