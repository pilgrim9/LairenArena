using System;
using System.Collections;
using System.Collections.Generic;
using StackObjects;

public class Abilities
{
    public delegate IEnumerator ResolveEffectDelegate(Effect effect, List<int> targets);

    public static readonly Dictionary<EffectType, ResolveEffectDelegate> EffectResolvers = new()
    {
        { EffectType.Damage, ResolveDamageEffect },
        { EffectType.GrantKeyword, ResolveGrantKeywordEffect },
        { EffectType.LoseLife, ResolveLoseLifeEffect },
        { EffectType.Destroy, ResolveDestroyEffect }
    };

    public static readonly Dictionary<string, Ability> AllAbilities = new()
    {
        { "DealDamage", new Ability()
            {
                Effects = new List<Effect>()
                {
                    new Effect()
                    {
                        Type = EffectType.Damage,
                        Amount = 3,
                        ValidTargets = new TargetInfo()
                        {
                            Type = TargetType.CardInZone,
                            Zone = Zone.Regroup,
                            CardTypes = new List<string>() { "Ally" },
                            MaxPower = 3,
                            CanTargetOpponent = true
                        }
                    }
                }
            }
        },
        {
            "BrujaElementalistaTrigger", new Ability()
            {
                Trigger = GameEvent.OnOrderPlayed,
                Effects = new List<Effect>()
                {
                    new Effect()
                    {
                        Type = EffectType.Damage,
                        Amount = 1,
                        ValidTargets = new TargetInfo()
                        {
                            Type = TargetType.Player,
                            CanTargetOpponent = true
                        }
                    }
                }
            }
        },
        {
            "MuerteInminente", new Ability()
            {
                Effects = new List<Effect>()
                {
                    new Effect()
                    {
                        Type = EffectType.Destroy,
                        ValidTargets = new TargetInfo()
                        {
                            Type = TargetType.CardInZone,
                            Zone = Zone.Regroup,
                            CardTypes = new List<string>() { "Ally" },
                            CanTargetOpponent = true
                        }
                    }
                }
            }
        },
        {
            "RojoFugazStatic", new Ability()
            {
                Effects = new List<Effect>()
                {
                    new Effect()
                    {
                        Type = EffectType.GrantKeyword,
                        Keyword = Keyword.Frenzy,
                        ValidTargets = new TargetInfo()
                        {
                            Type = TargetType.CardInZone,
                            Zone = Zone.Regroup,
                            CardTypes = new List<string>() { "Animal" },
                            CanTargetSelf = true
                        }
                    }
                }
            }
        },
        {
            "SombraDelDesiertoTrigger", new Ability()
            {
                Trigger = GameEvent.OnCardEntersBattlefield,
                Effects = new List<Effect>()
                {
                    new Effect()
                    {
                        Type = EffectType.LoseLife,
                        Amount = 1,
                        ValidTargets = new TargetInfo()
                        {
                            Type = TargetType.Player,
                            CanTargetOpponent = true
                        }
                    }
                }
            }
        }
    };

    [Serializable]
    public class Ability : Stackable
    {
        public List<Effect> Effects;
        public GameEvent Trigger;
    }

    [Serializable]
    public class Effect
    {
        public EffectType Type;
        public int Amount;
        public Keyword Keyword;
        public TargetInfo ValidTargets;
    }

    public enum EffectType
    {
        Damage,
        DrawCard,
        GainLife,
        GrantKeyword,
        LoseLife,
        Destroy
    }

    private static IEnumerator ResolveDamageEffect(Effect effect, List<int> targets)
    {
        foreach (var targetId in targets)
        {
            Cards.getCardFromID(targetId).Damage += effect.Amount;
        }
        yield return null;
    }

    private static IEnumerator ResolveDestroyEffect(Effect effect, List<int> targets)
    {
        foreach (var targetId in targets)
        {
            GameController.instance.MoveCard(targetId, Zone.Discard);
        }
        yield return null;
    }

    private static IEnumerator ResolveLoseLifeEffect(Effect effect, List<int> targets)
    {
        foreach (var targetId in targets)
        {
            GameController.instance.gameState.Players[targetId].Life -= effect.Amount;
        }
        yield return null;
    }

    private static IEnumerator ResolveGrantKeywordEffect(Effect effect, List<int> targets)
    {
        foreach (var targetId in targets)
        {
            var card = Cards.getCardFromID(targetId);
            if (!card.Keywords.Contains(effect.Keyword))
            {
                card.Keywords.Add(effect.Keyword);
            }
        }
        yield return null;
    }
}
