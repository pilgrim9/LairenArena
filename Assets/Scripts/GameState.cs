using System;
using System.Collections.Generic;
using Mirror;
using StackObjects;
using Unity.VisualScripting;

[Serializable]
public class GameState
{
    [SyncVar]
    public List<Player> Players = new();
    public int Turn;
    [SyncVar]
    public int ActivePlayer;

    [SyncVar]
    public List<StackItem> TheStack = new();
    [SyncVar]
    public int playerWithPriority;
    [SyncVar]
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
    public Player GetPlayerWithPriority() => Players[playerWithPriority];
}
