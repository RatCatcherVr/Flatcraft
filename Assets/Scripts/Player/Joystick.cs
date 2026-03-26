using UnityEngine;
using UnityEngine.EventSystems;

public class Joystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    public RectTransform handle;
    private Vector2 inputVector;

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out pos
        );

        pos.x = (pos.x / ((RectTransform)transform).sizeDelta.x);
        pos.y = (pos.y / ((RectTransform)transform).sizeDelta.y);

        inputVector = new Vector2(pos.x * 2, pos.y * 2);
        inputVector = (inputVector.magnitude > 1) ? inputVector.normalized : inputVector;

        handle.anchoredPosition = new Vector2(
            inputVector.x * ((RectTransform)transform).sizeDelta.x / 2,
            inputVector.y * ((RectTransform)transform).sizeDelta.y / 2
        );
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
    }

    public float Horizontal() => inputVector.x;
    public float Vertical() => inputVector.y;
}