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
        public string Name;
        public string speed = Speed.SLOW;

        [NonSerialized] public List<Abilities.ResolutionEffect> ResolutionEffects = new();
        public int Owner = -1;
        public int Caster = -1;

        public bool IsCard()
        {
            return typeof(Cards.Card) == GetType();
        }

    }

    [Serializable]
    public class StackItem
    {
        public int InGameId = -1;
        public Cards.Card card;
        public Abilities.TriggeredAbility TriggeredAbility;
        public Abilities.ActivatedAbility ActivatedAbility;

        public Stackable getItem()
        {
            if (card != null) return card;
            if (TriggeredAbility != null) return TriggeredAbility;
            return ActivatedAbility;
        }

        public StackItem () {}
        public StackItem(Stackable StackThis)
        {
            if (StackThis.GetType() == typeof(Abilities.TriggeredAbility)) TriggeredAbility = (Abilities.TriggeredAbility)StackThis;
            if (StackThis.GetType() == typeof(Abilities.ActivatedAbility)) ActivatedAbility = (Abilities.ActivatedAbility)StackThis;
            if (StackThis.GetType() == typeof(Cards.Card)) card = (Cards.Card)StackThis;
        }
    }
}