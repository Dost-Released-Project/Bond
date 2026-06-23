using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Shapes;
using Bond.UI;

public class BuffIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private CharacterSlotBuffBar _bar;
    private string _tooltipDescription;

    public void Setup(string tooltipText, CharacterSlotBuffBar bar)
    {
        _tooltipDescription = tooltipText;
        _bar = bar;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"[BuffIcon] OnPointerEnter detected! Text: {_tooltipDescription}");
        if (string.IsNullOrEmpty(_tooltipDescription)) return;

        var uiDocs = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        UIDocument activeDoc = null;
        foreach (var doc in uiDocs)
        {
            if (doc.isActiveAndEnabled && doc.rootVisualElement != null)
            {
                activeDoc = doc;
                break;
            }
        }

        if (activeDoc != null)
        {
            var root = activeDoc.rootVisualElement;
            Vector2 screenPos = new Vector2(eventData.position.x, Screen.height - eventData.position.y);
            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(root.panel, screenPos);
            
            Debug.Log($"[BuffIcon] Displaying tooltip via UI Toolkit. Target Doc: {activeDoc.name}, Panel Pos: {panelPos}");
            
            var tooltipLabel = TooltipPopup.BuildText(_tooltipDescription);
            TooltipPopup.ShowAt(tooltipLabel, panelPos, root);
        }
        else
        {
            Debug.LogWarning("[BuffIcon] Active UIDocument not found in scene!");
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("[BuffIcon] OnPointerExit detected.");
        var uiDocs = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        foreach (var doc in uiDocs)
        {
            if (doc.isActiveAndEnabled && doc.rootVisualElement != null)
            {
                TooltipPopup.Hide(doc.rootVisualElement.panel);
            }
        }
    }
}
