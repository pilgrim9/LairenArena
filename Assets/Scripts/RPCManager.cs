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
    public void RpcSyncMulliganDecision(bool keepHand, int playerId)
    {
        GameController.instance.gameState.Players[playerId].KeepHand = keepHand;
        GameController.instance.gameState.Players[playerId].MulliganDecisionMade = true;
    }
    [Command(requiresAuthority = false)]
    public void RPCSelectCardForBottom(int card, int playerId)
    {
        GameController.instance.gameState.Players[playerId].SelectCardForBottom(card);
    }

    [Command(requiresAuthority = false)]
    public void RpcSyncPriorityPassed(int playerId)
    {
        GameController.instance.gameState.Players[playerId].HasPassedPriority = true;
    }
    [Command(requiresAuthority = false)]
    public void RpcAddCardToStack(int cardId, int playerId)
    {
        GameController.instance.AddToStack(GameController.instance.gameState.Players[playerId], Cards.getCardFromID(cardId));
    }
    [Command(requiresAuthority = false)]
    public void RpcConfirmAttackers(int playerId)
    {
        GameController.instance.gameState.Players[playerId].hasDeclaredAttack = true;
    }
    [Command(requiresAuthority = false)]
    public void RPCConfirmBlockers(int playerId)
    {
        GameController.instance.gameState.Players[playerId].hasDeclaredBlock = true;
    }
    [Command(requiresAuthority = false)]
    public void RpcSelectAtacker (int card) {
    }

    public void AcceptRpc()
    {
        RpcSyncPriorityPassed(GameController.instance.GetLocalPlayerId());
        RpcSyncMulliganDecision(true, GameController.instance.GetLocalPlayerId());
        RpcConfirmAttackers(GameController.instance.GetLocalPlayerId());
        RPCConfirmBlockers(GameController.instance.GetLocalPlayerId());
    }
    public void CancelRpc()
    {
        RpcSyncMulliganDecision(false, GameController.instance.GetLocalPlayerId());
        //RpcCancelTargets(GameController.instance.GetLocalPlayerId());
    }
}