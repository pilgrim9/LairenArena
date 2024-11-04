public class HandStackView : CardStackView
{

    protected override int GetPlayer()
    {
        if (isOwner) return GameController.instance.GetLocalPlayerId();
        return 1 - GameController.instance.GetLocalPlayerId();
    }
    
}
