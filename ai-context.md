# Lairen Arena - AI Context

## Project Overview

**Lairen Arena** is a multiplayer trading card game (TCG) built in Unity 2022, inspired by Magic: The Gathering. Lairen is a local collectible TCG with custom mechanics and card types.

### Technology Stack

- **Engine**: Unity 2022
- **Networking**: Mirror (Unity multiplayer networking framework)
- **Testing Tool**: ParrelSync (for local multiplayer testing with cloned Unity instances)
- **Architecture**: Client-Server with SyncVar state synchronization

---

## Project Structure

### Core Directories

- **`Assets/`** - Main Unity assets folder
  - **`Scripts/`** - Core game logic (61 files)
    - `GameController.cs` - Main game loop and server-authoritative logic
    - `Cards.cs` - Card definitions and card data structures
    - `Player.cs` - Player state management
    - `Abilities.cs` - Ability system with effect resolvers
    - `RPCManager.cs` - Network RPC handlers for client-server communication
    - Various view scripts (`CardView.cs`, `CardStackView.cs`, etc.)
  - **`Resources/`** - Unity Resources folder (674 items, likely card images/assets)
  - **`Scenes/`** - Unity scenes
    - `SampleScene.unity` - Main game scene
  - **`Mirror/`** - Mirror networking framework (2113 items)
  - **`Plugins/`** - Third-party plugins including ParrelSync
  - **`TextMesh Pro/`** - UI text rendering

### Key Configuration Files

- `LairenArena.sln` - Visual Studio solution
- `Packages/manifest.json` - Unity package dependencies

---

## Core Game Systems

### 1. Card System

**Location**: `Assets/Scripts/Cards.cs`

Cards are defined as serializable classes with the following properties:

- **Types**: Card types (e.g., `ALLY`, `ORDER`, `TREASURE`, `MONUMENT`)
- **Subtypes**: Creature subtypes (e.g., `ANIMAL`, `BRUJA`, `DESERTOR`)
- **Supertypes**: Additional categorization (e.g., `ROYALTY`)
- **Stats**: Power, Resistance, Cost, Points
- **Keywords**: Static keywords (e.g., `Frenzy`)
- **Abilities**: List of triggered/activated abilities
- **Zones**: Current zone and valid playable zones

**Card Types**:

- `ALLY` - Creatures that enter the Regroup zone (battlefield)
- `ORDER` - Spells that go to discard after resolution
- `TREASURE` - Resource cards (stored in Vault)
- `MONUMENT` - Permanent structures
- `FAST` - Modifier indicating instant-speed spells

**Example Cards** (hardcoded in `Cards.cs`):

- `ROJO_FUGAZ` - 2/2 Animal with Frenzy
- `SOMBRA_DEL_DESIERTO` - 3/3 Desertor/Bruja with ETB trigger
- `BRUJA_ELEMENTALISTA` - 2/3 Royalty Bruja
- `MUERTE_INMINENTE` - Fast destruction spell with life payment

### 2. Zone System

**Location**: `Assets/Scripts/Zones.cs`

Players have multiple zones (similar to MTG):

- **Kingdom** - Library/deck
- **Hand** - Cards in hand
- **Regroup** - Battlefield (where Allies exist)
- **Reserve** - Untapped resources (land pool)
- **Paid** - Tapped resources
- **Vault** - Treasure cards (resource deck, separate from main deck)
- **Discard** - Graveyard
- **Avernus** - Exile zone
- **Attackers** - Cards currently attacking
- **Blockers** - Virtual zone for blocking creatures
- **Stack** - The stack (for resolving spells/abilities)

### 3. Game State & Networking

**Location**: `Assets/Scripts/GameController.cs`, `Assets/Scripts/GameState.cs`

**Architecture**:

- Server-authoritative using Mirror's `NetworkBehaviour`
- Game state serialized as JSON via `SyncVar` for synchronization
- All game logic runs on the server; clients receive state updates via hooks

**Key Components**:

- `GameController` - Server-side game orchestrator
  - Manages priority passing
  - Handles mulligan phase
  - Validates card plays and payments
  - Resolves stack items
  - Updates state-based effects
