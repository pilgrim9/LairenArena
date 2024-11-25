using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using StackObjects;
using UnityEngine;
using Random = System.Random;
using Card = Cards.Card;
using kcp2k;
using Edgegap.Editor.Api.Models.Results;


public class GameController : NetworkBehaviour
{
    public static GameController instance;
    public List<Card> cards = new();

    [SerializeField] public GameObject CardViewPrefab;
    private void Awake()
    {
        instance = this;
    }
    public bool firstTurn = true;
    
    [SyncVar(hook=nameof(OnGameStateUpdated))]
    private string serializedGameState;  // We'll use this to sync the full state

    public GameState gameState = new GameState();

    // Method to update the game state
    [Server]
    private void UpdateGameState()
    {
        serializedGameState = JsonUtility.ToJson(gameState);
    }

    // Hook that runs when serializedGameState changes
    private void OnGameStateUpdated(string oldState, string newState)
    {
        Debug.Log($"GameController | OnGameStateUpdated {oldState==newState}");
        if (!string.IsNullOrEmpty(newState) && oldState != newState)
        {
            if (!isServer) gameState = JsonUtility.FromJson<GameState>(newState);
            GameStateUpdated?.Invoke(JsonUtility.FromJson<GameState>(oldState), gameState);
        }
    }

    public event Action<GameState, GameState> GameStateUpdated;

    public int GetLocalPlayerId()
    {
        if (isServer) // Host
            return 0;
        if (!isServer) // Client only
            return 1;
            
        Debug.LogError("GetLocalPlayerId called on invalid instance (server-only?)");
        return -1;
    }
    public Player getLocalPlayer()
    {
        return gameState.Players[GetLocalPlayerId()];
    }
    public Player getRemotePlayer() {
        return gameState.Players[1];
    }
    private Coroutine gameLoop;

    public CustomNetworkManager networkManager;
    public override void OnStartServer()
    {
        base.OnStartServer();
        networkManager = (CustomNetworkManager)NetworkManager.singleton;
        Debug.Log("GameController | OnStartServer");
        networkManager.OnClientConnectAction += OnClientConnect;
    }

    public void OnClientConnect() {
        if (!isServer) {
            Debug.Log("GameController | OnClientConnect ignored - not server");
            return;
        }
        
        Debug.Log($"GameController | OnClientConnect - Current players: {NetworkManager.singleton.numPlayers}");
        
        if (NetworkManager.singleton.numPlayers < 1 || NetworkManager.singleton.numPlayers > 1) {
            Debug.Log("GameController | Waiting for more players to connect...");
            return;
        }
        
        Debug.Log("GameController | Starting game setup with 2 players");
        StartCoroutine(SetupGame());
    }

    public bool WaitingForResponse { get; private set; }

    private IEnumerator SetupGame()
    {
        Debug.Log("GameController | Beginning SetupGame");
        gameState = new GameState();
        gameState.Players = new List<Player>();  // Make sure this is initialized as a List
        
        // Initialize both players
        for (int i = 0; i < 2; i++)
        {
            Player player = new Player
            {
                Hand = new(),
                Kingdom = GetDeck(i),
                Vault = GetVault(i)
            };
            gameState.Players.Add(player);
            Debug.Log($"Initialized player {i} with deck size: {player.Kingdom.Count}");
        }

        yield return ServerSetDirty();
        DetermineStartingPlayer();
        
        yield return HandleMulligans();
        gameLoop = StartCoroutine(GameLoop());
    }

    public List<int> GetDeck(int playerId)
    {
        List<int> deck = new List<int>();
        for (int i = 0; i < 45+playerId; i++)
        {

            deck.Add(NewCard(Cards.ROJO_FUGAZ, playerId).InGameId);
        }
        return deck;
    }
    public List<int> GetVault(int playerid)
    {
        List<int> deck = new List<int>();
        for (int i = 0; i < 15; i++)
        {
            deck.Add(NewCard(Cards.TreasureGenerico, playerid).InGameId);
        }
        return deck;
    }

    public Card NewCard(Card card, int playerId) {
        Card newCard = card.Clone();
        newCard.InGameId = cards.Count;
        cards.Add(newCard); 
        newCard.Owner = playerId;
        return newCard;
    }
    public void DetermineStartingPlayer()
    {
        var rnd = new Random();
        gameState.startingPlayer = rnd.NextDouble() < 0.5 ? 0 : 1;
    }

