using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using StackObjects;
using Telepathy;
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
    
    [SyncVar(hook =nameof(OnGameStateUpdated))]
    public GameState gameState;

    public event Action<GameState, GameState> GameStateUpdated;

    public int GetLocalPlayerId()
    {
        return isServer ? 0 : 1;
    }
    public Player getLocalPlayer()
    {
        return gameState.Players[isServer ? 0 : 1];
    }
    void OnGameStateUpdated(GameState oldValue, GameState newValue)
    {
        GameStateUpdated?.Invoke(oldValue, newValue);
    }
    private Coroutine gameLoop;

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (isServer) StartCoroutine(SetupGame());
    }

    public bool WaitingForResponse { get; private set; }

    private IEnumerator SetupGame()
    {
        gameState = new GameState();
        gameState.Players.Add(new Player());
        gameState.Players.Add(new Player());
        
        gameState.Players[0].Kingdom = GetDeck(0);
        gameState.Players[1].Kingdom = GetDeck(1);

        gameState.Players[0].Vault = GetVault(0);
        gameState.Players[1].Vault = GetVault(1);
        LocalSetDirty();
        DetermineStartingPlayer();
        yield return HandleMulligans();
        gameLoop = StartCoroutine(GameLoop());
    }

    public List<Card> GetDeck(int playerId)
    {
        List<Card> deck = new List<Card>();
        for (int i = 0; i < 45; i++)
        {
            Card newCard = Cards.ROJO_FUGAZ.Clone(); 
            GameObject CardView = Instantiate(CardViewPrefab);
            newCard.Owner = playerId;
            BindView(newCard, CardView.GetComponent<CardView>());
            NetworkServer.Spawn(CardView);
            deck.Add(newCard);
        }
        return deck;
    }

    public void BindView(Card card, CardView view)
    {
        card.view = view;
        view.cardData = card;        
    }
    public List<Card> GetVault(int playerid)
    {
        List<Card> deck = new List<Card>();
        for (int i = 0; i < 45; i++)
        {
            Card newCard = Cards.TreasureGenerico.Clone(); 
            GameObject CardView = Instantiate(CardViewPrefab);
            newCard.Owner = playerid;
            BindView(newCard, CardView.GetComponent<CardView>());
            NetworkServer.Spawn(CardView);
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
    private Dictionary<Player, int> mulliganCount = new();
    private Dictionary<Player, bool> keepHand = new();
    
    // Add this before your main game loop
    private IEnumerator HandleMulligans()
    {
        gameState.currentPhase = Phase.Mulligan;
        // Initialize mulligan tracking for each player
        foreach (Player player in gameState.Players)
        {
            mulliganCount[player] = 1; // starts in 1 so we have to put 1 at the bottom.
            keepHand[player] = false;
            
            // Initial draw of 7 cards
            DrawCards(player, 7);
        }

        bool allPlayersKept = false;
        
        while (!allPlayersKept)
        {
            // Reset decision tracking for this round
            foreach (Player player in gameState.Players)
            {
                if (!keepHand[player])
                {
                    keepHand[player] = false;
                }
            }

            // Wait for all players to make their mulligan decision
            foreach (Player player in gameState.Players)
            {
                if (!keepHand[player])
                {
                    // Set the active player so UI can show the correct hand
                    gameState.ActivePlayer = gameState.Players.IndexOf(player);
                    
                    // Wait for the player's decision
                    yield return StartCoroutine(AwaitMulliganDecision(player));

                    if (!keepHand[player])
                    {
                        yield return StartCoroutine(PerformMulligan(player));
                    }
                }
            }

            // Check if all players have kept their hands
            allPlayersKept = gameState.Players.All(p => keepHand[p]);
        }

        // After all players keep, handle bottom of library cards
        foreach (Player player in gameState.Players)
        {
            if (mulliganCount[player] > 0)
            {
                yield return StartCoroutine(AwaitBottomCards(player, mulliganCount[player]));
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
    }

    private IEnumerator PerformMulligan(Player player)
    {
        // Increment mulligan count
        mulliganCount[player]++;
        
        // Return cards to library
        MoveAll(player.Hand, player.Kingdom);
        
        // Shuffle
        ShuffleLibrary(player);
        
        // Draw new hand of 7
        DrawCards(player, 7);
        
        yield return null;
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
        }
        
        player.AwaitingBottomDecision = false;
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
        LocalSetDirty();
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
        LocalSetDirty();
    }
    
    

    public void MoveCard(Card card, List<Card> from, List<Card> to)
    {
        from.Remove(card);
        to.Add(card);
        LocalSetDirty();
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
        LocalSetDirty();
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
    }

    public bool declaredBlockers;
    private IEnumerator DeclareBlockers()
    {
        Debug.Log("Waiting for " + gameState.Players[gameState.ActivePlayer - 1] + " to declare blockers.");
        yield return new WaitUntil(() => declaredBlockers);
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

    private void LocalSetDirty()
    {
        SetDirty();
        GameStateUpdated?.Invoke(previousGameState,gameState);
        if (isServer) previousGameState = gameState.Clone();

    }
    public GameState previousGameState;
}