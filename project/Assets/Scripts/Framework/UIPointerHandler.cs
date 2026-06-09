using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIPointerHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Serializable]
    public class Vector2Event : UnityEvent<Vector2>
    {
    }

    [SerializeField] private bool ensureRaycastTarget = true;
    [SerializeField] private float longPressDuration = 0.5f;
    [SerializeField] private float dragThreshold = 10f;
    [SerializeField] private UnityEvent onClick;
    [SerializeField] private UnityEvent onLongPress;
    [SerializeField] private UnityEvent onPointerDownEvent;
    [SerializeField] private UnityEvent onPointerUpEvent;
    [SerializeField] private UnityEvent onBeginDragEvent;
    [SerializeField] private UnityEvent onEndDragEvent;
    [SerializeField] private Vector2Event onDragEvent;

    private Graphic targetGraphic;
    private bool isPointerDown;
    private bool isLongPressTriggered;
    private bool isDragging;
    private float pointerDownTime;
    private Vector2 pointerDownPosition;

    public event Action Clicked;
    public event Action LongPressed;
    public event Action PointerDowned;
    public event Action PointerUpped;
    public event Action BeginDragged;
    public event Action EndDragged;
    public event Action<Vector2> Dragged;

    public void ClearClickListeners()
    {
        Clicked = null;
    }

    public void ClearPointerDownListeners()
    {
        PointerDowned = null;
    }

    public void ClearPointerUpListeners()
    {
        PointerUpped = null;
    }

    public void ClearPressListeners()
    {
        LongPressed = null;
    }

    public void ClearBeginDragListeners()
    {
        BeginDragged = null;
    }

    public void ClearEndDragListeners()
    {
        EndDragged = null;
    }

    public void ClearDragListeners()
    {
        Dragged = null;
    }

    private void Awake()
    {
        EnsureRaycastTarget();
    }

    private void OnValidate()
    {
        EnsureRaycastTarget();
    }

    private void Update()
    {
        if (!isPointerDown || isDragging || isLongPressTriggered)
        {
            return;
        }

        if (Time.unscaledTime - pointerDownTime < longPressDuration)
        {
            return;
        }

        isLongPressTriggered = true;
        onLongPress?.Invoke();
        LongPressed?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDragging || isLongPressTriggered)
        {
            return;
        }

        onClick?.Invoke();
        Clicked?.Invoke();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
        isLongPressTriggered = false;
        isDragging = false;
        pointerDownTime = Time.unscaledTime;
        pointerDownPosition = eventData.position;
        onPointerDownEvent?.Invoke();
        PointerDowned?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;
        onPointerUpEvent?.Invoke();
        PointerUpped?.Invoke();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!ShouldStartDrag(eventData))
        {
            return;
        }

        isDragging = true;
        onBeginDragEvent?.Invoke();
        BeginDragged?.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            if (!ShouldStartDrag(eventData))
            {
                return;
            }

            isDragging = true;
            onBeginDragEvent?.Invoke();
            BeginDragged?.Invoke();
        }

        onDragEvent?.Invoke(eventData.position);
        Dragged?.Invoke(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;
        isPointerDown = false;
        onEndDragEvent?.Invoke();
        EndDragged?.Invoke();
    }

    private bool ShouldStartDrag(PointerEventData eventData)
    {
        return Vector2.Distance(pointerDownPosition, eventData.position) >= dragThreshold;
    }

    private void EnsureRaycastTarget()
    {
        if (!ensureRaycastTarget)
        {
            return;
        }

        if (targetGraphic == null)
        {
            targetGraphic = GetComponent<Graphic>();
        }

        if (targetGraphic != null)
        {
            targetGraphic.raycastTarget = true;
        }
    }
}
