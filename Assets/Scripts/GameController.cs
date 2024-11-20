using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using StackObjects;
using UnityEngine;
using Random = System.Random;
using Card = Cards.Card;


public class GameController : NetworkBehaviour
{
    public static GameController instance;

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
            gameState = JsonUtility.FromJson<GameState>(newState);
            GameStateUpdated?.Invoke(JsonUtility.FromJson<GameState>(oldState), gameState);
        }
    }

    public event Action<GameState, GameState> GameStateUpdated;

    public int GetLocalPlayerId()
    {
        return isServer ? 0 : 1;
    }
    public Player getLocalPlayer()
    {
        return gameState.Players[isServer ? 0 : 1];
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
        gameState.Players.Add(new Player());
        gameState.Players.Add(new Player());
        
        Debug.Log("GameController | Setting up decks for players");
        gameState.Players[0].Kingdom = GetDeck(0);
        gameState.Players[1].Kingdom = GetDeck(1);

        Debug.Log("GameController | Setting up vaults for players");
        gameState.Players[0].Vault = GetVault(0);
        gameState.Players[1].Vault = GetVault(1);
        
        ServerSetDirty();
        DetermineStartingPlayer();
        Debug.Log($"GameController | Starting player determined: Player {gameState.startingPlayer}");
        
        Debug.Log("GameController | Beginning mulligan phase");
        yield return HandleMulligans();
        
        Debug.Log("GameController | Starting main game loop");
        gameLoop = StartCoroutine(GameLoop());
    }

    public List<Card> GetDeck(int playerId)
    {
        List<Card> deck = new List<Card>();
        for (int i = 0; i < 45; i++)
        {
            Card newCard = Cards.ROJO_FUGAZ.Clone(); 
            newCard.Owner = playerId;
            deck.Add(newCard);
        }
        return deck;
    }
    public List<Card> GetVault(int playerid)
    {
        List<Card> deck = new List<Card>();
        for (int i = 0; i < 45; i++)
        {
            Card newCard = Cards.TreasureGenerico.Clone(); 
            newCard.Owner = playerid;
            deck.Add(newCard);
        }
        return deck;
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
            player.mulliganCount = 1; // starts in 1 so we have to put 1 at the bottom.
            player.KeepHand = false;
            
            // Initial draw of 7 cards
            DrawCards(player, 7);
            ServerSetDirty();
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
                    yield return StartCoroutine(AwaitMulliganDecision(player));

                    if (!player.KeepHand)
                    {
                        yield return StartCoroutine(PerformMulligan(player));
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
                yield return StartCoroutine(AwaitBottomCards(player, player.mulliganCount));
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
        ServerSetDirty();
    }

    private IEnumerator PerformMulligan(Player player)
    {
        // Increment mulligan count
        player.mulliganCount++;
        
        // Return cards to library
        MoveAll(player.Hand, player.Kingdom);
        
        // Shuffle
        ShuffleLibrary(player);
        
        // Draw new hand of 7
        DrawCards(player, 7);
        
        yield return null;
        ServerSetDirty();
    }

    private IEnumerator AwaitBottomCards(Player player, int cardsToBottom)
    {
        player.AwaitingBottomDecision = true;
        player.CardsToBottom = cardsToBottom;
        
        while (player.CardsToBottom > 0)
        {
            Debug.Log("Waiting for " + player + " to select a card to put on bottom.");
            // Wait for player to select a card to put on bottom
            yield return new WaitUntil(() => player.SelectedCardForBottom != null);
            
            // Move selected card to bottom of library
            MoveCard(player.SelectedCardForBottom, player.Hand, player.Kingdom);
            player.CardsToBottom--;
            player.SelectedCardForBottom = null;
            ServerSetDirty();
        }
        player.AwaitingBottomDecision = false;
        ServerSetDirty();
    }

    # endregion
    private void ShuffleLibrary(Player player)
    {
        var rng = new Random();
        player.Kingdom = player.Kingdom.OrderBy(x => rng.Next()).ToList();
    }

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
        ServerSetDirty();
        ResetPriorityPassed();
        yield return HandlePriority();
    }

    public void ResolveCard(Card card)
    {
        MoveCardTo(card, gameState.Players[card.Owner].GetZone(card.OnResolutionTargetZone));
    }


    public void MoveCardTo(Card card, List<Card> to)
    {
        to.Add(card);
    }
    public void MoveCardTo(Card card, List<Card> from, List<Card> to)
    {
        to.Add(from[from.IndexOf(card)]);
        from.RemoveAt(from.IndexOf(card));
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

    private void MoveAll(List<Card> from, List<Card> to)
    {
        to.AddRange(from);
        from.Clear();
        ServerSetDirty();
    }
    
    

    public void MoveCard(Card card, List<Card> from, List<Card> to)
    {
        from.Remove(card);
        to.Add(card);
        ServerSetDirty();
    }
    

    private void ApplyDamage(Player attackingPlayer, Player defendingPlayer)
    {
        var attackers = attackingPlayer.Attackers;
        foreach (Card attacker in attackers)
        {
            if (attacker.Blockers.Count == 0)
            {
                defendingPlayer.Life = (int)defendingPlayer.Life - (int)attacker.Power;
            }
            else
            {
                var blockers = attacker.Blockers;
                foreach (Card blocker in blockers)
                {
                    blocker.Damage += attacker.Power;
                    attacker.Damage += blocker.Damage;
                }
            }
        }
        ServerSetDirty();
    }

    private void Untap()
    {
        MoveAll(gameState.GetActivePlayer().Paid, gameState.GetActivePlayer().Reserve);
        MoveAll(gameState.GetActivePlayer().Attackers, gameState.GetActivePlayer().Regroup);
    }
    
    
    private void Reveal()
    {
        if (gameState.GetActivePlayer().Reserve.Count >=7) return;
        MoveCardTo(gameState.GetActivePlayer().Reserve[0], gameState.GetActivePlayer().Vault, gameState.GetActivePlayer().Reserve);
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
        //
        // var card = GetBestPlay(ActivePlayer);
        // if (card != null)
        // {
        //     PlayCard(ActivePlayer, card);
        // }
    }    
    private void CombatStart()
    {
        
    }
    public bool declaredAttackers;

    private IEnumerator DeclareAttackers()
    {
        Debug.Log("Waiting for " + gameState.GetActivePlayer() + " to declare attackers.");
        yield return new WaitUntil(() => declaredAttackers);
        ServerSetDirty();
    }

    public bool declaredBlockers;
    private IEnumerator DeclareBlockers()
    {
        Debug.Log("Waiting for " + gameState.Players[gameState.ActivePlayer - 1] + " to declare blockers.");
        yield return new WaitUntil(() => declaredBlockers);
        ServerSetDirty();
    }
    
    private void Damage()
    {
        ApplyDamage(gameState.GetActivePlayer(), gameState.Players[gameState.ActivePlayer - 1]);
    }

    private void EndPhase()
    {
    }

    public Card cardToDiscard;

    private IEnumerator Cleanup()
    {
        while (gameState.GetActivePlayer().Hand.Count > 7)
        {
            Debug.Log("Waiting for " + gameState.GetActivePlayer() + " to discard a card.");
            yield return new WaitUntil(() => cardToDiscard!= null);
            ServerSetDirty();
            MoveCard(cardToDiscard, gameState.GetActivePlayer().Hand, gameState.GetActivePlayer().Discard);
            cardToDiscard = null;
        }
    }

    // Coroutine for the game loop
    private IEnumerator GameLoop()
    {
        
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
        if (isServer) AddToStack(gameState.GetActivePlayer(), _new.getItem());
    }
    [Server]
    private void ServerSetDirty()
    {
        SetDirty();
        UpdateGameState();
    }
}