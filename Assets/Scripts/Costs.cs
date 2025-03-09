using System.Collections.Generic;

public static class Costs {
    public class Cost
    {
        public int Amount;
    }

    public class PayLife : Cost {}
    public class Discard : Cost {}

    public static Dictionary<string,Cost> additionalCosts = new Dictionary<string,Cost>() {
        { Pay2Life, new PayLife() { Amount = 2} },
        { DiscardACard, new Discard() { Amount = 1} }    
    };
    public static Cost getResourceCost( int amount) {
        return new Cost() { Amount = amount };
    }
    
    public static string Pay2Life = "Pay2Life";
    public static string DiscardACard = "DiscardACard";

}
