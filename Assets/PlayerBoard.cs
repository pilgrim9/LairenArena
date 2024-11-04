using UnityEngine;

public class PlayerBoard : MonoBehaviour
{
    public bool IsOwner = false;
    public CardStackView[] cardStackViews;

    private void Start()
    {
        cardStackViews = GetComponentsInChildren<CardStackView>();
        foreach (var cardStackView in cardStackViews)
        {
            if (IsOwner) cardStackView.Player = GameController.instance.GetLocalPlayerId();
            else cardStackView.Player = 1 - GameController.instance.GetLocalPlayerId();
        }
    }
}
