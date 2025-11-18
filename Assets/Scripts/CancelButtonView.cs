using UnityEngine;
using UnityEngine.UI;

public class CancelButtonView : MonoBehaviour
{
    public Button cancelButton;

    private void Start()
    {
        GameController.instance.GameStateUpdated += UpdateView;
        cancelButton.onClick.AddListener(OnCancelButtonClick);
    }

    private void UpdateView(GameState oldState, GameState newState)
    {
        cancelButton.gameObject.SetActive(newState.state == State.AwaitingTarget);
    }

    private void OnCancelButtonClick()
    {
        RPCManager.instance.RpcCancelTargets(GameController.instance.GetLocalPlayerId());
    }
}
