using System;
using System.Collections;
using System.Collections.Generic;

public static class Costs
{
    public delegate IEnumerator ResolveCostDelegate(Player player);

    public static readonly Dictionary<string, ResolveCostDelegate> CostResolvers = new()
    {
        { "Pay2Life", Pay2Life }
    };

    public static readonly string Pay2Life = "Pay2Life";
    // Add other costs here

    private static IEnumerator Pay2Life(Player player)
    {
        player.Life -= 2;
        yield return null;
    }
}
