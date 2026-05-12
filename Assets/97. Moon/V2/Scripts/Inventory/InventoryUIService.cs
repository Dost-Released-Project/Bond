using UnityEngine;
using UnityEngine.UIElements;

public class InventoryUIService
{
    // 드래그 상태 관리
    public int CurrentDraggingIndex { get; private set; } = -1;
    public IInventory CurrentSourceInventory { get; private set; } = null;
    public VisualElement GlobalDragGhost { get; private set; }

    public void StartDrag(IInventory inv, int index, Sprite icon, VisualElement ghostElement, Vector2 initialPos, Vector2 offset)
    {
        CurrentDraggingIndex = index;
        CurrentSourceInventory = inv;
        GlobalDragGhost = ghostElement;
    
        if (GlobalDragGhost != null)
        {
            GlobalDragGhost.style.backgroundImage = new StyleBackground(icon);
        
            // [수정] 가시화하기 전에 위치를 먼저 설정하여 잔상 현상 제거
            GlobalDragGhost.style.left = initialPos.x - offset.x;
            GlobalDragGhost.style.top = initialPos.y - offset.y;
        
            GlobalDragGhost.style.visibility = Visibility.Visible;
        }
    }

    public void ResetDrag()
    {
        CurrentDraggingIndex = -1;
        CurrentSourceInventory = null;
        if (GlobalDragGhost != null)
        {
            GlobalDragGhost.style.visibility = Visibility.Hidden;
        }
    }

    public void UpdateGhostPosition(Vector2 pos, Vector2 offset)
    {
        if (GlobalDragGhost != null)
        {
            GlobalDragGhost.style.left = pos.x - offset.x;
            GlobalDragGhost.style.top = pos.y - offset.y;
        }
    }
}