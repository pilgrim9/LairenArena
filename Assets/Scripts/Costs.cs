using System;
using System.Collections;
using System.Collections.Generic;


public static class Costs
{
    public delegate IEnumerator ResolveCostDelegate(Player player);

    [NonSerialized]

    public static readonly string pay2life = "Pay2Life";
    public static readonly string discardACard = "DiscardACard";
    private static IEnumerator Pay2Life(Player player)
    {
        player.Life -= 2;
        yield return null;
    }
    public static readonly Dictionary<string, ResolveCostDelegate> CostResolvers = new()
    {
        { pay2life, Pay2Life},
    };

}
