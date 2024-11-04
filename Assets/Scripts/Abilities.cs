using System;
using System.Collections.Generic;
using StackObjects;

public class Abilities
{


    public static readonly Dictionary<string, StaticAbility> StaticAbilities = new()
    {
        { "ROJO FUGAZ", new RojoFugazStaticAbility() }
    };
    public static readonly Dictionary<string, ActivatedAbility> ActivatedAbilities = new()
    {
    };
    public static readonly Dictionary<string, TriggeredAbility> TriggeredAbilities = new()
    {
    };
    
    [Serializable]
    public class TriggeredAbility : Stackable
    {
        public Triggers[] Triggers;
        public bool OncePerTurn;
    }

    [Serializable]
    public class ActivatedAbility : Stackable
    {
        public Zone[] PlayableFrom = { Zone.Attackers, Zone.Regroup};
    }
    [Serializable]
    public class StaticAbility
    {
        
    }
    public delegate void ResolutionEffect(Stackable self);

    public static void Play(Stackable self)
    {
        Cards.Card card = (Cards.Card)self;
        GameController.instance.gameState.Players[card.Owner].GetZone(card.OnResolutionTargetZone).Add(card);
    }
    
    public static void Discard(Stackable self)
    {
        
    }

    
    public class RojoFugazStaticAbility : StaticAbility
    {
        
    }
}
