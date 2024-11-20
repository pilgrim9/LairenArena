using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using StackObjects;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using Card = Cards.Card;

[Serializable]
public class Player
{
    public List<Card> Kingdom = new List<Card>();
    public List<Card> Hand = new List<Card>();
    public List<Card> Reserve = new();
    public List<Card> Paid = new();
    public List<Card> Vault = new();
    public List<Card> Attackers = new();
    public List<Card> Regroup = new();
    public List<Card> Discard = new();
    public List<Card> Avernus = new();
    public int Life;
    public bool HasPassedPriority;
    public StackItem AddToStack;
    public bool HasAddedToStack;
    public int AmountToPay;
    public bool PaymentCanceled;
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
        yield return new WaitUntil(() => AmountToPay == 0 || PaymentCanceled);
    }
    // Add these new fields to your Player class
    public bool AwaitingMulliganDecision { get; set; }
    public bool MulliganDecisionMade { get; set; }
    public bool KeepHand { get; set; }
    public int mulliganCount { get; set; } = 1;

    public bool AwaitingBottomDecision { get; set; }
    public int CardsToBottom { get; set; }
    public Card SelectedCardForBottom { get; set; }

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

    public void SelectCardForBottom(Card card)
    {
        if (AwaitingBottomDecision && Hand.Contains(card))
        {
            SelectedCardForBottom = card;
        }
    }

    public List<Card> GetZone(Zone zone)
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