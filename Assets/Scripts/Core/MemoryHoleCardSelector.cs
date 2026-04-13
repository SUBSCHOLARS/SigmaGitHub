using UnityEngine;
using UnityEngine.EventSystems;

// Memory Hole パネル内の各カードにアタッチする軽量セレクター
public class MemoryHoleCardSelector : MonoBehaviour, IPointerClickHandler
{
    public CardData cardData;
    public bool isFromTarget; // true=ターゲット手札, false=自分手札
    public MemoryHolePanelController controller;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller == null) return;
        if (isFromTarget)
        {
            controller.OnTargetCardClicked(cardData, gameObject);
        }
        else
        {
            controller.OnPlayerCardClicked(cardData, gameObject);
        }
    }
}
