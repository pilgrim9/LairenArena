using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticAbilityController : MonoBehaviour
{
    void Start()
    {
        GameController.instance.GameStateUpdated += OnGameStateUpdated;
    }

    void OnGameStateUpdated(GameState oldState, GameState newState)
    {
        // Remove old static effects
        foreach (var card in oldState.cards)
        {
            foreach (var keyword in card.GrantedKeywords)
            {
                card.Keywords.Remove(keyword);
            }
            card.GrantedKeywords.Clear();
        }

        // Apply new static effects
        foreach (var card in newState.cards)
        {
            if (card.Name == "ROJO FUGAZ" && card.currentZone == Zone.Regroup)
            {
                foreach (var otherCard in newState.cards)
                {
                    if (otherCard.Subtypes.Contains("Animal") && !otherCard.Keywords.Contains(Keyword.Frenzy))
                    {
                        otherCard.Keywords.Add(Keyword.Frenzy);
                        otherCard.GrantedKeywords.Add(Keyword.Frenzy);
                    }
                }
            }
        }
    }
}
