using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockersStackView : CardStackView
{
    CardView cardView;

    protected override void Awake()
    {
        base.Awake();
        cardView = transform.GetComponentInParent<CardView>();
        isOwner = !cardView.cardData.IsOwnerLocal();
    }
    protected override List<int> GetCardList(GameState _new)
    {
        return cardView.cardData.Blockers;
    }
}
