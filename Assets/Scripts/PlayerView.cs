using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerView : MonoBehaviour, IPointerClickHandler
{
    public Player player;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (GameController.instance.gameState.state == State.AwaitingTarget)
        {
            if (GameController.instance.gameState.CurrentTargetInfo.IsValidTarget(player.PlayerId, GameController.instance.getLocalPlayer()))
            {
                RPCManager.instance.RpcSelectTarget(player.PlayerId, GameController.instance.GetLocalPlayerId());
            }
        }
    }
}