    # region Mulligan
    // Add this before your main game loop
    private IEnumerator HandleMulligans()
    {
        gameState.currentPhase = Phase.Mulligan;
        // Initialize mulligan tracking for each player
        foreach (Player player in gameState.Players)
        {
            Debug.Log(gameState.Players.Count);
            Debug.Log($"Drawing initial hand for player {player}");
            player.KeepHand = false;
            yield return PerformMulligan(player);
        }

        bool allPlayersKept = false;
        
        while (!allPlayersKept)
        {
            // Reset decision tracking for this round
            foreach (Player player in gameState.Players)
            {
                if (!player.KeepHand)
                {
                    player.KeepHand = false;
                }
            }

            // Wait for all players to make their mulligan decision
            foreach (Player player in gameState.Players)
            {
                if (!player.KeepHand)
                {
                    // Set the active player so UI can show the correct hand
                    gameState.ActivePlayer = gameState.Players.IndexOf(player);
                    
                    // Wait for the player's decision
                    yield return AwaitMulliganDecision(player);

                    if (!player.KeepHand)
                    {
                        yield return PerformMulligan(player);
                    }
                }
            }

            // Check if all players have kept their hands
            allPlayersKept = gameState.Players.All(p => p.KeepHand);
        }

        // After all players keep, handle bottom of library cards
        foreach (Player player in gameState.Players)
        {
            if (player.mulliganCount > 0)
            {
                yield return AwaitBottomCards(player, player.mulliganCount);
            }
        }
    }

    private IEnumerator AwaitMulliganDecision(Player player)
    {
        player.AwaitingMulliganDecision = true;

        Debug.Log("Waiting for " + player + " to make a mulligan decision.");
        // Wait until the player makes a decision
        yield return new WaitUntil(() => player.MulliganDecisionMade);

        player.AwaitingMulliganDecision = false;
        player.MulliganDecisionMade = false;
        yield return ServerSetDirty();
    }

    private IEnumerator PerformMulligan(Player player)
    {
        // Increment mulligan count
        player.mulliganCount++;
        
        // Return cards to library
        yield return MoveAll(player.Hand, player.Kingdom);
        Debug.Log("Shuffling and drawing a new hand for " + player + "." + " Cards in hand: " + player.Hand.Count);
        // Shuffle
        player.ShuffleLibrary();
        
        // Draw new hand of 7
        DrawCards(player, 7);
        
        yield return null;
        yield return ServerSetDirty();
    }

    private IEnumerator AwaitBottomCards(Player player, int cardsToBottom)
    {
        player.AwaitingBottomDecision = true;
        player.CardsToBottom = cardsToBottom;
        
        while (player.CardsToBottom > 0)
        {
            Debug.Log("Waiting for " + player + " to select a card to put on bottom.");
            // Wait for player to select a card to put on bottom
            yield return new WaitUntil(() => player.SelectedCardIdForBottom != -1);
            yield return ServerSetDirty();
            // Move selected card to bottom of library
            yield return MoveCard(player.SelectedCardIdForBottom, player.Hand, player.Kingdom);
            player.CardsToBottom--;
            player.SelectedCardIdForBottom = -1;
            yield return ServerSetDirty();
        }
        player.AwaitingBottomDecision = false;
        yield return ServerSetDirty();
    }

    # endregion
    public bool CanStackSpeed(Player player, Stackable stackable)
    {
        if (stackable.speed == Speed.SLOW && !player.CanStackSlowActions())
        {
            return false;
        }
        if (stackable.speed == Speed.FAST && !player.CanStackFastActions())
        {
            return false;
        }
        return true;
    }
    
