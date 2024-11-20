using Mirror;
using StackObjects;

public class RPCManager : NetworkBehaviour
{
    public static RPCManager instance;

    public void Awake()
    {
        if (!instance)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    [Command(requiresAuthority = false)]
    public void RpcSyncMulliganDecision(bool keepHand)
    {
        GameController.instance.getLocalPlayer().KeepHand = keepHand;
        GameController.instance.getLocalPlayer().MulliganDecisionMade = true;
    }
    [Command(requiresAuthority = false)]
    public void RPCSelectCardForBottom(Cards.Card card)
    {
        GameController.instance.getLocalPlayer().SelectCardForBottom(card);
    }

    [Command(requiresAuthority = false)]
    public void RpcSyncPriorityPassed()
    {
        GameController.instance.getLocalPlayer().HasPassedPriority = true;
    }
    [Command(requiresAuthority = false)]
    public void RpcAddCardToStack(Cards.Card card)
    {
        GameController.instance.wantsToStack = new StackItem(card); 
    }

    public void AcceptRpc()
    {
        RpcSyncPriorityPassed();
        RpcSyncMulliganDecision(true);
    }
    public void CancelRpc()
    {
        RpcSyncMulliganDecision(false);
    }
}