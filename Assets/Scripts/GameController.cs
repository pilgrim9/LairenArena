using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using StackObjects;
using UnityEngine;
using Random = System.Random;
using Card = Cards.Card;
using UnityEngine.Scripting.APIUpdating;
using Mirror.Examples.Common.Controllers.Player;

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
            if (!isServer) gameState = JsonUtility.FromJson<GameState>(newState);
            GameStateUpdated?.Invoke(JsonUtility.FromJson<GameState>(oldState), gameState);
        }
    }

    public event Action<GameState, GameState> GameStateUpdated;

    public int GetLocalPlayerId()
    {
        Debug.Log("GetLocalPlayerId | isServer: " + isServer);
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
    private Coroutine cardPlayedValidator;
    private Coroutine cardPaymentValidator;

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
        cardPlayedValidator = StartCoroutine(ValidateCardPlayed());
        cardPaymentValidator = StartCoroutine(ValidatePayments());
    }

    private IEnumerator ValidateCardPlayed()
    {
        while (true)
        {
            Debug.Log("GameController | ValidateCardPlayed");
            yield return new WaitUntil(() => gameState.Players.Any(player => player.wantToStack > -1));
            Debug.Log("GameController | ValidateCardPlayed - Player wants to stack");
            foreach (Player player in gameState.Players)
            {
                Debug.Log($"Player {player} wants to stack {player.wantToStack}");
                if (player.wantToStack > -1)
                {
                    Debug.Log($"Player {player} wants to stack {player.wantToStack}");
                    yield return AddToStack(player, Cards.getCardFromID(player.wantToStack));
                    player.wantToStack = -1;
                }
            }
        }
    }

    private IEnumerator ValidatePayments()
    {
        while (true)
        {
            yield return new WaitUntil(() => gameState.Players.Any(player => player.wantsToPayWith > -1));
            foreach (Player player in gameState.Players)
            {
                if (player.wantsToPayWith > -1)
                {
                    yield return MoveCard(player.wantsToPayWith, Zone.Paid);
                    player.AmountToPay -= 1;
                    player.wantsToPayWith = -1;
                    yield return ServerSetDirty();
                }
            }
        }
    }

    public List<int> GetDeck(int playerId)
    {
        List<int> deck = new List<int>();
        for (int i = 0; i < 45; i++)
        {
            Card newCard = NewCard(Decks.SampleDeck[i], playerId);
            deck.Add(newCard.InGameId);
            newCard.currentZone = Zone.Kingdom;

            // deck.Add(NewCard(Cards.ROJO_FUGAZ, playerId).InGameId);
        }
        return deck;
    }
    public List<int> GetVault(int playerid)
    {
        List<int> deck = new List<int>();
        for (int i = 0; i < 15; i++)
        {
            Card newCard = NewCard(Cards.TreasureGenerico, playerid);
            deck.Add(newCard.InGameId);
            newCard.currentZone = Zone.Vault;
        }
        return deck;
    }

    public Card NewCard(Card card, int playerId) {
        Card newCard = card.Clone();
        newCard.InGameId = gameState.cards.Count;
        gameState.cards.Add(newCard); 
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
        yield return MoveAll(player.Hand, player.Kingdom, Zone.Kingdom);
        Debug.Log("Shuffling and drawing a new hand for " + player + "." + " Cards in hand: " + player.Hand.Count);
        // Shuffle
        player.ShuffleLibrary();
        
        // Draw new hand of 7
        yield return DrawCards(player, 7);
        
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
            Debug.Log("Selected card to put on bottom: " + player.SelectedCardIdForBottom);
            yield return ServerSetDirty();
            // Move selected card to bottom of library
            yield return MoveCard(player.SelectedCardIdForBottom, Zone.Kingdom);
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

    StackItem AddThisToStack;
    public IEnumerator AddToStack(Player player, Stackable stackable)
    {
        Debug.Log("GameController | AddToStack | Player " + player + " is adding " + stackable + " to stack.");
        if (gameState.GetActivePlayer() != player)
        {
            Debug.Log("Player " + gameState.GetActivePlayer() + " is not active player.");
            yield break; // Doesn't have priority  
        }
        if (!CanStackSpeed(player, stackable))
        {
            Debug.Log("Player " + player + " cannot stack " + stackable + " at this speed.");
            yield break; // Can't cast at this speed.
        }
        if (!player.CanPay((Card)stackable))
        {
            Debug.Log("Player " + player + " cannot pay to stack " + stackable);
            yield break;
        }
        if (stackable.IsCard())
        {
            Debug.Log("Player " + player + " must pay to stack " + stackable);
            yield return player.MustPay(((Card)stackable).Cost);
            if (player.PaymentCanceled) yield break;
            player.HasAddedToStack = true;
            stackable.Caster = instance.gameState.Players.IndexOf(player);
            player.Hand.Remove(stackable.InGameId);
            Cards.getCardFromID(stackable.InGameId).currentZone = Zone.Stack;
            AddThisToStack = new StackItem(stackable);
        }

    }
    
    // Add this method to handle priority passing
    private IEnumerator HandlePriority()
    {
        // Start with active player
        gameState.playerWithPriority = gameState.ActivePlayer;
        bool roundOfPriority = true;

        while (roundOfPriority)
        {
            yield return ServerSetDirty();
            // Wait for the player with priority to make a decision
            WaitingForResponse = true;
            Debug.Log("Waiting for " + gameState.GetPlayerWithPriority() + " to make a decision.");
            yield return new WaitUntil(() =>
                gameState.GetPlayerWithPriority().HasPassedPriority ||
                gameState.GetPlayerWithPriority().HasAddedToStack);

            if (gameState.GetPlayerWithPriority().HasAddedToStack)
            {

                // Add their action to the stack
                gameState.TheStack.Add(AddThisToStack);
                AddThisToStack = null;
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
            Stackable resolveThis = gameState.PopStack().stackable;
            // foreach (var effect in resolveThis.ResolutionEffects)
            // {
                // effect.Invoke(resolveThis);
            // }
             
            if (resolveThis is Card card)
            {
                yield return ResolveCard(card);
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

    public IEnumerator ResolveCard(Card card)
    {
        yield return MoveCard(card.InGameId, card.getResolutionTargetZone());
    }



    public IEnumerator MoveCard(int cardId, Zone targetZone)
    {
        Card card = Cards.getCardFromID(cardId);
        List<int> from = card.getOwner().GetZone(card.currentZone);
        List<int> to = card.getOwner().GetZone(targetZone);
        yield return MoveCard(cardId, from, to, targetZone);
    }

    private IEnumerator DrawCards(Player player, int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return DrawCard(player);
        }
    }

    private IEnumerator DrawCard(Player player)
    {
        if (player.Kingdom.Count == 0)
        {
            Lose(player);
        }
        int cardId = player.Kingdom[0];
        yield return MoveCard(cardId, Zone.Hand);
        yield return null;
    }

    private void Lose(Player player)
    {
        StopCoroutine(gameLoop);
        Debug.Log("Player loses!");
    }


    private IEnumerator MoveAll(List<int> from, List<int> to, Zone targetZone)
    {
        Debug.Log("Moving " + from.Count + " cards from " + from + " to " + to);
        List<int> cardsToMove = new(from); 
        foreach (int cardId in cardsToMove) {
            yield return MoveCard(cardId, from, to, targetZone);
        }
        yield return ServerSetDirty();
    }
  
    
    public IEnumerator MoveCard(int card, List<int> from, List<int> to, Zone targetZone)
    {
        Debug.Log($"Attempting to move card {gameState.cards[card].Name} from {from} cards to {to}");
        if (from == null)
        {
            Debug.Log("GameController | Movecard | Card is in stack or, an unreegistered zone!");
        }
        else if (!from.Contains(card))
        {
            Debug.LogError($"Card {gameState.cards[card].Name} not found in source list!");
            yield break;
        }
        from.Remove(card);
        to.Add(card);
        gameState.cards[card].currentZone = targetZone;

        yield return ServerSetDirty();
    }

    private IEnumerator ApplyDamage(Player attackingPlayer, Player defendingPlayer)
    {
        var attackers = attackingPlayer.Attackers;
        foreach (int attackerId in attackers)
        {
            Card attacker = gameState.cards[attackerId];
            if (attacker.Blockers.Count == 0)
            {
                defendingPlayer.Life = (int)defendingPlayer.Life - (int)attacker.Power;
            }
            else
            {
                var blockers = attacker.Blockers;
                foreach (int blockerId in blockers)
                {
                    gameState.cards[blockerId].Damage += attacker.Power;
                    attacker.Damage += gameState.cards[blockerId].Damage;
                }
            }
        }
        yield return ServerSetDirty();
    }

    private IEnumerator Untap()
    {
        gameState.currentPhase = Phase.Untap;
        yield return MoveAll(gameState.GetActivePlayer().Paid, gameState.GetActivePlayer().Reserve, Zone.Reserve);
        yield return MoveAll(gameState.GetActivePlayer().Attackers, gameState.GetActivePlayer().Regroup, Zone.Regroup);
    } 
    
    private IEnumerator Reveal() {
        gameState.currentPhase = Phase.Reveal;
        if (gameState.GetActivePlayer().Reserve.Count >=7) {
            Debug.Log("Reserve is full, skipping reveal phase."); // TODO, move flag to upkeep.
            yield break;
        }
        yield return MoveCard(gameState.GetActivePlayer().Vault[0], Zone.Reserve);
    }

    private IEnumerator Draw() {
        gameState.currentPhase = Phase.Draw;
        if (!firstTurn)
        {
            yield return DrawCard(gameState.GetActivePlayer());
        }
        else
        {
            firstTurn = false;
        }
    }

    private IEnumerator MainPhase1()
    {
        gameState.currentPhase = Phase.MainPhase1;
        yield return MainPhase(); //
    }      
    private IEnumerator MainPhase2()
    {
        gameState.currentPhase = Phase.MainPhase2;
        yield return MainPhase(); //
    }    
    private IEnumerator MainPhase()
    {
        yield return null;
    }    
    private IEnumerator CombatStart()
    {
        gameState.currentPhase = Phase.CombatStart;
        yield return null;
    }

    private IEnumerator DeclareAttackers()
    {
        gameState.currentPhase = Phase.DeclareAttackers;

        Debug.Log("Waiting for " + gameState.GetActivePlayer() + " to declare attackers.");
        gameState.GetActivePlayer().hasDeclaredAttack = false;
        gameState.GetActivePlayer().wantsToAttackWith = -1;
        while (gameState.GetActivePlayer().hasDeclaredAttack == false)
        {
            yield return new WaitUntil(() => gameState.GetActivePlayer().hasDeclaredAttack || gameState.GetActivePlayer().wantsToAttackWith != -1);
            if (gameState.GetActivePlayer().wantsToAttackWith != -1)
            {
                int cardId = gameState.GetActivePlayer().wantsToAttackWith;
                if (Cards.getCardFromID(cardId).currentZone == Zone.Attackers)
                {
                    yield return MoveCard(cardId, Zone.Regroup);
                }
                else if (Cards.getCardFromID(cardId).currentZone == Zone.Regroup)
                {
                    yield return MoveCard(cardId, Zone.Attackers);
                }
                gameState.GetActivePlayer().wantsToAttackWith = -1;
            }
            yield return ServerSetDirty();
        } 
    }

    private IEnumerator DeclareBlockers()
    {
        gameState.currentPhase = Phase.DeclareBlockers;
        Debug.Log("Waiting for " + gameState.GetInActivePlayer() + " to declare blockers.");
        if (gameState.GetActivePlayer().Attackers.Count == 0) yield break;
        gameState.GetInActivePlayer().hasDeclaredBlock = false;
        yield return new WaitUntil(() => gameState.GetInActivePlayer().hasDeclaredBlock);
        yield return ServerSetDirty();
    }
    
    private IEnumerator Damage()
    {
        gameState.currentPhase = Phase.Damage;
        yield return ApplyDamage(gameState.GetActivePlayer(), gameState.GetInActivePlayer());
        yield return ServerSetDirty();
    }

    private IEnumerator EndPhase()
    {
        gameState.currentPhase = Phase.EndPhase;
        yield return ServerSetDirty();
    }

    public int cardToDiscard = -1;

    private IEnumerator Cleanup()
    {
        gameState.currentPhase = Phase.Cleanup;
        while (gameState.GetActivePlayer().Hand.Count > 7)
        {
            Debug.Log("Waiting for " + gameState.GetActivePlayer() + " to discard a card.");
            yield return new WaitUntil(() => cardToDiscard != -1);
            yield return MoveCard(cardToDiscard, Zone.Discard);
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
                Debug.Log($"Starting turn {i} for {gameState.GetActivePlayer()}");

                Debug.Log("Entering Untap Phase");
                yield return Untap();
                yield return AwaitPriority();

                Debug.Log("Entering Reveal Phase");
                yield return Reveal();
                yield return AwaitPriority();
                
                Debug.Log("Entering Draw Phase");
                yield return Draw();
                yield return AwaitPriority();

                Debug.Log("Entering Main Phase 1");
                yield return MainPhase1();
                yield return AwaitPriority();
                
                Debug.Log("Entering Combat Phase");
                yield return CombatStart();
                // Combat phase
                yield return AwaitPriority();
                
                Debug.Log("Entering Declare Attackers Phase");
                yield return DeclareAttackers();
                yield return AwaitPriority();

                Debug.Log("Entering Declare Blockers Phase");
                yield return DeclareBlockers();
                yield return AwaitPriority();

                Debug.Log("Entering Damage Phase");
                yield return Damage();
                yield return AwaitPriority();

                Debug.Log("Entering Main Phase 2");
                yield return MainPhase2();
                yield return AwaitPriority();

                Debug.Log("Entering End Phase");
                yield return EndPhase();
                yield return AwaitPriority();
                
                Debug.Log("Entering Cleanup Phase");
                yield return Cleanup();
                // Wait for the next frame
                yield return null;
            }
        }
    }

    [Server]
    private IEnumerator ServerSetDirty()
    {
        yield return null;
        UpdateGameState();
        SetDirty();
        yield return new WaitForSeconds(0.001f);
    }
}