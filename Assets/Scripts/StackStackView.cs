using System.Collections.Generic;

public class StackStackView : CardStackView
{
    protected override List<int> GetCardList(GameState _new)
    {
        List<int> cards = new List<int>();
        for (int i = 0; i < _new.TheStack.Count; i++)
        {
            cards.Add(_new.TheStack[i].stackable.GetRelatedCard());
        }
        return cards;
    }
}