- `GameState` - Serializable game state containing:
  - Players list
  - All cards in game
  - Current phase
  - Active player
  - The stack
  - Event queue

**Game Loop**:

1. **Setup** - Initialize players, deal starting hands
2. **Mulligan** - Players decide to keep or mulligan (with bottom cards penalty)
3. **Main Game Loop**:
   - Untap phase
   - Reveal phase
   - Main Phase 1
   - Attack phase (declare attackers, blockers, damage)
   - Main Phase 2
   - End phase
4. **Priority System** - Players pass priority to resolve stack items

### 4. Ability System

**Location**: `Assets/Scripts/Abilities.cs`

**Ability Structure**:

- **Effects**: List of effects to resolve
- **Trigger**: Game event that triggers the ability (optional)
- **ValidTargets**: Targeting restrictions

**Effect Types**:

- `Damage` - Deal damage to cards/players
- `GrantKeyword` - Permanently grant keyword
- `GrantTemporaryKeyword` - Grant keyword until end of turn
- `LoseLife` - Direct life loss
- `Destroy` - Destroy a target card
- `AddCounters` - Add counters to cards
- `Drain` - Damage opponent and gain life
- `ReturnToBattlefield` - Reanimate from discard

**Game Events**:

- `OnOrderPlayed`
- `OnSelfEntersBattlefield`
- `OnAnotherCardEntersBattlefield`
- `OnCardDefeated`

**Targeting System**:

- Flexible targeting with validation
- Supports distributed damage/counters
- Multi-target selection with confirmation

### 5. Player System

**Location**: `Assets/Scripts/Player.cs`

**Player State**:

- Zones (Hand, Kingdom, Reserve, etc.)
- Life total
- Priority/stack interaction flags
- Mulligan tracking
- Payment tracking
- Target selection

**Key Mechanics**:

- **Mulligan**: Players can mulligan multiple times, then bottom N cards (where N = mulligan count)
- **Resource System**: Reserve zone acts as mana pool (paid from Vault treasures)
- **Priority**: Active player gets priority first, passes around table
- **Payment**: Interactive payment system where players select cards to pay costs

### 6. View System

**Location**: Various `*View.cs` scripts in `Assets/Scripts/` and root `Assets/`

UI components that observe game state and render:

- `CardView.cs` - Individual card rendering
- `CardStackView.cs` - Stack of cards rendering
- `HandStackView.cs` - Hand display
- `BlockersStackView.cs` - Blockers display
- `PlayerView.cs` - Player board state
- `PhaseIndicator.cs` - Current phase display
- `MulliganUI.cs` - Mulligan interface

### 7. Network Architecture

**Location**: `Assets/CustomNetworkManager.cs`, `Assets/Scripts/RPCManager.cs`

**Mirror Networking**:

- Uses `CustomNetworkManager` extending Mirror's `NetworkManager`
- Client connects to host
- Game starts when 2 players are connected
- RPC calls for client actions (play card, pass priority, select targets, etc.)

**Synchronization**:

- `GameState` serialized to JSON and synced via `SyncVar`
- Hook method `OnGameStateUpdated` notifies clients of changes
- Clients render based on received state

---

## Development Workflow

### Testing with ParrelSync

**ParrelSync** allows running multiple Unity Editor instances from the same project:

1. Open Unity normally (acts as Host/Server)
2. Create ParrelSync clone (acts as Client)
3. Host starts game, client connects
4. Test multiplayer interactions locally

**ParrelSync Location**:

- Package: `com.veriorpies.parrelsync` (from GitHub)
- Settings: `Assets/Plugins/ParrelSync/ScriptableObjects/ParrelSyncProjectSettings.asset`

### Important Notes

- **Server Authority**: All game logic must run on server side
- **State Updates**: Use `UpdateGameState()` and `Propagate()` to sync state to clients
- **Coroutines**: Most game actions are coroutines for turn-based sequencing
- **Event Queue**: Events are queued and processed during priority rounds

---

## Key Design Patterns

### 1. Stack System

- Cards and abilities go on a stack (like MTG)
- Stack resolves top-to-bottom with priority passing
- Players get priority after each stack item resolves

### 2. Speed System

- **SLOW** actions: Can only be played during main phase with empty stack
- **FAST** actions: Can be played any time with priority

