using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using StackObjects;
using UnityEngine;
using System.Linq;
using Card = Cards.Card;

[Serializable]
public class Player
{
    public List<int> Hand = new();
    public List<int> Kingdom = new();
    public List<int> Reserve = new();
    public List<int> Paid = new();
    public List<int> Vault = new();
    public List<int> Attackers = new();
    public List<int> Regroup = new();
    public List<int> Discard = new();
    public List<int> Avernus = new();
    public int Life;
    public bool HasPassedPriority;
    public int wantToStack = -1; 
    public int wantsToPayWith = -1;
    public int wantsToTarget = -1;
    public int wantsToAttackWith = -1;
    public int wantsToBlockWith = -1;
    public int wantsToBlockTarget = -1;
    public List<int> chosenTargets; 
    public bool HasAddedToStack;
    public int AmountToPay;
    public bool hasDeclaredAttack;
    public bool hasDeclaredBlock;
    public bool PaymentCanceled;
    public bool TargetsCancelled;  
    public bool CanStackSlowActions()
    {
        return GameController.instance.gameState.GetActivePlayer() == this &&
                new List<Phase> { Phase.MainPhase1, Phase.MainPhase2 }.Contains(GameController.instance.gameState.currentPhase);
    }
    public bool CanStackFastActions()
    {
        return true;
    }
    public bool CanPay(Cards.Card card)
    {
        return Convert.ToInt32(card.Cost) <= Reserve.Count;
    }

    public IEnumerator MustPay(int cost)
    {
        AmountToPay = cost;
        PaymentCanceled = false;
        GameController.instance.gameState.state = GameState.State.AwaitingPayment;
        yield return new WaitUntil(() => AmountToPay == 0 || PaymentCanceled);
        GameController.instance.gameState.state = GameState.State.InProgress;
    }
    // Add these new fields to your Player class
    public bool AwaitingMulliganDecision;
    public bool MulliganDecisionMade;
    public bool KeepHand;
    public int mulliganCount = 0;

    public bool AwaitingBottomDecision;
    public int CardsToBottom; 
    public int SelectedCardIdForBottom = -1;

    // Methods for handling mulligan decisions
    public void DecideToKeep()
    {
        KeepHand = true;
        MulliganDecisionMade = true;
    }

    public void DecideToMulligan()
    {
        KeepHand = false;
        MulliganDecisionMade = true;
    }

    public void SelectCardForBottom(int cardId)
    {
        Debug.Log($"Player attempted to select a card for bottom | AwaitingBottomDecision={AwaitingBottomDecision} | Hand.Contains(card)={Hand.Contains(cardId)}");
        if (AwaitingBottomDecision && Hand.Contains(cardId))
        {
            SelectedCardIdForBottom = cardId;
        }
    }

    public void ShuffleLibrary()
    {
        var rng = new System.Random();
        Kingdom = Kingdom.OrderBy(x => rng.Next()).ToList();
    }

    public Card getTopCardFrom(Zone zone) {
        return Cards.getCardFromID(GetZone(zone)[0]);
    }    
    public Card getBottomCardFrom(Zone zone) {
        return Cards.getCardFromID(GetZone(zone)[^1]);
        
    }

    public List<int> GetZone(Zone zone)
    {
        switch (zone)
        {
            case Zone.Avernus : return Avernus;
            case Zone.Kingdom : return Kingdom;
            case Zone.Attackers : return Attackers;
            case Zone.Vault : return Vault;
            case Zone.Paid : return Paid;
            case Zone.Regroup : return Regroup;
            case Zone.Discard : return Discard;
            case Zone.Hand : return Hand;
            case Zone.Reserve : return Reserve;
            default : return null;
        }   
    }
}