using System;
using System.Collections.Generic;
using Mirror;

namespace StackObjects
{
    public static class Speed
    {
        public static readonly string FAST = "Rapida";
        public static readonly string SLOW = "Lenta";
        public static readonly string TRIGGERED = "Disparada";
    }
    [Serializable]
    public abstract class Stackable
    {
        public int InGameId = -1;
        protected int RelatedCard = -1;
        public virtual int GetRelatedCard() { return RelatedCard; }
        public string Name = "";
        public string speed = Speed.SLOW;

        public int Owner = -1;
        public int Caster = -1;
        public List<int> Targets = new();
        [NonSerialized]
        public List<Abilities.ResolutionEffect> ResolutionEffects = new();
        public bool IsCard()
        {
            return typeof(Cards.Card) == GetType();
        }
        public Player getOwner() { return GameController.instance.gameState.Players[Owner]; }
        public bool IsOwnerLocal()
        {
            return getOwner().IsLocal();
        }

    }

    [Serializable]
    public class StackItem
    {
        public int card = -1;
        public string TriggeredAbility = "";
        public string ActivatedAbility = "";
        [NonSerialized] public Stackable stackable;
        public int RelatedCardId = -1;

        public StackItem(int InGameId) 
        {
            card = InGameId;
            stackable = Cards.getCardFromID(InGameId);
        }
        public StackItem(Stackable StackThis)
        {

            stackable = StackThis;
            if (StackThis.GetType() == typeof(Abilities.TriggeredAbility)) TriggeredAbility = StackThis.Name;
            if (StackThis.GetType() == typeof(Abilities.ActivatedAbility)) ActivatedAbility = StackThis.Name;
            if (StackThis.IsCard()) card = StackThis.InGameId;
            RelatedCardId = StackThis.GetRelatedCard();
        }
    }
}