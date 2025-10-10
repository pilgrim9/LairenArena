using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockersStackView : CardStackView
{
    CardView cardView;
    void Awake()
    {
        cardView = transform.GetComponentInParent<CardView>();
    }
    protected override List<int> GetCardList(GameState _new)
    {
        return cardView.cardData.Blockers;
    }
}
