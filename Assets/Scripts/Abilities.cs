using System;
using System.Collections;
using System.Collections.Generic;
using StackObjects;

public class Abilities
{
    public delegate IEnumerator ResolveEffectDelegate(Effect effect, Dictionary<int, int> targets, Stackable stackable);

    public static readonly Dictionary<EffectType, ResolveEffectDelegate> EffectResolvers = new()
    {
        { EffectType.Damage, ResolveDamageEffect },
        { EffectType.GrantKeyword, ResolveGrantKeywordEffect },
        { EffectType.GrantTemporaryKeyword, ResolveGrantTemporaryKeywordEffect },
        { EffectType.LoseLife, ResolveLoseLifeEffect },
        { EffectType.Destroy, ResolveDestroyEffect },
        { EffectType.AddCounters, ResolveAddCountersEffect },
        { EffectType.Drain, ResolveDrainEffect },
        { EffectType.ReturnToBattlefield, ResolveReturnToBattlefieldEffect }
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
                Trigger = GameEvent.OnAnotherCardEntersBattlefield,
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
        },
        {
            "AncianaMaestra", new Ability()
            {
                Trigger = GameEvent.OnSelfEntersBattlefield,
                Effects = new List<Effect>()
                {
                    new Effect()
                    {
                        Type = EffectType.ReturnToBattlefield,
                        ValidTargets = new TargetInfo()
                        {
                            Type = TargetType.CardInZone,
                            Zone = Zone.Discard,
                            CardSubtypes = new List<string>() { "Bruja" },
                            CanTargetSelf = true,
                            MaxTargets = 2
                        }
                    },
                    new Effect()
                    {
                        Type = EffectType.AddCounters,
                        Counter = CounterType.PlusOnePlusOne,
                        Amount = 2
                    },
                    new Effect()
                    {
                        Type = EffectType.GrantTemporaryKeyword,
                        Keyword = Keyword.Frenzy
                    }
                }
            }
        },
        {
            "FelinoDeLaMontana", new Ability()
            {
                Trigger = GameEvent.OnSelfEntersBattlefield,
                Effects = new List<Effect>()
                {
                    new Effect()
                    {
                        Type = EffectType.AddCounters,
                        Counter = CounterType.PlusOnePlusOne,
                        Amount = 2,
                        ValidTargets = new TargetInfo()
                        {
                            Type = TargetType.CardInZone,
                            Zone = Zone.Regroup,
                            CanTargetSelf = true,
                            MaxTargets = 2,
                            AmountToDistribute = 2
                        }
                    }
                }
            }
        },
        {
            "Cascabufalo", new Ability()
            {
                Trigger = GameEvent.OnCardDefeated,
                Effects = new List<Effect>()
                {
                    new Effect()
                    {
                        Type = EffectType.Drain,
                        Amount = 1
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
        public CounterType Counter;
        public TargetInfo ValidTargets;
    }

    public enum EffectType
    {
        Damage,
        DrawCard,
        GainLife,
        GrantKeyword,
        GrantTemporaryKeyword,
        LoseLife,
        Destroy,
        AddCounters,
        Drain,
        ReturnToBattlefield,
    }

    private static IEnumerator ResolveDamageEffect(Effect effect, Dictionary<int, int> targets, Stackable stackable)
    {
        foreach (var (targetId, _) in targets)
        {
            Cards.getCardFromID(targetId).Damage += effect.Amount;
        }
        yield return null;
    }

    private static IEnumerator ResolveGrantTemporaryKeywordEffect(Effect effect, Dictionary<int, int> targets, Stackable stackable)
    {
        foreach (var (targetId, _) in targets)
        {
            var card = Cards.getCardFromID(targetId);
            if (!card.TemporaryKeywords.Contains(effect.Keyword))
            {
                card.TemporaryKeywords.Add(effect.Keyword);
            }
        }
        yield return null;
    }

    private static IEnumerator ResolveDestroyEffect(Effect effect, Dictionary<int, int> targets, Stackable stackable)
    {
        foreach (var (targetId, _) in targets)
        {
            GameController.instance.MoveCard(targetId, Zone.Discard);
        }
        yield return null;
    }

    private static IEnumerator ResolveLoseLifeEffect(Effect effect, Dictionary<int, int> targets, Stackable stackable)
    {
        foreach (var (targetId, _) in targets)
        {
            GameController.instance.gameState.Players[targetId].Life -= effect.Amount;
        }
        yield return null;
    }

    private static IEnumerator ResolveGrantKeywordEffect(Effect effect, Dictionary<int, int> targets, Stackable stackable)
    {
        foreach (var (targetId, _) in targets)
        {
            var card = Cards.getCardFromID(targetId);
            if (!card.Keywords.Contains(effect.Keyword))
            {
                card.Keywords.Add(effect.Keyword);
            }
        }
        yield return null;
    }

    private static IEnumerator ResolveAddCountersEffect(Effect effect, Dictionary<int, int> targets, Stackable stackable)
    {
        foreach (var (targetId, amount) in targets)
        {
            var card = Cards.getCardFromID(targetId);
            int amountToAdd = effect.ValidTargets.AmountToDistribute > 0 ? amount : effect.Amount;
            card.AddCounters(effect.Counter, amountToAdd);
        }
        yield return null;
    }

    private static IEnumerator ResolveDrainEffect(Effect effect, Dictionary<int, int> targets, Stackable stackable)
    {
        var opponent = GameController.instance.gameState.GetInActivePlayer();
        opponent.Life -= effect.Amount;
        var controller = Cards.getCardFromID(stackable.SourceCardInGameId).getOwner();
        controller.Life += effect.Amount;
        yield return null;
    }

    private static IEnumerator ResolveReturnToBattlefieldEffect(Effect effect, Dictionary<int, int> targets, Stackable stackable)
    {
        foreach (var (targetId, _) in targets)
        {
            GameController.instance.MoveCard(targetId, Zone.Regroup);
        }
        yield return null;
    }
}