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
        { EffectType.ReturnToBattlefield, ResolveReturnToBattlefieldEffect },
        { EffectType.CancelStackItem, ResolveCancelStackItemEffect },
        { EffectType.Fight, ResolveFightEffect },
        { EffectType.LoseLifeEqualToDamageReceived, ResolveLoseLifeEqualToDamageReceivedEffect },
        { EffectType.RevealAndDiscard, ResolveRevealAndDiscardEffect },
        { EffectType.CreateToken, ResolveCreateTokenEffect },
        { EffectType.DrawCard, ResolveDrawCardEffect },
        { EffectType.GainLife, ResolveGainLifeEffect },
        { EffectType.DamageAll, ResolveDamageAllEffect }
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
                            CanTargetOpponent = true,
                            MaxPower = 3
                        }
                    }
                }
            }
        },
        {
            "RojoFugazStatic", new Ability()
            {
                IsContinuous = true,
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
                TriggerRequiresSameController = true,
                TriggerRequiresCardTypes = new List<string>() { "Ally" },
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
                            CardTypes = new List<string>() { "Bruja" },
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
        },
        {
            "CumuloDeHongosEnters", new Ability()
            {
                Trigger = GameEvent.OnSelfEntersBattlefield,
                Effects = new List<Effect>()
                {
                    new Effect()
                    {
                        Type = EffectType.CreateToken,
                        Amount = 1,
                        TokenTemplateName = "INSECTO_TOKEN"
                    }
                }
            }
        },
        {
            "CiudadEnLlamas", new Ability()
            {
                Effects = new List<Effect>()
                {
                    new Effect()
                    {
                        Type = EffectType.Damage,
                        Amount = 3,
                        ValidTargets = new TargetInfo()
                        {
                            Type = TargetType.Player,
                            CanTargetOpponent = true
                        }
                    },
                    new Effect()
                    {
                        Type = EffectType.DamageAll,
                        Amount = 3,
                        ValidTargets = new TargetInfo()
                        {
                            Type = TargetType.CardInZone,
                            Zone = Zone.Regroup,
                            CanTargetOpponent = true
                        }
                    }
                }
            }
        },
        {
            "PlanesFrustrados", new Ability()
            {
                Effects = new List<Effect>()
                {
                    new Effect()
                    {
                        Type = EffectType.CancelStackItem,
                        ValidTargets = new TargetInfo()
                        {
                            Type = TargetType.StackItem,
                            CardTypes = new List<string>() { CardTypes.ORDER },
                            CanTargetOpponent = true,
                            CanTargetSelf = true
                        }
                    }
                }
            }
        },
        {
            "RitualDeNegacion", new Ability()
            {
                Cost = new List<string>() { Costs.discardACard },
                Modes = new List<Mode>()
                {
                    new Mode()
                    {
                        Description = "Counter target stack ability.",
                        Effects = new List<Effect>()
                        {
                            new Effect()
                            {
                                Type = EffectType.CancelStackItem,
                                ValidTargets = new TargetInfo()
                                {
                                    Type = TargetType.StackAbility,
                                    CanTargetOpponent = true,
                                    CanTargetSelf = true
                                }
                            }
                        }
                    },
                    new Mode()
                    {
                        Description = "Counter target order or ally spell with cost 3 or less.",
                        Effects = new List<Effect>()
                        {
                            new Effect()
                            {
                                Type = EffectType.CancelStackItem,
                                ValidTargets = new TargetInfo()
                                {
                                    Type = TargetType.StackItem,
                                    CardTypes = new List<string>() { CardTypes.ORDER, CardTypes.ALLY },
                                    MaxCost = 3,
                                    CanTargetOpponent = true,
                                    CanTargetSelf = true
                                }
                            }
                        }
                    }
                }
            }
        },
        {
            "LiderDeLaManadaStatic", new Ability()
            {
                IsContinuous = true,
                Effects = new List<Effect>()
                {
                    new Effect()
                    {
                        Type = EffectType.GrantKeyword,
                        Keyword = Keyword.MustAttack,
                        ValidTargets = new TargetInfo()
                        {
                            Type = TargetType.CardInZone,
                            Zone = Zone.Regroup,
                            CanTargetSelf = true
                        }
                    }
                }
            }
        },
        {
            "LiderDeLaManadaTrigger", new Ability()
            {
                Trigger = GameEvent.OnSelfAttacks,
                Effects = new List<Effect>()
                {
                    new Effect()
                    {
                        Type = EffectType.Fight,
                        ValidTargets = new TargetInfo()
                        {
                            Type = TargetType.CardInZone,
                            Zone = Zone.Regroup,
                            CanTargetOpponent = true
                        }
                    },
                    new Effect()
                    {
                        Type = EffectType.LoseLifeEqualToDamageReceived
                    }
                }
            }
        },
        {
            "GatitosDeBrujaTrigger", new Ability()
            {
                Trigger = GameEvent.OnSelfEntersBattlefield,
                TriggerCondition = new TriggerCondition()
                {
                    RequiresControlledSubtypes = new List<string>() { "BRUJA", "ANIMAL" },
                    MinCount = 1,
                    ExcludeSelf = true
                },
                Effects = new List<Effect>()
                {
                    new Effect()
                    {
                        Type = EffectType.CancelStackItem,
                        ValidTargets = new TargetInfo()
                        {
                            Type = TargetType.StackItem,
                            CardTypes = new List<string>() { CardTypes.ORDER },
                            MaxCost = 2,
                            CanTargetOpponent = true,
                            CanTargetSelf = true
                        }
                    }
                }
            }
        },
        {
            "NicolLaAprendizTrigger", new Ability()
            {
                Trigger = GameEvent.OnSelfEntersBattlefield,
                Effects = new List<Effect>()
                {
                    new Effect()
                    {
                        Type = EffectType.RevealAndDiscard,
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

    public static readonly Dictionary<string, ActivatedAbility> AllActivatedAbilities = new()
    {
        {
            "CumuloDeHongosActivated", new ActivatedAbility()
            {
                AdditionalCosts = new List<string> { Costs.pay1Gold, Costs.destroySelf },
                Effects = new List<Effect>()
                {
                    new Effect()
                    {
                        Type = EffectType.Drain,
                        Amount = 1
                    }
                }
            }
        },
        {
            "CascabufaloActivated", new ActivatedAbility()
            {
                AdditionalCosts = new List<string> { Costs.destroySelf },
                Effects = new List<Effect>()
                {
                    new Effect()
                    {
                        Type = EffectType.CancelStackItem, // Will need to define this later, but keeps fidelity
                        ValidTargets = new TargetInfo()
                        {
                            Type = TargetType.StackAbility
                        }
                    }
                }
            }
        }
    };

    [Serializable]
    public class Mode
    {
        public string Description;
        public List<Effect> Effects;
    }

    [Serializable]
    public class TriggerCondition
    {
        public List<string> RequiresControlledSubtypes;
        public int MinCount = 1;
        public bool ExcludeSelf = true;
    }

    [Serializable]
    public class Ability : Stackable
    {
        public List<Effect> Effects;
        public GameEvent Trigger;
        public TriggerCondition TriggerCondition;
        public bool TriggerRequiresSameController;
        public List<string> TriggerRequiresCardTypes = new();
        public bool IsContinuous;
        public List<Mode> Modes;
    }

    [Serializable]
    public class ActivatedAbility : Stackable
    {
        public List<Effect> Effects;
        public List<string> AdditionalCosts = new();
    }


    [Serializable]
    public class Effect
    {
        public EffectType Type;
        public int Amount;
        public Keyword Keyword;
        public CounterType Counter;
        public TargetInfo ValidTargets;
        public string TokenTemplateName;
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
        CancelStackItem,
        Fight,
        LoseLifeEqualToDamageReceived,
        RevealAndDiscard,
        CreateToken,
        DamageAll
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

    private static IEnumerator ResolveCancelStackItemEffect(Effect effect, Dictionary<int, int> targets, Stackable stackable)
    {
        foreach (var (targetId, _) in targets)
        {
            GameController.instance.RemoveFromStack(targetId);
        }
        yield return null;
    }

    private static IEnumerator ResolveFightEffect(Effect effect, Dictionary<int, int> targets, Stackable stackable)
    {
        if (stackable.SourceCardInGameId == -1) yield break;
        var sourceCard = Cards.getCardFromID(stackable.SourceCardInGameId);
        
        foreach (var (targetId, _) in targets)
        {
            var targetCard = Cards.getCardFromID(targetId);
            int targetPower = targetCard.Power;
            int sourcePower = sourceCard.Power;
            sourceCard.Damage += targetPower;
            targetCard.Damage += sourcePower;
        }
        yield return null;
    }

    private static IEnumerator ResolveLoseLifeEqualToDamageReceivedEffect(Effect effect, Dictionary<int, int> targets, Stackable stackable)
    {
        if (stackable.SourceCardInGameId == -1) yield break;
        var sourceCard = Cards.getCardFromID(stackable.SourceCardInGameId);
        var owner = sourceCard.getOwner();
        
        // This calculates total damage currently on the card. 
        // For Líder, assuming this resolves right after Fight, it includes the damage just dealt.
        owner.Life -= sourceCard.Damage;
        
        yield return null;
    }

    private static IEnumerator ResolveRevealAndDiscardEffect(Effect effect, Dictionary<int, int> targets, Stackable stackable)
    {
        var caster = Cards.getCardFromID(stackable.SourceCardInGameId).getOwner();
        foreach (var (targetId, _) in targets)
        {
            var targetPlayer = GameController.instance.gameState.Players[targetId];
            
            GameController.instance.gameState.state = State.AwaitingCardSelection;
            GameController.instance.gameState.RevealedHand = targetPlayer.Hand;
            GameController.instance.gameState.RevealedHandFilter = new List<string> { CardTypes.ORDER }; // Only action/order cards
            
            caster.wantsToTarget = -1;
            caster.TargetsCancelled = false;
            
            yield return new UnityEngine.WaitUntil(() => caster.wantsToTarget != -1 || caster.TargetsCancelled);
            
            if (caster.wantsToTarget != -1)
            {
                var selectedCardId = caster.wantsToTarget;
                var selectedCard = Cards.getCardFromID(selectedCardId);
                // Validate it's actually in their hand and matches the filter
                if (targetPlayer.Hand.Contains(selectedCardId) && selectedCard.Types.Contains(CardTypes.ORDER))
                {
                    yield return GameController.instance.MoveCard(selectedCardId, Zone.Discard);
                }
            }
            
            caster.wantsToTarget = -1;
            GameController.instance.gameState.RevealedHand = new List<int>();
            GameController.instance.gameState.RevealedHandFilter = new List<string>();
            GameController.instance.gameState.state = State.InProgress;
        }
        yield return null;
    }

    private static IEnumerator ResolveCreateTokenEffect(Effect effect, Dictionary<int, int> targets, Stackable stackable)
    {
        var templateField = typeof(Cards).GetField(effect.TokenTemplateName);
        if (templateField != null)
        {
            var templateCard = (Cards.Card)templateField.GetValue(null);
            for (int i = 0; i < effect.Amount; i++)
            {
                var newCard = GameController.instance.NewCard(templateCard, stackable.Caster);
                yield return GameController.instance.MoveCard(newCard.InGameId, Zone.Regroup);
            }
        }
        else
        {
            UnityEngine.Debug.LogError("Token template not found: " + effect.TokenTemplateName);
        }
        yield return null;
    }

    private static IEnumerator ResolveDrawCardEffect(Effect effect, Dictionary<int, int> targets, Stackable stackable)
    {
        var player = GameController.instance.gameState.Players[stackable.Caster];
        for (int i = 0; i < effect.Amount; i++)
        {
            if (player.Kingdom.Count > 0)
            {
                yield return GameController.instance.MoveCard(player.Kingdom[0], Zone.Hand);
            }
        }
    }

    private static IEnumerator ResolveGainLifeEffect(Effect effect, Dictionary<int, int> targets, Stackable stackable)
    {
        var player = GameController.instance.gameState.Players[stackable.Caster];
        player.Life += effect.Amount;
        yield return null;
    }

    private static IEnumerator ResolveDamageAllEffect(Effect effect, Dictionary<int, int> targets, Stackable stackable)
    {
        var targetInfos = effect.ValidTargets;
        if (targetInfos != null)
        {
            var validCards = new List<int>();
            foreach (var card in GameController.instance.gameState.cards)
            {
                if (targetInfos.IsValidTarget(card.InGameId, GameController.instance.gameState.Players[stackable.Caster]))
                {
                    validCards.Add(card.InGameId);
                }
            }
            foreach (var cardId in validCards)
            {
                Cards.getCardFromID(cardId).Damage += effect.Amount;
            }
        }
        yield return null;
    }
}