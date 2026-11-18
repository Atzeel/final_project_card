using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class ConveyorCard : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int cardType;                   // 0 is heart 1 is diamond 2 is star

    public bool IsDragging { get; private set; }
    public Vector2 TrackPosition { get; private set; }

    private RectTransform rect;
    private CanvasGroup group;
    private CardConveyor conveyor;
    private Vector2 mouseOffset;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        group = GetComponent<CanvasGroup>();
    }

    public void Setup(CardConveyor owner, Vector2 position)
    {
        conveyor = owner;
        SetPosition(position);
    }

    public void SetPosition(Vector2 position)
    {
        TrackPosition = position;          
        rect.anchoredPosition = position;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        IsDragging = true;

        transform.SetAsLastSibling();      
        group.blocksRaycasts = false;      

        RectTransform parent = rect.parent as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 mousePosition
        );

        mouseOffset = rect.anchoredPosition - mousePosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsDragging)
            return;

        RectTransform parent = rect.parent as RectTransform;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 mousePosition))
        {
            rect.anchoredPosition = mousePosition + mouseOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!IsDragging)
            return;

        IsDragging = false;
        group.blocksRaycasts = true;

        if (conveyor == null)
            return;                        // can move already picked cards

        if (rect.anchoredPosition.y <= conveyor.releaseY)
        {
            ReleaseFromConveyor();         // picked
        }
        else
        {
            rect.anchoredPosition = TrackPosition;
            conveyor.RestoreStackOrder();  // not picked, put back
        }
    }

    public void ReleaseFromConveyor()
    {
        if (conveyor == null)
            return;

        conveyor.RemoveCard(this);
        conveyor = null;                   // not controlled by conveyor
    }

    void OnDestroy()
    {
        if (conveyor != null)
            conveyor.RemoveCard(this);
    }
}