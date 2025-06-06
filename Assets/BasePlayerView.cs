using UnityEngine;

public abstract class BasePlayerView : MonoBehaviour
{
    public bool isOwner;

    protected virtual int GetPlayer()
    {
        if (isOwner) return GameController.instance.GetLocalPlayerId();
        return 1 - GameController.instance.GetLocalPlayerId();
    }

    
    private void Start()
    {
        GameController.instance.GameStateUpdated += UpdateView;
    }

    protected abstract void UpdateView(GameState old, GameState _new);
}
