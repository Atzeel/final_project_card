using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("Card Pattern")]
    public Sprite heartSprite;
    public Sprite starSprite;
    public Sprite diamondSprite;
    [Tooltip("red means selecting card")]
    public Sprite highlightFrame;

    [Header("Layout")]
    public int columns = 4;
    public float columnSpacing = 1.8f;
    public float columnTopY = 1.5f;       // first card inevery row's height 
    public float cardSpacing = 2f;      //gap btween cards）
    public int maxPerColumn = 8;
    public bool growDown = true;          // true=up to down；false=visa versa
    public float dropZoneHeight = 2.5f;   // drop card zone's height

    [Header("UpperPart")]
    public int trayCount = 3;
    public float trayY = 4f;
    public float traySpacing = 2f;

    [Header("Rules")]
    [Tooltip("click and select, 2 click erase the cards")]
    public bool eliminateOnFirstClick = false;

    List<Card>[] columnCards;
    Card[] trayCards;
    List<Card> selected = new List<Card>();
    int score;

    void Awake()
    {
        columnCards = new List<Card>[columns];
        for (int i = 0; i < columns; i++) columnCards[i] = new List<Card>();
        trayCards = new Card[trayCount];
    }

    void Start()
    {
        for (int i = 0; i < trayCount; i++) SpawnTrayCard(i);
    }
    
    //
    float ColumnX(int c) => (c - (columns - 1) / 2f) * columnSpacing;

    Vector3 SlotPos(int c, int index)
    {
        float dir = growDown ? -1f : 1f;
        return new Vector3(ColumnX(c), columnTopY + dir * index * cardSpacing, 0);
    }

    Vector3 TrayPos(int slot) =>
        new Vector3((slot - (trayCount - 1) / 2f) * traySpacing, trayY, 0);

   //card playing (sorting)
    void SpawnTrayCard(int slot)
    {
        var type = (CardType)Random.Range(0, 3);
        var go = new GameObject("Card_" + type);
        go.AddComponent<SpriteRenderer>();
        go.AddComponent<BoxCollider2D>();
        var card = go.AddComponent<Card>();
        card.Init(type, SpriteOf(type), highlightFrame, this);
        card.TraySlot = slot;
        card.HomePos = TrayPos(slot);
        go.transform.position = card.HomePos;
        trayCards[slot] = card;
    }

    Sprite SpriteOf(CardType t)
    {
        switch (t)
        {
            case CardType.Heart: return heartSprite;
            case CardType.Star: return starSprite;
            default: return diamondSprite;
        }
    }

    //set card into different rows
    public bool TryDrop(Card card)
    {
        Vector3 p = card.transform.position;

        //in card drop zone's height
        if (Mathf.Abs(p.y - columnTopY) > dropZoneHeight * 0.5f) return false;

        //closest row finding
        int best = -1;
        float bestDist = columnSpacing * 0.5f;
        for (int c = 0; c < columns; c++)
        {
            float d = Mathf.Abs(p.x - ColumnX(c));
            if (d < bestDist) { bestDist = d; best = c; }
        }
        if (best < 0) return false;
        if (columnCards[best].Count >= maxPerColumn) return false;

        //put the card in
        var list = columnCards[best];
        card.InColumn = true;
        int slot = card.TraySlot;
        card.TraySlot = -1;
        list.Add(card);
        //earlier = below
        card.SetOrder(list.Count);
        //after being put in row, going down slowly
        card.MoveTo(SlotPos(best, list.Count - 1)); 

        //fill in previous card deck's gap
        trayCards[slot] = null;
        SpawnTrayCard(slot);

        CheckGameOver();
        return true;
    }

    //select
    public void OnCardClicked(Card card)
    {
        //if click the selected 3 cards=erase
        if (selected.Contains(card))
        {
            EliminateSelected();
            return;
        }

        ClearSelection();

        int c = ColumnOf(card);
        if (c < 0) return;
        int idx = columnCards[c].IndexOf(card);
        int start = FindTripleStart(columnCards[c], idx);
        //if not selected=nothing returns
        if (start < 0) return;

        for (int i = 0; i < 3; i++)
        {
            var cd = columnCards[c][start + i];
            cd.SetSelected(true);
            selected.Add(cd);
        }

        if (eliminateOnFirstClick) EliminateSelected();
    }

    int ColumnOf(Card card)
    {
        for (int c = 0; c < columns; c++)
            if (columnCards[c].Contains(card)) return c;
        return -1;
    }
    
    int FindTripleStart(List<Card> list, int idx)
    {
        for (int s = Mathf.Max(0, idx - 2); s <= idx; s++)
        {
            if (s + 2 >= list.Count) break;
            if (list[s].Type == list[s + 1].Type && list[s].Type == list[s + 2].Type)
                return s;
        }
        return -1;
    }

    void ClearSelection()
    {
        foreach (var c in selected) if (c != null) c.SetSelected(false);
        selected.Clear();
    }

    void EliminateSelected()
    {
        var touchedCols = new HashSet<int>();
        foreach (var card in selected)
        {
            int c = ColumnOf(card);
            if (c >= 0) { columnCards[c].Remove(card); touchedCols.Add(c); }
            Destroy(card.gameObject);
        }
        selected.Clear();
        score += 3;
        
        foreach (int c in touchedCols) Relayout(c);
    }

    void Relayout(int c)
    {
        var list = columnCards[c];
        for (int i = 0; i < list.Count; i++)
        {
            list[i].SetOrder(i + 1);
            list[i].MoveTo(SlotPos(c, i));
        }
    }

    void CheckGameOver()
    {
        foreach (var l in columnCards)
            if (l.Count < maxPerColumn) return;
    }

    //card spot frame
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        for (int c = 0; c < columns; c++)
        {
            float x = (c - (columns - 1) / 2f) * columnSpacing;
            Gizmos.DrawWireCube(new Vector3(x, columnTopY, 0),
                new Vector3(columnSpacing * 0.9f, dropZoneHeight, 0));
        }
    }
}