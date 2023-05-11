using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class UIXRDetector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private XRUIInputModule InputModule => EventSystem.current.currentInputModule as XRUIInputModule;
 
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (gameObject.layer != LayerMask.NameToLayer("UI")) return;
        XRRayInteractor interactor = InputModule.GetInteractor(eventData.pointerId) as XRRayInteractor;
        XRPlayerHand hand = interactor?.GetComponentInParent<XRPlayerHand>();
        if (hand != null)
        {
            hand.State = XRPlayerHand.InteractionState.UI;
        }
    }

	public void OnPointerExit(PointerEventData eventData)
	{
        XRRayInteractor interactor = InputModule.GetInteractor(eventData.pointerId) as XRRayInteractor;
        XRPlayerHand hand = interactor?.GetComponentInParent<XRPlayerHand>();
        if (hand != null)
        {
            hand.State = XRPlayerHand.InteractionState.Default;
        }
	}
}
