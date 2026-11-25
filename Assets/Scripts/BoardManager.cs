using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BoardManager : MonoBehaviour
{
    [Header("card pictures")]
    public Sprite heartSprite;
    public Sprite starSprite;
    public Sprite diamondSprite;
    [Tooltip("red=selected")]
    public Sprite highlightFrame;

    [Header("layout")]
    public int columns = 4;

    public float columnSpacing = 1.8f;
    // the place of 1st card each row
    public float columnTopY = 1.5f;
    //gap between cards
    public float cardSpacing = 1.5f;
    public int maxPerColumn = 9;
    //true=up to down, visaversa
    public bool growDown = true;
    //drop zone he
    public float dropZoneHeight = 2.5f;

    [Header("top area")]
    public int trayCount = 3;
    public float trayY = 4f;
    public float traySpacing = 2f;

    public bool eliminateOnFirstClick = false;

    [Header("fail check line")]
    public float loseLineY = -3.5f;
    public bool showLoseLine = true;

    [Header("Game end ui")]
    public GameObject gameOverPanel;
    public TMP_Text gameOverText;
    

    bool gameOver;

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
        if (showLoseLine) CreateLoseLine();
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    void Update()
    {
        if (SpacePressed()) ResetGame();
    }

    static bool SpacePressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && kb.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }
    
    void OnGUI()
    {
        if (!gameOver || gameOverPanel != null) return;
        var style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 36,
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = Color.white;
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height),
            "GAME OVER\nScore: " + score + "\nPress SPACE to restart", style);
    }

    void CreateLoseLine()
    {
        var go = new GameObject("LoseLine");
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = Color.red;
        lr.startWidth = lr.endWidth = 0.06f;
        lr.sortingOrder = -1;
        float halfW = columns * columnSpacing * 0.5f + 0.5f;
        lr.positionCount = 2;
        lr.SetPosition(0, new Vector3(-halfW, loseLineY, 0));
        lr.SetPosition(1, new Vector3(halfW, loseLineY, 0));
    }

    //locations
    float ColumnX(int c) => (c - (columns - 1) / 2f) * columnSpacing;

    Vector3 SlotPos(int c, int index)
    {
        float dir = growDown ? -1f : 1f;
        return new Vector3(ColumnX(c), columnTopY + dir * index * cardSpacing, 0);
    }

    Vector3 TrayPos(int slot) =>
        new Vector3((slot - (trayCount - 1) / 2f) * traySpacing, trayY, 0);

   //card attributing
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

    //drop cards
    public bool TryDrop(Card card)
    {
        if (gameOver) return false;
        Vector3 p = card.transform.position;

        //in drop area
        if (Mathf.Abs(p.y - columnTopY) > dropZoneHeight * 0.5f) return false;

        //closest row
        int best = -1;
        float bestDist = columnSpacing * 0.5f;
        for (int c = 0; c < columns; c++)
        {
            float d = Mathf.Abs(p.x - ColumnX(c));
            if (d < bestDist) { bestDist = d; best = c; }
        }
        if (best < 0) return false;
        if (columnCards[best].Count >= maxPerColumn) return false;

        //put in cards
        var list = columnCards[best];
        card.InColumn = true;
        int slot = card.TraySlot;
        card.TraySlot = -1;
        list.Add(card);
        card.SetOrder(list.Count); 
        //card will move down slowly to the tail
        card.MoveTo(SlotPos(best, list.Count - 1));

        //fill the gap of the upper zone
        trayCards[slot] = null;
        SpawnTrayCard(slot);

        CheckGameOver(best, card);
        return true;
    }

    //click=select
    public void OnCardClicked(Card card)
    {
        if (gameOver) return;
        //if selected 1 of 3 red cards = erase
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
        //if not 1 of 3 reds=no response
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

    void CheckGameOver(int col, Card placed)
    {
        //win/lose check is the final place card being put not while moving
        float halfH = placed.GetComponent<SpriteRenderer>().bounds.extents.y;
        float y = SlotPos(col, columnCards[col].Count - 1).y;
        bool crossed = growDown ? (y - halfH <= loseLineY) : (y + halfH >= loseLineY);
        if (crossed) { GameOver(); return; }

        //if every rows are full the end
        foreach (var l in columnCards)
            if (l.Count < maxPerColumn) return;
        GameOver();
    }

    void GameOver()
    {
        if (gameOver) return;
        gameOver = true;
        ClearSelection();
       
    }

    public void ResetGame()
    {
        CancelInvoke();
        ClearSelection();

        foreach (var list in columnCards)
        {
            foreach (var c in list) if (c != null) Destroy(c.gameObject);
            list.Clear();
        }
        for (int i = 0; i < trayCards.Length; i++)
        {
            if (trayCards[i] != null) Destroy(trayCards[i].gameObject);
            trayCards[i] = null;
        }

        score = 0;
        gameOver = false;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        for (int i = 0; i < trayCount; i++) SpawnTrayCard(i);
    }

    //visuals
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        float halfW = columns * columnSpacing * 0.5f + 0.5f;
        Gizmos.DrawLine(new Vector3(-halfW, loseLineY, 0), new Vector3(halfW, loseLineY, 0));

        Gizmos.color = Color.green;
        for (int c = 0; c < columns; c++)
        {
            float x = (c - (columns - 1) / 2f) * columnSpacing;
            Gizmos.DrawWireCube(new Vector3(x, columnTopY, 0),
                new Vector3(columnSpacing * 0.9f, dropZoneHeight, 0));
        }
    }
}