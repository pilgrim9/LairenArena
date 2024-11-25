using System;
using UnityEngine;
using UnityEngine.UI;

public class MulliganUI : MonoBehaviour
{
    public Button keepButton;
    public Button mulliganButton;
    public GameObject mulliganPanel;
    public GameObject bottomCardPanel;
    public Text updatableText;

    private void Awake()
    {
        keepButton.onClick.AddListener(OnKeepClicked) ;
        mulliganButton.onClick.AddListener(OnMulliganClicked);
    }

    private void OnDestroy()
    {
        keepButton.onClick.RemoveListener(OnKeepClicked) ;
        mulliganButton.onClick.RemoveListener(OnMulliganClicked);
    }

    private void Update()
    {
        Player currentPlayer = GameController.instance.gameState.GetActivePlayer();
        
        // Show/hide mulligan UI based on whether we're awaiting a decision
        mulliganPanel.SetActive(currentPlayer.AwaitingMulliganDecision);
        
        // Show/hide bottom card selection UI
        bottomCardPanel.SetActive(currentPlayer.AwaitingBottomDecision);
        if (bottomCardPanel.activeSelf)
        {
            UpdateBottomCardText(currentPlayer.CardsToBottom);
        }
    }

    public void OnKeepClicked()
    {
        GameController.instance.gameState.GetActivePlayer().DecideToKeep();
    }

    public void OnMulliganClicked()
    {
        GameController.instance.gameState.GetActivePlayer().DecideToMulligan();
    }

    public void OnCardClicked(Cards.Card card)
    {
        // If we're selecting cards to put on bottom
        if (GameController.instance.gameState.GetActivePlayer().AwaitingBottomDecision)
        {
            GameController.instance.gameState.GetActivePlayer().SelectCardForBottom(card.InGameId);
        }
    }

    private void UpdateBottomCardText(int remainingCards)
    {
        // Update UI text to show how many cards still need to be put on bottom
        updatableText.text = $"Select {remainingCards} card{(remainingCards > 1 ? "s" : "")} to put on the bottom of your library";
    }
}