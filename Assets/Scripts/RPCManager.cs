using Mirror;
using StackObjects;
using Unity.VisualScripting;
using UnityEngine;
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
        Debug.Log("RpcAddCardToStack" + cardId);
        GameController.instance.gameState.Players[playerId].wantToStack = cardId;
    }

    [Command(requiresAuthority = false)]
    public void RpcPay(int cardId, int playerId)
    {
        Debug.Log("RpcPay" + cardId);
        GameController.instance.gameState.Players[playerId].wantsToPayWith = cardId;
    }
    [Command(requiresAuthority = false)]
    public void RpcCancelPayment(int playerId)
    {
        GameController.instance.gameState.Players[playerId].PaymentCanceled = true;
    }
    [Command(requiresAuthority = false)]
    public void RpcCancelTargets(int playerId)
    {
        GameController.instance.gameState.Players[playerId].TargetsCancelled = true;
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
    public void RpcSelectAtacker(int playerId,int card)
    {
        GameController.instance.gameState.Players[playerId].wantsToAttackWith = card;
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
        RpcCancelTargets(GameController.instance.GetLocalPlayerId());
        RpcCancelPayment(GameController.instance.GetLocalPlayerId());
    }
}