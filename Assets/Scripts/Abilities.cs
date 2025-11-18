using System;
using System.Collections;
using System.Collections.Generic;
using StackObjects;

public class Abilities
{
    public delegate IEnumerator ResolveEffectDelegate(Effect effect, List<int> targets);

    public static readonly Dictionary<EffectType, ResolveEffectDelegate> EffectResolvers = new()
    {
        { EffectType.Damage, ResolveDamageEffect }
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
    }

    [Serializable]
    public class Effect
    {
        public EffectType Type;
        public int Amount;
        public TargetInfo ValidTargets;
    }

    public enum EffectType
    {
        Damage,
        DrawCard,
        GainLife
    }

    private static IEnumerator ResolveDamageEffect(Effect effect, List<int> targets)
    {
        foreach (var targetId in targets)
        {
            Cards.getCardFromID(targetId).Damage += effect.Amount;
        }
        yield return null;
    }
}
