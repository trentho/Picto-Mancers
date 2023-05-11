using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
// using Unity

public class UIHoverEvent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float EnterTime = 0f;
    public UnityEvent OnEnter;
    public UnityEvent OnExit;

    public bool IsHovering { get; private set; }
    private bool didEnter = false;
    private float enterTimer = 0f;
 
    public void OnPointerEnter(PointerEventData eventData)
    {
        didEnter = true;
        enterTimer = 0f;
    }

	public void OnPointerExit(PointerEventData eventData)
	{
        didEnter = false;
        IsHovering = false;
        OnExit.Invoke();
	}

    void Update()
    {
        enterTimer += Time.deltaTime;
        if (didEnter && !IsHovering && enterTimer > EnterTime)
        {
            OnEnter.Invoke();
            IsHovering = true;
        }
    }
}