### 3. Card Cloning

- Card definitions are static templates
- Each card in game is a deep clone with unique `InGameId`
- Cloning uses binary serialization for deep copy

### 4. Rule Enforcement

- Server validates all actions
- Clients can only request actions via RPC
- State-based effects checked during priority rounds

---

## Current Card Pool

Sample cards defined in `Cards.cs`:

- **Allies (9)**: ROJO_FUGAZ, SOMBRA_DEL_DESIERTO, BRUJA_ELEMENTALISTA, ANCIANA_MAESTRA, LIDER_DE_LA_MANADA, FELINO_DE_LA_MONTANA, GATITOS_DE_BRUJA, CASCABUFALO, NICOL_LA_APRENDIZ
- **Orders (4)**: CIUDAD_EN_LLAMAS, MUERTE_INMINENTE, PLANES_FRUSTRADOS, RITUAL_DE_NEGACION
- **Monuments (1)**: CUMULO_DE_HONGOS
- **Treasures**: TreasureGenerico (generic resource)

**Sample Deck** (`Decks.cs`):

- 45 cards from predefined card pool

---

## Dependencies

### Unity Packages (from `Packages/manifest.json`)

- Unity Collaboration: `com.unity.collab-proxy` (2.7.1)
- Unity IDE Cursor integration: Custom GitHub package
- TextMeshPro: `com.unity.textmeshpro` (3.0.7)
- Newtonsoft JSON: `com.unity.nuget.newtonsoft-json` (3.2.1)
- ParrelSync: `com.veriorpies.parrelsync` (from GitHub)
- Unity UI: `com.unity.ugui` (1.0.0)
- Visual Scripting: `com.unity.visualscripting` (1.9.4)

### Mirror Networking

- Mirror core framework (included in Assets/Mirror)
- Multiple csproj files for Mirror components:
  - `Mirror.csproj`
  - `Mirror.Authenticators.csproj`
  - `Mirror.Components.csproj`
  - `Mirror.Editor.csproj`
  - `Mirror.Examples.csproj`
  - `Mirror.Transports.csproj`
  - `Telepathy.csproj` (TCP transport)
  - `SimpleWebTransport.csproj` (WebGL transport)
  - `kcp2k.csproj` (KCP transport)

---

## Common Tasks

### Adding a New Card

1. Define card in `Cards.cs` as a static `Card` object
2. Set types, subtypes, stats, keywords
3. Create abilities in `Abilities.AllAbilities` if needed
4. Add card to deck list in `Decks.cs`
5. Add card art to `Assets/Resources/Cards/`

### Adding a New Ability

1. Define ability in `Abilities.AllAbilities` dictionary
2. Create effect(s) with targeting info
3. Add effect resolver to `EffectResolvers` if new effect type
4. Implement resolver method (takes Effect, targets, Stackable)

### Adding a New Keyword

1. Add to `Keywords.cs` enum
2. Implement keyword logic in relevant systems:
   - Combat (e.g., Frenzy = double strike)
   - Timing restrictions
   - Targeting

### Debugging Multiplayer

1. Use Debug.Log extensively (check both Host and Client console)
2. Verify server-side state changes
3. Confirm `Propagate()` is called after state changes
4. Check RPC calls are reaching server
5. Validate client receives state updates via hook

---

## Architecture Diagrams

### Game Flow

```
Setup → Mulligan → Game Loop (Phases) → Win/Loss
                        ↓
              Priority Rounds (Stack Resolution)
```

### Network Flow

```
Client Action (UI) → RPC to Server → Server Validates → 
Server Updates State → State Synced → Client Renders
```

---

## Notes for AI

- All game rules are server-authoritative
- Card data is currently hardcoded, not data-driven
- Resource system uses a separate Vault deck (15 treasures)
- Game is designed for exactly 2 players
- Starting life: 20
- Deck size: 45 cards (Kingdom) + 15 treasures (Vault)
- Deep cloning is used for card instances (memory intensive)
- Most methods return IEnumerator for coroutine-based sequencing

---

## Future Considerations

- More card types and mechanics
- Help With Debugging
- Better player feedback
- Test and implement card abilities.