    public IEnumerator AddToStack(Player player, Stackable stackable)
    {
        if (gameState.GetActivePlayer() != player) yield break; // Doesn't have priority
        if (!CanStackSpeed(player, stackable)) yield break; // Can't cast at this speed.
        if (stackable.IsCard() && player.CanPay((Card)stackable))
        {
            yield return player.MustPay(((Card)stackable).Cost);
            if (player.PaymentCanceled) yield break;
        }
        player.HasAddedToStack = true;
        stackable.Caster = instance.gameState.Players.IndexOf(player);
        player.AddToStack = new StackItem(stackable);
    }
    // Add this method to handle priority passing
    private IEnumerator HandlePriority()
    {
        // Start with active player
        gameState.playerWithPriority = gameState.ActivePlayer;
        bool roundOfPriority = true;

        while (roundOfPriority)
        {
            // Wait for the player with priority to make a decision
            WaitingForResponse = true;
            Debug.Log("Waiting for " + gameState.GetPlayerWithPriority() + " to make a decision.");
            yield return new WaitUntil(() => 
                gameState.GetPlayerWithPriority().HasPassedPriority || 
                gameState.GetPlayerWithPriority().HasAddedToStack);

            if (gameState.GetPlayerWithPriority().HasAddedToStack)
            {
                // Add their action to the stack
                gameState.TheStack.Add(gameState.GetPlayerWithPriority().AddToStack);
                gameState.GetPlayerWithPriority().AddToStack = null;
                gameState.GetPlayerWithPriority().HasAddedToStack = false;
                
                // Start a new round of priority
                gameState.playerWithPriority = gameState.ActivePlayer;
                continue;
            }

            if (gameState.GetPlayerWithPriority().HasPassedPriority)
            {
                // Move priority to the next player
                int currentIndex = gameState.playerWithPriority;
                int nextIndex = (currentIndex + 1) % gameState.Players.Count;
                
                // If we're back to the active player and everyone passed
                if (nextIndex == gameState.ActivePlayer && AllPlayersPassedPriority())
                {
                    if (gameState.TheStack.Count > 0)
                    {
                        // Resolve the top of the stack
                        yield return ResolveTopOfStack();
                        
                        // Start a new round of priority with active player
                        ResetPriorityPassed();
                        gameState.playerWithPriority = gameState.ActivePlayer;
                    }
                    else
                    {
                        // If stack is empty and all passed, move to next phase
                        roundOfPriority = false;
                    }
                }
                else
                {
                    // Pass priority to next player
                    gameState.playerWithPriority = nextIndex;
                }
            }

            WaitingForResponse = false;
            yield return null;
        }
    }

    private bool AllPlayersPassedPriority()
    {
        return gameState.Players.All(p => p.HasPassedPriority);
    }

    private void ResetPriorityPassed()
    {
        foreach (var player in gameState.Players)
        {
            player.HasPassedPriority = false;
        }
    }

    private IEnumerator ResolveTopOfStack()
    {
        if (gameState.TheStack.Count > 0)
        {
            Stackable resolveThis = gameState.PopStack().getItem();
            foreach (var effect in resolveThis.ResolutionEffects)
            {
                effect.Invoke(resolveThis);
            }
            
            if (resolveThis is Card card)
            {
                ResolveCard(card);
            }
        }
        yield return null;
    }

    private IEnumerator AwaitPriority()
    {
        yield return ServerSetDirty();
        ResetPriorityPassed();
        yield return HandlePriority();
    }

    public void ResolveCard(Card card)
    {
        MoveCardTo(card.InGameId, gameState.Players[card.Owner].GetZone(card.OnResolutionTargetZone));
    }


    public void MoveCardTo(int cardId, List<int> to)
    {
        to.Add(cardId);
    }
    public IEnumerator MoveCardTo(int card, List<int> from, List<int> to)
    {
        to.Add(from[from.IndexOf(card)]);
        from.RemoveAt(from.IndexOf(card));
        yield return ServerSetDirty();
    }
    
    private void DrawCards(Player player, int count)
    {
        for (int i = 0; i < count; i++)
        {
            DrawCard(player);
        }
    }

    private void DrawCard(Player player)
    {
        if (player.Kingdom.Count == 0)
        {
            Lose(player);
        }

        player.Hand.Add(player.Kingdom[0]);
        player.Kingdom.RemoveAt(0);
    }

    private void Lose(Player player)
    {
        StopCoroutine(gameLoop);
        Debug.Log("Player loses!");
    }


    private IEnumerator MoveAll(List<int> from, List<int> to)
    {
        Debug.Log("Moving " + from.Count + " cards from " + from + " to " + to);
        List<int> cardsToMove = new List<int>(from); // Create a copy to avoid modification during iteration
        foreach (int cardId in cardsToMove) {
            yield return MoveCard(cardId, from, to);
        }
        yield return ServerSetDirty();
    }
    
    
    public IEnumerator MoveCard(int card, List<int> from, List<int> to)
    {
        Debug.Log($"Attempting to move card {cards[card].Name} from {from.Count} cards to {to.Count} cards");
        if (!from.Contains(card))
        {
            Debug.LogError($"Card {cards[card].Name} not found in source list!");
            yield break;
        }

        int initialFromCount = from.Count;
        int initialToCount = to.Count;
        
        from.Remove(card);
        to.Add(card);
        
        Debug.Log($"Move complete - From: {initialFromCount}->{from.Count}, To: {initialToCount}->{to.Count}");
        
        if (from.Count != initialFromCount - 1 || to.Count != initialToCount + 1)
        {
            Debug.LogError("Card move operation did not complete as expected!");
        }
        
        yield return ServerSetDirty();
    }
    

