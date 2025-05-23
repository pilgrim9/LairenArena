using System;
using System.Collections.Generic;
using System.Resources;
using Mirror;
using StackObjects;

[Serializable]
public class GameState
{

    public enum State
    {
        InProgress,
        AwaitingPayment
    }
    public State state;
    public List<Cards.Card> cards = new();
    public List<Player> Players = new();
    public int Turn;
    public int ActivePlayer;

    // TODO: ACA DEJASTE TENES QUE HACER QUE EL STACK ITEM SEA UN ID.

    public List<StackItem> TheStack = new();
    public int playerWithPriority;
    public Phase currentPhase;
    public bool firstTurn;
    public int startingPlayer;

    public StackItem PopStack()
    {
        StackItem returnThis = TheStack[^1];
        TheStack.RemoveAt(TheStack.Count-1);
        return returnThis;
    }
    public GameState Clone()
    {
        return new GameState()
        {
            Players = new List<Player>(Players),
            Turn = Turn,
            ActivePlayer = ActivePlayer,
            TheStack = new List<StackItem>(TheStack),
            playerWithPriority = playerWithPriority,
            currentPhase = currentPhase,
            firstTurn = firstTurn,
            startingPlayer = startingPlayer,
        };
    }
    public Player GetActivePlayer() => Players[ActivePlayer];
    public Player GetInActivePlayer() => Players[1-ActivePlayer];
    public Player GetPlayerWithPriority() => Players[playerWithPriority];
}
