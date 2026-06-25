using System;
using System.Collections;
using System.Collections.Generic;
using StackObjects;

public static class Costs
{
    public delegate IEnumerator ResolveCostDelegate(Player player, Stackable card);

    [NonSerialized]

    public static readonly string pay2life = "Pay2Life";
    public static readonly string discardACard = "DiscardACard";
    public static readonly string pay1Gold = "Pay1Gold";
    public static readonly string destroySelf = "DestroySelf";

    private static IEnumerator Pay2Life(Player player, Stackable card)
    {
        player.Life -= 2;
        yield return null;
    }

    private static IEnumerator Pay1Gold(Player player, Stackable card)
    {
        player.AmountToPay = 1;
        player.PaymentCanceled = false;
        GameController.instance.gameState.state = State.AwaitingPayment;
        yield return new UnityEngine.WaitUntil(() => player.AmountToPay == 0 || player.PaymentCanceled);
        GameController.instance.gameState.state = State.InProgress;
    }

    private static IEnumerator DestroySelf(Player player, Stackable card)
    {
        yield return GameController.instance.MoveCard(card.InGameId, Zone.Discard);
    }

    private static IEnumerator DiscardACard(Player player, Stackable card)
    {
        player.AwaitingDiscard = true;
        player.wantsToDiscard = -1;
        player.PaymentCanceled = false;
        // Reusing AwaitingPayment state or creating a new one. We can just use AwaitingPayment for general prompt
        GameController.instance.gameState.state = State.AwaitingPayment;
        
        yield return new UnityEngine.WaitUntil(() => player.wantsToDiscard != -1 || player.PaymentCanceled);
        
        if (!player.PaymentCanceled && player.wantsToDiscard != -1)
        {
            yield return GameController.instance.MoveCard(player.wantsToDiscard, Zone.Discard);
        }
        
        player.AwaitingDiscard = false;
        player.wantsToDiscard = -1;
        GameController.instance.gameState.state = State.InProgress;
    }

    public static readonly Dictionary<string, ResolveCostDelegate> CostResolvers = new()
    {
        { pay2life, Pay2Life },
        { pay1Gold, Pay1Gold },
        { destroySelf, DestroySelf },
        { discardACard, DiscardACard }
    };

}
