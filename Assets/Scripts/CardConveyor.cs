using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardConveyor : MonoBehaviour
{
    public RectTransform cardPrefab;
    public RectTransform cardParent;
    public Sprite[] cardSprites;

    public float spawnInterval = 2f;       
    public float moveSpeed = 180f;         

    public float spawnX = 700f;
    public float beltY = 350f;
    public float stopX = -700f;

    public int stackCapacity = 5;          // 5 cards at left
    public float stackOffset = 30f;        // card stack gap
    public float cardGap = 20f;            // conveyor gap

    public float releaseY = 160f;          // check if card is picked

    private float timer;
    private float spacing;
    private List<int> cardBag = new List<int>();

    private List<ConveyorCard> movingCards = new List<ConveyorCard>();
    private List<ConveyorCard> stackedCards = new List<ConveyorCard>();

    void Start()
    {
        if (cardPrefab == null || cardParent == null ||
            cardSprites == null || cardSprites.Length == 0)
        {
            Debug.LogError("CardConveyor：请先填好模板、Canvas 和卡牌图片。");
            enabled = false;
            return;
        }

        spacing = cardPrefab.rect.width + cardGap;
        SpawnCard();
    }

    void Update()
    {
        movingCards.RemoveAll(card => card == null);
        stackedCards.RemoveAll(card => card == null);

        UpdateStack();
        UpdateConveyor();

        timer += Time.deltaTime;

        if (timer >= spawnInterval && EntranceIsClear())
        {
            SpawnCard();
            timer = 0f;
        }
    }

    void UpdateStack()
    {
        for (int i = 0; i < stackedCards.Count; i++)
        {
            ConveyorCard card = stackedCards[i];

            if (card.IsDragging)
                continue;                  // keep the stack gap while picking

            Vector2 target = new Vector2(
                stopX + i * stackOffset,
                beltY
            );

            Vector2 position = Vector2.MoveTowards(
                card.TrackPosition,
                target,
                moveSpeed * 3f * Time.deltaTime
            );

            card.SetPosition(position);    // refill the stack gap
        }
    }

    void UpdateConveyor()
    {
        for (int i = 0; i < movingCards.Count; i++)
        {
            ConveyorCard card = movingCards[i];

            if (card.IsDragging)
                continue;

            bool hasSpace = stackedCards.Count < stackCapacity;

            float targetX;

            if (hasSpace)
            {
                targetX = stopX + stackedCards.Count * stackOffset;
            }
            else
            {
                targetX = stopX
                    + (stackCapacity - 1) * stackOffset
                    + spacing;             // stack full, cards waiting on conveyor
            }

            if (i > 0)
            {
                float previousX = movingCards[i - 1].TrackPosition.x;
                targetX = Mathf.Max(targetX, previousX + spacing);
            }

            // only going left, not going right
            float nextX = Mathf.MoveTowards(
                card.TrackPosition.x,
                Mathf.Min(card.TrackPosition.x, targetX),
                moveSpeed * Time.deltaTime
            );

            card.SetPosition(new Vector2(nextX, beltY));

            if (i == 0 && hasSpace &&
                Mathf.Abs(nextX - targetX) < 0.1f)
            {
                movingCards.RemoveAt(i);
                stackedCards.Add(card);

                RestoreStackOrder();
                i--;                       // list shorten, go process on next card
            }
        }
    }

    bool EntranceIsClear()
    {
        if (movingCards.Count == 0)
            return true;

        ConveyorCard lastCard = movingCards[movingCards.Count - 1];

        return lastCard.TrackPosition.x <= spawnX - spacing;
    }

    int DrawCardType()
    {
        // 袋子空了，就把 Inspector 里每种牌的编号各放进去一次 if bag empty put all # card in inspector again
        if (cardBag.Count == 0)
        {
            for (int i = 0; i < cardSprites.Length; i++)
            {
                cardBag.Add(i);
            }
        }

        // 随机选袋子里的一个位置，拿出那个位置上的牌。randomly pick a card from bag
        int bagIndex = Random.Range(0, cardBag.Count);
        int cardType = cardBag[bagIndex];
        cardBag.RemoveAt(bagIndex);

        return cardType;
    }
    
    void SpawnCard()
    {
        RectTransform newCard = Instantiate(cardPrefab, cardParent);

        // use center
        newCard.anchorMin = new Vector2(0.5f, 0.5f);
        newCard.anchorMax = new Vector2(0.5f, 0.5f);
        newCard.pivot = new Vector2(0.5f, 0.5f);
        newCard.localScale = Vector3.one;

        int randomIndex = DrawCardType();

        newCard.GetComponent<Image>().sprite = cardSprites[randomIndex];

        ConveyorCard card = newCard.GetComponent<ConveyorCard>();

        card.cardType = randomIndex;
        card.Setup(this, new Vector2(spawnX, beltY));

        movingCards.Add(card);
    }

    public void RemoveCard(ConveyorCard card)
    {
        movingCards.Remove(card);
        stackedCards.Remove(card);
        RestoreStackOrder();
    }

    public void RestoreStackOrder()
    {
        foreach (ConveyorCard card in stackedCards)
        {
            if (card != null && !card.IsDragging)
                card.transform.SetAsLastSibling();
        }

        // picked card show on the top layer
        foreach (ConveyorCard card in stackedCards)
        {
            if (card != null && card.IsDragging)
                card.transform.SetAsLastSibling();
        }

        foreach (ConveyorCard card in movingCards)
        {
            if (card != null && card.IsDragging)
                card.transform.SetAsLastSibling();
        }
    }
}