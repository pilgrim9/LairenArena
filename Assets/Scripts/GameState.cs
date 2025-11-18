using System;
using System.Collections.Generic;
using System.Resources;
using Mirror;
using StackObjects;
public enum State
{
    WaitingForPlayers,
    InProgress,
    AwaitingPayment,
    AwaitingTarget
}

[Serializable]
public class GameState
{
    public State state = State.WaitingForPlayers;
    public List<Cards.Card> cards = new();
    public List<Player> Players = new();
    public int Turn;
    public int ActivePlayer;
    public List<StackItem> TheStack = new();
    public int playerWithPriority;
    public Phase currentPhase = Phase.NoGame;
    public bool firstTurn;
    public int startingPlayer;
    public int winner;
    [NonSerialized]
    public TargetInfo CurrentTargetInfo;

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
    public int GetInActivePlayerID() => 1 - ActivePlayer;
    public Player GetPlayerWithPriority() => Players[playerWithPriority];
}
