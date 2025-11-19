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

    public int STARTING_LIFE = 20;
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
                PlayerId = i,
                Hand = new(),
                Kingdom = GetDeck(i),
                Vault = GetVault(i),
                Life = STARTING_LIFE,
            };
            gameState.Players.Add(player);
            Debug.Log($"Initialized player {i} with deck size: {player.Kingdom.Count}");
        }

        yield return Propagate();
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
                    yield return Propagate();
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
        yield return Propagate();
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
        yield return Propagate();
    }

    private IEnumerator AwaitBottomCards(Player player, int cardsToBottom)
    {
        player.AwaitingBottomDecision = true;
        player.CardsToBottom = cardsToBottom;
        
        while (player.CardsToBottom > 0)
        {
            Debug.Log("Waiting for " + player + " to select a card to put on bottom.");
            yield return Propagate();
            // Wait for player to select a card to put on bottom
            yield return new WaitUntil(() => player.SelectedCardIdForBottom != -1);
            Debug.Log("Selected card to put on bottom: " + player.SelectedCardIdForBottom);
            yield return Propagate();
            // Move selected card to bottom of library
            yield return MoveCard(player.SelectedCardIdForBottom, Zone.Kingdom);
            player.CardsToBottom--;
            player.SelectedCardIdForBottom = -1;
            yield return Propagate();
        }
        player.AwaitingBottomDecision = false;
        yield return Propagate();
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

            Card card = (Card)stackable;

            foreach (var cost in card.AdditionalCosts)
            {
                if (Costs.CostResolvers.TryGetValue(cost, out var resolver))
                {
                    yield return resolver(player);
                }
            }

            var requiredTargets = new Queue<TargetInfo>();

            foreach (var ability in card.Abilities)
            {
                foreach (var effect in ability.Effects)
                {
                    if (effect.ValidTargets != null)
                    {
                        requiredTargets.Enqueue(effect.ValidTargets);
                    }
                }
            }

            while (requiredTargets.Count > 0)
            {
                var currentTargetInfo = requiredTargets.Dequeue();
                gameState.CurrentTargetInfo = currentTargetInfo;
                gameState.state = State.AwaitingTarget;

                if (currentTargetInfo.AmountToDistribute > 0)
                {
                    int remainingAmount = currentTargetInfo.AmountToDistribute;
                    int targetsSelected = 0;
                    while (remainingAmount > 0 && targetsSelected < currentTargetInfo.MaxTargets)
                    {
                        yield return new WaitUntil(() => player.wantsToTarget != -1 || player.TargetsCancelled);
                        if (player.TargetsCancelled)
                        {
                            player.TargetsCancelled = false;
                            yield break;
                        }
                        if (currentTargetInfo.IsValidTarget(player.wantsToTarget, player))
                        {
                            int amount = player.wantsToTargetAmount;
                            if (amount > 0 && amount <= remainingAmount)
                            {
                                var currentTargets = new Dictionary<int, int>();
                                currentTargets[player.wantsToTarget] = amount;
                                stackable.AllTargets.Add(currentTargets);
                                remainingAmount -= amount;
                                targetsSelected++;
                            }
                        }
                        player.wantsToTarget = -1;
                        player.wantsToTargetAmount = 0;
                    }
                }
                else
                {
                    var currentTargets = new Dictionary<int, int>();
                    int targetsSelected = 0;
                    while (targetsSelected < currentTargetInfo.MaxTargets)
                    {
                        yield return new WaitUntil(() => player.wantsToTarget != -1 || player.TargetsCancelled || player.TargetsConfirmed);

                        if (player.TargetsCancelled || player.TargetsConfirmed)
                        {
                            player.TargetsCancelled = false;
                            player.TargetsConfirmed = false;
                            break;
                        }

                        if (currentTargetInfo.IsValidTarget(player.wantsToTarget, player))
                        {
                            currentTargets[player.wantsToTarget] = 0;
                            targetsSelected++;
                        }
                        else
                        {
                            Debug.Log("Invalid target selected.");
                        }
                        player.wantsToTarget = -1;
                    }
                    stackable.AllTargets.Add(currentTargets);
                }
            }

            gameState.state = State.InProgress;
            player.HasAddedToStack = true;
            if (card.Types.Contains(CardTypes.ORDER))
            {
                FireEvent(GameEvent.OnOrderPlayed);
            }
            stackable.Caster = instance.gameState.Players.IndexOf(player);
            player.Hand.Remove(stackable.InGameId);
            Cards.getCardFromID(stackable.InGameId).currentZone = Zone.Stack;
            AddThisToStack = new StackItem(stackable);
        }

    }

    // Add this method to handle priority passing
    private List<(GameEvent gameEvent, int sourceCardId)> eventQueue = new();

    public void FireEvent(GameEvent gameEvent, int sourceCardId = -1)
    {
        eventQueue.Add((gameEvent, sourceCardId));
    }

    private IEnumerator CheckTriggers()
    {
        while (eventQueue.Count > 0)
        {
            var (gameEvent, sourceCardId) = eventQueue[0];
            eventQueue.RemoveAt(0);

            foreach (var card in gameState.cards)
            {
                foreach (var ability in card.Abilities)
                {
                    bool eventMatches = ability.Trigger == gameEvent;
                    bool isSelfTrigger = sourceCardId != -1 && card.InGameId == sourceCardId && ability.Trigger == GameEvent.OnSelfEntersBattlefield;
                    bool isOtherTrigger = sourceCardId != -1 && card.InGameId != sourceCardId && ability.Trigger == GameEvent.OnAnotherCardEntersBattlefield;

                    if (eventMatches || isSelfTrigger || isOtherTrigger)
                    {
                        var triggeredAbility = ability.Clone() as Abilities.Ability;
                        triggeredAbility.SourceCardInGameId = card.InGameId;
                        gameState.TheStack.Add(new StackItem(triggeredAbility));
                    }
                }
            }
        }
        yield return null;
    }

    private IEnumerator HandlePriority()
    {
        // Start with active player
        gameState.playerWithPriority = gameState.ActivePlayer;
        bool roundOfPriority = true;

        while (roundOfPriority)
        {
            yield return UpdateStateBasedEffects();
            yield return CheckTriggers();
            yield return Propagate();
            // Wait for the player with priority to make a decision
            WaitingForResponse = true;
            Debug.Log("Waiting for " + gameState.GetPlayerWithPriority() + " to make a decision.");
            if (gameState.GetPlayerWithPriority().autoSkip) gameState.GetPlayerWithPriority().HasPassedPriority = true;
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

    private IEnumerator UpdateStateBasedEffects()
    {
        // layer effects an other rule problems go here
        yield return null;
        // players win or lose
        foreach (Player player in gameState.Players)
        {
            if (player.Life <= 0)
            {
                yield return Lose(player);
            }
        }

        foreach (var card in gameState.cards)
        {
            if (card.Damage >= card.Resistance && card.currentZone == Zone.Regroup)
            {
                yield return MoveCard(card.InGameId, Zone.Discard);
            }
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
            List<Abilities.Ability> abilities = new List<Abilities.Ability>();

            if (resolveThis is Card card)
            {
                abilities = card.Abilities;
            }
            else if (resolveThis is Abilities.Ability ability)
            {
                abilities.Add(ability);
            }

            int currentTargetIndex = 0;
            foreach (var ability in abilities)
            {
                foreach (var effect in ability.Effects)
                {
                    if (Abilities.EffectResolvers.TryGetValue(effect.Type, out var resolver))
                    {
                        Dictionary<int, int> currentTargets = null;
                        if (effect.ValidTargets != null)
                        {
                            if (currentTargetIndex < resolveThis.AllTargets.Count)
                            {
                                currentTargets = resolveThis.AllTargets[currentTargetIndex];
                                currentTargetIndex++;
                            }
                        }
                        else if (resolveThis.AllTargets.Count > 0)
                        {
                            currentTargets = resolveThis.AllTargets[currentTargetIndex -1];
                        }

                        yield return resolver(effect, currentTargets, resolveThis);
                    }
                }
            }
             
            if (resolveThis is Card cardToResolve)
            {
                yield return ResolveCard(cardToResolve);
            }
        }
        yield return null;
    }

    private IEnumerator AwaitPriority()
    {
        yield return Propagate();
        ResetPriorityPassed();
        yield return HandlePriority();
    }

    public IEnumerator ResolveCard(Card card)
    {
        yield return MoveCard(card.InGameId, card.getResolutionTargetZone());
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
            yield return Lose(player);
        }
        int cardId = player.Kingdom[0];
        yield return MoveCard(cardId, Zone.Hand);
        yield return null;
    }

    private IEnumerator Lose(Player player)
    {
        StopCoroutine(gameLoop);
        Debug.Log("Player loses!");
        player.lost = true;

        // The other player wins
        foreach (Player otherPlayer in gameState.Players)
        {
            if (otherPlayer == player) continue;
            gameState.winner = otherPlayer.PlayerId;
        }
        gameState.currentPhase = Phase.GameEnded;
        yield return Propagate();
    }


    private IEnumerator MoveAll(List<int> from, List<int> to, Zone targetZone)
    {
        Debug.Log("Moving " + from.Count + " cards from " + from + " to " + to);
        List<int> cardsToMove = new(from); 
        foreach (int cardId in cardsToMove) {
            yield return MoveCard(cardId, from, to, targetZone);
        }
        yield return Propagate();
    }
  
    
    public IEnumerator MoveCard(int cardId, Zone targetZone)
    {
        Card card = Cards.getCardFromID(cardId);
        List<int> from = card.getOwner().GetZone(card.currentZone);
        List<int> to = card.getOwner().GetZone(targetZone);
        yield return MoveCard(cardId, from, to, targetZone);
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
        from?.Remove(card);
        to.Add(card);
        gameState.cards[card].currentZone = targetZone;

        if (targetZone == Zone.Regroup)
        {
            FireEvent(GameEvent.OnAnotherCardEntersBattlefield, card);
            FireEvent(GameEvent.OnSelfEntersBattlefield, card);
        }

        if (targetZone == Zone.Discard)
        {
            FireEvent(GameEvent.OnCardDefeated, card);
        }

        yield return Propagate();
    }

    public IEnumerator MoveCardToBlockers(int blockerId, int attackerId)
    {
        Card attacker = Cards.getCardFromID(attackerId);
        Card blocker = Cards.getCardFromID(blockerId);
        attacker.Blockers.Add(blockerId);
        List<int> from = blocker.getOwner().GetZone(blocker.currentZone);
        from.Remove(blockerId);
        blocker.currentZone = Zone.Blockers;
        blocker.BlockingAttacker = attackerId;
        yield return Propagate();
    }

    public IEnumerator RemoveCardFromBlockers(int blockerId, int attackerId)
    {
        Card attacker = Cards.getCardFromID(attackerId);
        Card blocker = Cards.getCardFromID(blockerId);
        attacker.Blockers.Remove(blockerId);
        blocker.currentZone = Zone.Regroup;
        blocker.BlockingAttacker = -1;
        yield return MoveCard(blockerId, attacker.Blockers, attacker.getOwner().GetZone(Zone.Regroup), Zone.Regroup);
    }

    private IEnumerator ApplyDamage(Player attackingPlayer, Player defendingPlayer)
    {
        var attackers = attackingPlayer.Attackers;
        foreach (int attackerId in attackers)
        {
            Card attacker = Cards.getCardFromID(attackerId);
            if (attacker.Blockers.Count == 0)
            {
                defendingPlayer.Life -= attacker.Power;
            }
            else
            {
                var blockers = attacker.Blockers;
                foreach (int blockerId in blockers)
                {
                    Card blocker = Cards.getCardFromID(blockerId);
                    blocker.Damage += attacker.Power;
                    attacker.Damage += blocker.Damage;
                }
            }
        }
        yield return Propagate();
    }

    private IEnumerator Untap()
    {
        gameState.currentPhase = Phase.Untap;
        foreach (var cardId in gameState.GetActivePlayer().Regroup)
        {
            Cards.getCardFromID(cardId).SummoningSickness = false;
        }
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
        yield return MainPhase(); 
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
        Debug.Log("GameController | DeclareAttackers | Waiting for " + gameState.GetActivePlayer() + " to declare attackers.");
        gameState.playerWithPriority = gameState.ActivePlayer;
        gameState.GetActivePlayer().hasDeclaredAttack = false;
        gameState.GetActivePlayer().wantsToAttackWith = -1;
        yield return Propagate();
        while (gameState.GetActivePlayer().hasDeclaredAttack == false)
        {
            Debug.Log("GameController | DeclareAttackers | Waiting for " + gameState.GetActivePlayer() + " to declare attackers OR add a new attacker.");
            yield return new WaitUntil(() => gameState.GetActivePlayer().hasDeclaredAttack || gameState.GetActivePlayer().wantsToAttackWith != -1);
            if (gameState.GetActivePlayer().wantsToAttackWith != -1)
            {
                Debug.Log("GameController | DeclareAttackers | Player wants to attack with " + gameState.GetActivePlayer().wantsToAttackWith);
                int cardId = gameState.GetActivePlayer().wantsToAttackWith;
                var card = Cards.getCardFromID(cardId);
                if (card.currentZone == Zone.Attackers)
                {
                    yield return MoveCard(cardId, Zone.Regroup);
                }
                else if (card.currentZone == Zone.Regroup)
                {
                    if (!card.SummoningSickness || card.Keywords.Contains(Keyword.Frenzy))
                    {
                        yield return MoveCard(cardId, Zone.Attackers);
                    }
                }
                gameState.GetActivePlayer().wantsToAttackWith = -1;
            }
            yield return Propagate();
        } 
    }

    private bool assignedABlocker() => (gameState.GetInActivePlayer().wantsToBlockWith != -1 && gameState.GetInActivePlayer().wantsToBlockTarget != -1);
    private bool wantsToRemoveBlocker() => (gameState.GetInActivePlayer().wantsToBlockWith != -1 && Cards.getCardFromID(gameState.GetInActivePlayer().wantsToBlockWith).currentZone == Zone.Blockers);
    private IEnumerator DeclareBlockers()
    {
        gameState.currentPhase = Phase.DeclareBlockers;
        Debug.Log("GameController | DeclareBlockers | Waiting for " + gameState.GetInActivePlayer() + " to declare blockers.");
        if (gameState.GetActivePlayer().Attackers.Count == 0 || gameState.GetInActivePlayer().Regroup.Count == 0) yield break;
        gameState.playerWithPriority = gameState.GetInActivePlayerID();
        gameState.GetInActivePlayer().hasDeclaredBlock = false;
        gameState.GetInActivePlayer().wantsToBlockWith = -1;
        gameState.GetInActivePlayer().wantsToBlockTarget = -1;
        yield return Propagate();
        while (gameState.GetInActivePlayer().hasDeclaredBlock == false)
        {
            Debug.Log("GameController | DeclareBlockers | Waiting for " + gameState.GetInActivePlayer() + " to declare blockers.");
            yield return new WaitUntil(() => gameState.GetInActivePlayer().hasDeclaredBlock || assignedABlocker() || wantsToRemoveBlocker());


            if (assignedABlocker())
            {
                Debug.Log("GameController | DeclareBlockers | Player wants to block with " + gameState.GetInActivePlayer().wantsToBlockWith);
                int blocker = gameState.GetInActivePlayer().wantsToBlockWith;
                int attacker = gameState.GetInActivePlayer().wantsToBlockTarget;
                yield return MoveCardToBlockers(blocker, attacker);
                gameState.GetInActivePlayer().wantsToBlockWith = -1;
                gameState.GetInActivePlayer().wantsToBlockTarget = -1;
            }
            if (wantsToRemoveBlocker())
            {
                Debug.Log("GameController | DeclareBlockers | Player wants to remove blocker with " + gameState.GetInActivePlayer().wantsToBlockWith);
                int blocker = gameState.GetInActivePlayer().wantsToBlockWith;
                int attacker = Cards.getCardFromID(blocker).BlockingAttacker;
                yield return RemoveCardFromBlockers(blocker, attacker);
                gameState.GetInActivePlayer().wantsToBlockWith = -1;
                gameState.GetInActivePlayer().wantsToBlockTarget = -1;
            }
            yield return Propagate();
        }
        yield return Propagate();
    }
    
    private IEnumerator Damage()
    {
        gameState.currentPhase = Phase.Damage;
        yield return ApplyDamage(gameState.GetActivePlayer(), gameState.GetInActivePlayer());
        yield return Propagate();
    }

    private IEnumerator EndPhase()
    {
        gameState.currentPhase = Phase.EndPhase;
        foreach (var card in gameState.cards)
        {
            card.ClearTemporaryKeywords();
        }
        yield return Propagate();
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
            yield return Propagate();
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
                CleanInputs();
                // Wait for next frame
                yield return null;
            }
        }
    }
    
    private void CleanInputs()
    {
        foreach (Player player in gameState.Players)
        {
            player.autoSkip = false;
        }
    }

    [Server]
    private IEnumerator Propagate()
    {
        yield return null;
        UpdateGameState();
        SetDirty();
        yield return new WaitForSeconds(0.001f);
    }
}