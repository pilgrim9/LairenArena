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
            cardStackView.isOwner = IsOwner;
        }
    }
}
