using System.Collections.Generic;

public static class Targets {
    
    public static string Opponent = "Opponent";
    public static string Player = "Player";
    public static string Unit = "Unit";
    public static string EnemyUnit = "EnemyUnit";
    public static string ActionOrUnitOrderWithCost3OrLess = "ActionOrUnitOrderWithCost3OrLess";

    public static string ActionOrderWithCost2OrLess = "ActionOrderWithCost2OrLess";
    public static string Order = "Order";
    public static string AbilityInPlay = "AbilityInPlay";
    public static string TwoWitchesInGraveyard = "TwoWitchesInGraveyard";
    public static string OneOrTwoUnits = "OneOrTwoUnits";

    public class SelectedTarget {
        public int playerId = -1;
        public int cardId = -1;
        public int stackIndex = -1;
    }

}
