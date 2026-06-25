using System.Collections.Generic;
using System.Linq;

public enum TargetType
{
    Player,
    CardInZone,
    StackAbility,
    StackItem
}
[System.Serializable]

public class TargetInfo
{
    public TargetType Type;
    public Zone Zone;
    public List<string> CardTypes;
    public List<string> Subtypes;
    public int? MaxPower;
    public int? MaxCost;
    public bool CanTargetSelf;
    public bool CanTargetOpponent;
    public int MaxTargets = 1;
    public int AmountToDistribute = 0;

    public bool IsValidTarget(int targetId, Player castingPlayer)
    {
        switch (Type)
        {
            case TargetType.CardInZone:
                var card = Cards.getCardFromID(targetId);
                if (card == null) return false;
                if (card.currentZone != Zone) return false;
                if (CardTypes != null && CardTypes.Count > 0 && !card.Types.Any(t => CardTypes.Contains(t))) return false;
                if (Subtypes != null && Subtypes.Count > 0 && !card.Subtypes.Any(st => Subtypes.Contains(st))) return false;
                if (MaxPower.HasValue && card.Power > MaxPower.Value) return false;
                if (MaxCost.HasValue && card.Cost > MaxCost.Value) return false;

                bool isSelf = card.Owner == castingPlayer.PlayerId;
                if (isSelf && !CanTargetSelf) return false;
                if (!isSelf && !CanTargetOpponent) return false;

                return true;

            case TargetType.Player:
                var targetPlayer = GameController.instance.gameState.Players.FirstOrDefault(p => p.PlayerId == targetId);
                if (targetPlayer == null) return false;

                bool isTargetSelf = targetPlayer.PlayerId == castingPlayer.PlayerId;
                if (isTargetSelf && !CanTargetSelf) return false;
                if (!isTargetSelf && !CanTargetOpponent) return false;

                return true;

            case TargetType.StackItem:
                if (targetId < 0 || targetId >= GameController.instance.gameState.TheStack.Count) return false;
                var stackItem = GameController.instance.gameState.TheStack[targetId];
                if (stackItem.stackable == null) return false;
                
                // If CardTypes is specified, check against the stackable's types
                if (CardTypes != null && CardTypes.Count > 0)
                {
                    if (!stackItem.stackable.IsCard()) return false;
                    var cardStackItem = (Cards.Card)stackItem.stackable;
                    if (!cardStackItem.Types.Any(t => CardTypes.Contains(t))) return false;
                }
                
                if (MaxCost.HasValue)
                {
                    if (!stackItem.stackable.IsCard()) return false;
                    var cardStackItem = (Cards.Card)stackItem.stackable;
                    if (cardStackItem.Cost > MaxCost.Value) return false;
                }
                
                return true;
        }
        return false;
    }
}
