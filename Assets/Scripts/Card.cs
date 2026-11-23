using UnityEngine;
using UnityEngine.EventSystems;

public enum CardType { Heart, Star, Diamond }

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class Card : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public CardType Type { get; private set; }
    public bool InColumn { get; set; }
    public int TraySlot { get; set; } = -1;
    public Vector3 HomePos { get; set; }

    public float moveSpeed = 8f;

    BoardManager board;
    SpriteRenderer sr;
    SpriteRenderer highlightSr;
    Sprite highlightFrame;

    Vector3 target;
    bool moving;
    bool dragging;
    Vector3 dragOffset;
    int baseOrder;

    public void Init(CardType type, Sprite face, Sprite frame, BoardManager owner)
    {
        Type = type;
        board = owner;
        highlightFrame = frame;

        sr = GetComponent<SpriteRenderer>();
        sr.sprite = face;

        var box = GetComponent<BoxCollider2D>();
        box.size = face.bounds.size;

        //red filter
        if (frame != null)
        {
            var go = new GameObject("Highlight");
            go.transform.SetParent(transform, false);
            highlightSr = go.AddComponent<SpriteRenderer>();
            highlightSr.sprite = frame;
            highlightSr.color = Color.red;
            go.SetActive(false);
        }
        SetOrder(0);
    }

    public void SetOrder(int order)
    {
        baseOrder = order;
        sr.sortingOrder = order;
        if (highlightSr != null) highlightSr.sortingOrder = order + 1;
    }

    public void SetSelected(bool selected)
    {
        if (highlightSr != null) highlightSr.gameObject.SetActive(selected);
        else sr.color = selected ? new Color(1f, 0.45f, 0.45f) : Color.white;
    }

    public void MoveTo(Vector3 worldPos)
    {
        target = worldPos;
        moving = true;
    }

    void Update()
    {
        if (!moving || dragging) return;
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        if ((transform.position - target).sqrMagnitude < 1e-6f) moving = false;
    }

    Vector3 ScreenToWorld(Vector2 screen)
    {
        var cam = Camera.main;
        var p = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
        p.z = 0;
        return p;
    }

    //draging
    public void OnBeginDrag(PointerEventData e)
    {
        if (InColumn) return; //the card already in the row cant be dragged
        dragging = true;
        moving = false;
        dragOffset = transform.position - ScreenToWorld(e.position);
        SetOrder(1000);
    }

    public void OnDrag(PointerEventData e)
    {
        if (!dragging) return;
        transform.position = ScreenToWorld(e.position) + dragOffset;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!dragging) return;
        dragging = false;
        if (!board.TryDrop(this))
        {
            SetOrder(0);
            MoveTo(HomePos); //not in correct place-back to upper part
        }
    }

    //click
    public void OnPointerClick(PointerEventData e)
    {
        if (e.dragging) return;
        if (InColumn) board.OnCardClicked(this);
    }
}