    private IEnumerator ApplyDamage(Player attackingPlayer, Player defendingPlayer)
    {
        var attackers = attackingPlayer.Attackers;
        foreach (int attackerId in attackers)
        {
            Card attacker = cards[attackerId];
            if (attacker.Blockers.Count == 0)
            {
                defendingPlayer.Life = (int)defendingPlayer.Life - (int)attacker.Power;
            }
            else
            {
                var blockers = attacker.Blockers;
                foreach (int blockerId in blockers)
                {
                    cards[blockerId].Damage += attacker.Power;
                    attacker.Damage += cards[blockerId].Damage;
                }
            }
        }
        yield return ServerSetDirty();
    }

    private IEnumerator Untap()
    {
        yield return MoveAll(gameState.GetActivePlayer().Paid, gameState.GetActivePlayer().Reserve);
        yield return MoveAll(gameState.GetActivePlayer().Attackers, gameState.GetActivePlayer().Regroup);
    }
    
    
    private IEnumerator Reveal()
    {
        if (gameState.GetActivePlayer().Reserve.Count >=7) yield break;
        yield return MoveCardTo(gameState.GetActivePlayer().Reserve[0], gameState.GetActivePlayer().Vault, gameState.GetActivePlayer().Reserve);
    }
    private void Draw()
    {
        if (!firstTurn)
        {
            DrawCard(gameState.GetActivePlayer());
        }
        else
        {
            firstTurn = false;
        }
    }

    private IEnumerator MainPhase1()
    {
        yield return MainPhase();
    }      
    private IEnumerator MainPhase2()
    {
        yield return  MainPhase();
    }    
    private IEnumerator MainPhase()
    {
        yield return AwaitPriority();
    }    
    private void CombatStart()
    {
        
    }
    public bool declaredAttackers;

    private IEnumerator DeclareAttackers()
    {
        Debug.Log("Waiting for " + gameState.GetActivePlayer() + " to declare attackers.");
        yield return new WaitUntil(() => declaredAttackers);
        yield return ServerSetDirty();
    }

    public bool declaredBlockers;
    private IEnumerator DeclareBlockers()
    {
        Debug.Log("Waiting for " + gameState.Players[gameState.ActivePlayer - 1] + " to declare blockers.");
        yield return new WaitUntil(() => declaredBlockers);
        yield return ServerSetDirty();
    }
    
    private void Damage()
    {
        ApplyDamage(gameState.GetActivePlayer(), gameState.Players[gameState.ActivePlayer - 1]);
    }

    private void EndPhase()
    {
    }

    public int cardToDiscard = -1;

    private IEnumerator Cleanup()
    {
        while (gameState.GetActivePlayer().Hand.Count > 7)
        {
            Debug.Log("Waiting for " + gameState.GetActivePlayer() + " to discard a card.");
            yield return new WaitUntil(() => cardToDiscard != -1);
            yield return MoveCard(cardToDiscard, gameState.GetActivePlayer().Hand, gameState.GetActivePlayer().Discard);
            cardToDiscard = -1;
            yield return ServerSetDirty();
        }
    }

    // Coroutine for the game loop
    private IEnumerator GameLoop()
    {
        
        Debug.Log("GameController | Starting game loop");
        // Main game loop
        while (true)
        {
            for (int i = 0; i < gameState.Players.Count; i++)
            {
                if (gameState.firstTurn)
                {
                    gameState.firstTurn = false;
                    if (i != gameState.startingPlayer) continue;
                }
                gameState.ActivePlayer = i;
                Untap();

                yield return AwaitPriority();

                Reveal();
                yield return AwaitPriority();
                
                Draw();
                yield return AwaitPriority();

                yield return MainPhase1();
                yield return AwaitPriority();

                CombatStart();
                // Combat phase
                yield return AwaitPriority();
                
                yield return DeclareAttackers();
                yield return AwaitPriority();

                yield return DeclareBlockers();
                yield return AwaitPriority();

                yield return MainPhase2();
                yield return AwaitPriority();

                EndPhase();
                yield return AwaitPriority();
                
                yield return Cleanup();
                // Wait for the next frame to simulate real-time play
                yield return null;
            }
        }
    }
    
    


    [SyncVar(hook = nameof(WantsToStackUpdated))] public StackItem wantsToStack;

    private void WantsToStackUpdated(StackItem old, StackItem _new)
    {
        if (_new == null) return;
        if (!isClient) AddToStack(gameState.GetActivePlayer(), _new.getItem());
    }
    [Server]
    private IEnumerator ServerSetDirty()
    {
        UpdateGameState();
        SetDirty();
        yield return new WaitForSeconds(0.001f);
    }
}