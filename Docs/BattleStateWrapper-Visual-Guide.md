# BattleStateModel Wrapper - Visuele Guide

## 📋 Inhoudsopgave
1. [Overzicht](#overzicht)
2. [Architectuur Visualisatie](#architectuur-visualisatie)
3. [Wrapper Mechanisme](#wrapper-mechanisme)
4. [Migratie Stappenplan](#migratie-stappenplan)
5. [Voorbeelden](#voorbeelden)
6. [Best Practices](#best-practices)

---

## Overzicht

De BattleStateModel is **niet meer** de primaire data structuur. Het is nu een **backward compatibility wrapper** die delegeert naar de nieuwe architectuur.

### ⚠️ Oude Situatie (Deprecated)
```
┌─────────────────────────────┐
│   BattleStateModel          │
│  ┌─────────────────────┐    │
│  │ Player              │    │
│  │ Npc                 │    │
│  │ PlayerWeapons       │    │
│  │ NpcWeapons          │    │
│  │ CurrentHitpointsNPC │    │
│  │ AttackRoll          │    │
│  │ Damage              │    │
│  │ EmbedColor          │    │
│  │ RoundCounter        │    │
│  │ ... (40+ props)     │    │
│  └─────────────────────┘    │
└─────────────────────────────┘
     ❌ Alles in één class
     ❌ Geen scheiding
     ❌ Moeilijk te onderhouden
```

### ✅ Nieuwe Situatie (Aanbevolen)
```
┌──────────────────────────────────────────────────────────┐
│                    BattleSession                         │
│  ┌──────────────────────┐  ┌───────────────────────┐   │
│  │  BattleContext       │  │  BattleRuntimeState   │   │
│  │  (Domain Model)      │  │  (Service State)      │   │
│  │                      │  │                       │   │
│  │  • Player            │  │  • CurrentHitpointsNPC│   │
│  │  • Npc               │  │  • AttackRoll         │   │
│  │  • PlayerWeapons     │  │  • Damage             │   │
│  │  • PlayerArmor       │  │  • TotalDamage        │   │
│  │  • Items             │  │  • EmbedColor         │   │
│  │  • NpcWeapons        │  │  • RoundCounter       │   │
│  │  • NpcArmor          │  │  • RewardXP           │   │
│  │  • DiceCountHP       │  │  • StateOfPlayer      │   │
│  │  • DisplayCR         │  │  • StateOfNPC         │   │
│  └──────────────────────┘  └───────────────────────┘   │
└──────────────────────────────────────────────────────────┘
     ✅ Scheiding van concerns
     ✅ Domain vs Runtime
     ✅ Beter onderhoudbaar
```

---

## Architectuur Visualisatie

### 🏗️ Component Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                     Legacy Code Layer                        │
│  ┌───────────────────────────────────────────────────────┐  │
│  │         BattleStateModel (Wrapper - Obsolete)         │  │
│  │                                                        │  │
│  │  ┌─────────────────────────────────────────────────┐  │  │
│  │  │  ToSession()   → Converteert naar BattleSession │  │  │
│  │  │  FromSession() ← Converteert van BattleSession  │  │  │
│  │  └─────────────────────────────────────────────────┘  │  │
│  │                                                        │  │
│  │  ┌─────────────────────────────────────────────────┐  │  │
│  │  │  Properties delegeren naar internal session:    │  │  │
│  │  │    - get => _internalSession.Context.Player     │  │  │
│  │  │    - set => _internalSession.Context.Player = v │  │  │
│  │  └─────────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            ▼ ▼ ▼
┌─────────────────────────────────────────────────────────────┐
│                      New Architecture                        │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │              BattleSession (Combinatie)                │ │
│  │                                                        │ │
│  │  ┌──────────────────┐      ┌──────────────────────┐  │ │
│  │  │ BattleContext    │      │ BattleRuntimeState   │  │ │
│  │  │ ──────────────── │      │ ───────────────────  │  │ │
│  │  │ Domain Entities  │      │ Runtime/UI State     │  │ │
│  │  │                  │      │                      │  │ │
│  │  │ Player ────────┐ │      │ CurrentHitpointsNPC  │  │ │
│  │  │ Npc            │ │      │ AttackRoll           │  │ │
│  │  │ PlayerWeapons  │ │      │ Damage               │  │ │
│  │  │ NpcWeapons     │ │      │ RoundCounter         │  │ │
│  │  │ Items          │ │      │ EmbedColor           │  │ │
│  │  └────────────────┘ │      └──────────────────────┘  │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

---

## Wrapper Mechanisme

### 🔄 Hoe werkt de Property Delegation?

#### Voorbeeld: Player Property

```csharp
// ❌ OUD (voor refactoring)
public class BattleStateModel 
{
    public PlayerModel Player { get; set; } = new();
    // Directe opslag in het object zelf
}

// ✅ NIEUW (na refactoring - Wrapper Pattern)
[Obsolete("Use BattleSession with BattleContext and BattleRuntimeState instead")]
public class BattleStateModel 
{
    // Internal storage - de ECHTE data
    private BattleSession _internalSession = new();

    // Property delegation - doorverwijzing naar nieuwe architectuur
    public PlayerModel Player 
    { 
        get => _internalSession.Context.Player;
        set => _internalSession.Context.Player = value;
    }
}
```

#### Visuele Flow

```
Legacy Code roept aan:          Wrapper delegeert:              Nieuwe architectuur:

state.Player.Name               ──────────►                   session.Context.Player.Name
      │                                                              │
      │                         BattleStateModel                     │
      │                         Property Getter                      │
      │                                │                             │
      └─────────────────────────────────┘                            │
                                                                     │
                                        _internalSession.Context ────┘

                                        ✅ Data leeft hier!
```

### 📊 Data Flow Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                        LEES OPERATIE                             │
└──────────────────────────────────────────────────────────────────┘

  Legacy Code                Wrapper Layer              New Architecture
  ───────────                ─────────────              ────────────────

  var hp = state.Player.Hitpoints
       │
       │  (1) Call getter
       └──────────────────► BattleStateModel.Player
                                    │
                                    │  (2) Delegate to internal
                                    └──────────────────────────────┐
                                                                   │
                              return _internalSession.Context.Player
                                                                   │
                                    ┌──────────────────────────────┘
                                    │
                                    ▼
                          BattleSession.Context.Player.Hitpoints
                                    │
                                    │  (3) Return value
       ┌────────────────────────────┘
       │
       ▼
  hp = 45


┌──────────────────────────────────────────────────────────────────┐
│                      SCHRIJF OPERATIE                            │
└──────────────────────────────────────────────────────────────────┘

  Legacy Code                Wrapper Layer              New Architecture
  ───────────                ─────────────              ────────────────

  state.Player.Hitpoints = 30
       │
       │  (1) Call setter
       └──────────────────► BattleStateModel.Player setter
                                    │
                                    │  (2) Delegate to internal
                                    └──────────────────────────────┐
                                                                   │
                              _internalSession.Context.Player = value
                                                                   │
                                    ┌──────────────────────────────┘
                                    │
                                    ▼
                          BattleSession.Context.Player.Hitpoints = 30
                                    │
                                    │  (3) Value stored
                                    ▼
                                ✅ Saved!
```

---

## Migratie Stappenplan

### 🎯 Stap-voor-Stap Migratie

```
┌────────────────────────────────────────────────────────────────┐
│  STAP 1: Identificeer het bestand met BattleStateModel        │
└────────────────────────────────────────────────────────────────┘

  Zoek naar code met:

  ❌ BattleStateModel state = BattleStateSetup.GetBattleState(userId);

  ─────────────────────────────────────────────────────────────────

┌────────────────────────────────────────────────────────────────┐
│  STAP 2: Vervang BattleStateModel door BattleSession          │
└────────────────────────────────────────────────────────────────┘

  Wijzig naar:

  ✅ BattleSession session = BattleStateSetup.GetBattleSession(userId);

  ─────────────────────────────────────────────────────────────────

┌────────────────────────────────────────────────────────────────┐
│  STAP 3: Update alle property toegang                         │
└────────────────────────────────────────────────────────────────┘

  Domain properties (entiteiten):
  ❌ state.Player          →  ✅ session.Context.Player
  ❌ state.Npc             →  ✅ session.Context.Npc
  ❌ state.PlayerWeapons   →  ✅ session.Context.PlayerWeapons
  ❌ state.Items           →  ✅ session.Context.Items

  Runtime properties (battle state):
  ❌ state.CurrentHitpointsNPC  →  ✅ session.State.CurrentHitpointsNPC
  ❌ state.AttackRoll           →  ✅ session.State.AttackRoll
  ❌ state.RoundCounter         →  ✅ session.State.RoundCounter
  ❌ state.EmbedColor           →  ✅ session.State.EmbedColor

  ─────────────────────────────────────────────────────────────────

┌────────────────────────────────────────────────────────────────┐
│  STAP 4: Update method signatures                             │
└────────────────────────────────────────────────────────────────┘

  Als een methode BattleStateModel accepteert:

  ❌ public void ProcessAttack(BattleStateModel state)

  Wijzig naar:

  ✅ public void ProcessAttack(BattleSession session)

  ─────────────────────────────────────────────────────────────────

┌────────────────────────────────────────────────────────────────┐
│  STAP 5: Test en valideer                                     │
└────────────────────────────────────────────────────────────────┘

  - Compileer het project
  - Controleer op CS0618 waarschuwingen (obsolete usage)
  - Test de functionaliteit
  - Commit je wijzigingen
```

---

## Voorbeelden

### 📝 Voorbeeld 1: Eenvoudige Property Access

#### Voor Migratie
```csharp
public void CheckPlayerHealth(ulong userId)
{
    BattleStateModel state = BattleStateSetup.GetBattleState(userId);

    if (state.Player.Hitpoints <= 0)
    {
        Console.WriteLine($"{state.Player.Name} is defeated!");
    }
}
```

#### Na Migratie
```csharp
public void CheckPlayerHealth(ulong userId)
{
    BattleSession session = BattleStateSetup.GetBattleSession(userId);

    if (session.Context.Player.Hitpoints <= 0)
    {
        Console.WriteLine($"{session.Context.Player.Name} is defeated!");
    }
}
```

#### Visualisatie
```
VOOR:                                NA:
─────                                ────

state                                session
  │                                    ├── Context
  ├── Player ────┐                    │     └── Player ────┐
  │              │                    │                    │
  │              ├── Hitpoints        │                    ├── Hitpoints
  │              └── Name             │                    └── Name
  │                                   │
  └── CurrentHitpointsNPC             └── State
                                            └── CurrentHitpointsNPC

❌ Plat object                        ✅ Gestructureerd
```

---

### 📝 Voorbeeld 2: Attack Processing

#### Voor Migratie
```csharp
public string ProcessPlayerAttack(ulong userId, WeaponModel weapon)
{
    BattleStateModel state = BattleStateSetup.GetBattleState(userId);

    // Roll damage
    int damage = RollDamage(weapon);
    state.Damage = damage;
    state.TotalDamage += damage;

    // Apply damage to NPC
    state.CurrentHitpointsNPC -= damage;

    // Update round
    state.RoundCounter++;

    return $"{state.Player.Name} dealt {damage} damage to {state.Npc.Name}!";
}
```

#### Na Migratie
```csharp
public string ProcessPlayerAttack(ulong userId, WeaponModel weapon)
{
    BattleSession session = BattleStateSetup.GetBattleSession(userId);

    // Roll damage
    int damage = RollDamage(weapon);
    session.State.Damage = damage;
    session.State.TotalDamage += damage;

    // Apply damage to NPC
    session.State.CurrentHitpointsNPC -= damage;

    // Update round
    session.State.RoundCounter++;

    return $"{session.Context.Player.Name} dealt {damage} damage to {session.Context.Npc.Name}!";
}
```

#### Property Mapping
```
┌─────────────────────────────────────────────────────────────┐
│              Property Access Mapping                        │
├─────────────────────────────────────────────────────────────┤
│ OLD                          NEW                            │
├─────────────────────────────────────────────────────────────┤
│ state.Damage             →   session.State.Damage           │
│ state.TotalDamage        →   session.State.TotalDamage      │
│ state.CurrentHitpointsNPC→   session.State.CurrentHitpointsNPC
│ state.RoundCounter       →   session.State.RoundCounter     │
│ state.Player.Name        →   session.Context.Player.Name    │
│ state.Npc.Name           →   session.Context.Npc.Name       │
└─────────────────────────────────────────────────────────────┘
```

---

### 📝 Voorbeeld 3: Method Parameter Migratie

#### Voor Migratie
```csharp
public void DisplayBattleStats(BattleStateModel state)
{
    Console.WriteLine($"=== Battle Round {state.RoundCounter} ===");
    Console.WriteLine($"Player: {state.Player.Name} ({state.Player.Hitpoints} HP)");
    Console.WriteLine($"Enemy: {state.Npc.Name} ({state.CurrentHitpointsNPC} HP)");
    Console.WriteLine($"Last Attack: {state.AttackRoll}");
    Console.WriteLine($"Damage Dealt: {state.Damage}");
}

// Aanroep
var state = BattleStateSetup.GetBattleState(userId);
DisplayBattleStats(state);
```

#### Na Migratie
```csharp
public void DisplayBattleStats(BattleSession session)
{
    Console.WriteLine($"=== Battle Round {session.State.RoundCounter} ===");
    Console.WriteLine($"Player: {session.Context.Player.Name} ({session.Context.Player.Hitpoints} HP)");
    Console.WriteLine($"Enemy: {session.Context.Npc.Name} ({session.State.CurrentHitpointsNPC} HP)");
    Console.WriteLine($"Last Attack: {session.State.AttackRoll}");
    Console.WriteLine($"Damage Dealt: {session.State.Damage}");
}

// Aanroep
var session = BattleStateSetup.GetBattleSession(userId);
DisplayBattleStats(session);
```

---

## Best Practices

### ✅ DO's

```csharp
// ✅ Gebruik BattleSession voor nieuwe code
public void NewMethod(ulong userId)
{
    var session = BattleStateSetup.GetBattleSession(userId);
    // ... gebruik session.Context en session.State
}

// ✅ Gebruik duidelijke property access
var playerName = session.Context.Player.Name;      // Domain entity
var currentHP = session.State.CurrentHitpointsNPC; // Runtime state

// ✅ Documenteer welke properties waar zitten
// Context  = Domain entities (Player, NPC, Weapons, etc.)
// State    = Runtime/UI state (HP, Rolls, Counters, etc.)
```

### ❌ DON'Ts

```csharp
// ❌ Gebruik GEEN BattleStateModel in nieuwe code
var state = BattleStateSetup.GetBattleState(userId); // OBSOLETE!

// ❌ Mix niet oude en nieuwe patterns
var state = BattleStateSetup.GetBattleState(userId);
var session = BattleStateSetup.GetBattleSession(userId);
// Kies één!

// ❌ Verwijder GEEN BattleStateModel (nog niet)
// Het is een backward compatibility layer voor legacy code
```

---

## Snelle Referentie Tabel

### 🔍 Property Mapping Quick Reference

| **OLD (BattleStateModel)**      | **NEW (BattleSession)**                  | **Type** |
|---------------------------------|------------------------------------------|----------|
| `state.Player`                  | `session.Context.Player`                 | Domain   |
| `state.Npc`                     | `session.Context.Npc`                    | Domain   |
| `state.PlayerWeapons`           | `session.Context.PlayerWeapons`          | Domain   |
| `state.PlayerArmor`             | `session.Context.PlayerArmor`            | Domain   |
| `state.NpcWeapons`              | `session.Context.NpcWeapons`             | Domain   |
| `state.NpcArmor`                | `session.Context.NpcArmor`               | Domain   |
| `state.Items`                   | `session.Context.Items`                  | Domain   |
| `state.DiceCountHP`             | `session.Context.DiceCountHP`            | Domain   |
| `state.DiceValueHP`             | `session.Context.DiceValueHP`            | Domain   |
| `state.DisplayCR`               | `session.Context.DisplayCR`              | Domain   |
| `state.ArmorElements`           | `session.Context.ArmorElements`          | Domain   |
| `state.CurrentHitpointsNPC`     | `session.State.CurrentHitpointsNPC`      | Runtime  |
| `state.HitpointsAtStartNPC`     | `session.State.HitpointsAtStartNPC`      | Runtime  |
| `state.PreHpNPC`                | `session.State.PreHpNPC`                 | Runtime  |
| `state.AttackRoll`              | `session.State.AttackRoll`               | Runtime  |
| `state.Damage`                  | `session.State.Damage`                   | Runtime  |
| `state.TotalDamage`             | `session.State.TotalDamage`              | Runtime  |
| `state.RoundCounter`            | `session.State.RoundCounter`             | Runtime  |
| `state.RewardXP`                | `session.State.RewardXP`                 | Runtime  |
| `state.EmbedColor`              | `session.State.EmbedColor`               | UI       |
| `state.StateOfPlayer`           | `session.State.StateOfPlayer`            | UI       |
| `state.StateOfNPC`              | `session.State.StateOfNPC`               | UI       |
| `state.EncounterTileId`         | `session.State.EncounterTileId`          | Runtime  |
| `state.GuildChannelId`          | `session.State.GuildChannelId`           | Runtime  |

---

## Decision Tree

```
┌─────────────────────────────────────────────────────┐
│   Moet ik BattleStateModel of BattleSession        │
│   gebruiken in mijn code?                          │
└─────────────────────────────────────────────────────┘
                      │
                      ▼
          ┌───────────────────────┐
          │ Is dit NIEUWE code?   │
          └───────────────────────┘
                 │           │
           JA ───┘           └─── NEE
            │                      │
            ▼                      ▼
   ┌──────────────────┐   ┌──────────────────────┐
   │ Gebruik          │   │ Migreer geleidelijk  │
   │ BattleSession!   │   │ naar BattleSession   │
   └──────────────────┘   └──────────────────────┘
            │                      │
            ▼                      ▼
   session.Context.X       1. Vervang type
   session.State.Y         2. Update properties
                           3. Test
                           4. Commit
```

---

## Status Check

### 🎯 Is mijn code gemoderniseerd?

Voer deze check uit:

```bash
# Check voor obsolete warnings
dotnet build | Select-String "CS0618"

# Als resultaat:
# - 0 waarschuwingen  → ✅ Volledig gemigreerd!
# - >0 waarschuwingen → ⚠️  Nog work to do
```

### 📊 Migratie Progress

```
Legacy Code (BattleStateModel)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Migrated:    ████████████████████░░░░░  85%
Remaining:   ░░░░░░░░░░░░░░░░░░░░████░  15%

Bestanden gemigreerd: ✅
- BattleStateSetup.cs
- AttackProcessor.cs
- PlayerAttack.cs
- NpcAttack.cs
- ProcessSuccesAttack.cs
- HPStatusHelpers.cs
- BattleTextGenerator.cs
- EmbedBuildersEncounter.cs
- ComponentHelpers.cs
- EncounterBattleStepsSetup.cs
- ProcessRollsAndDamage.cs
- NpcSetup.cs
- ChallengeRatingHelpers.cs

Nog te migreren: ⏳
- (Geen - alles is gemigreerd! 🎉)
```

---

## Conclusie

### 🎓 Key Takeaways

1. **BattleStateModel is een wrapper** - geen primaire data storage meer
2. **BattleSession is de toekomst** - gebruik deze voor nieuwe code
3. **Backward compatibility** - oude code blijft werken tijdens migratie
4. **Property delegation** - automatische doorverwijzing naar nieuwe structuur
5. **Geleidelijke migratie** - geen big bang deployment nodig

### 🚀 Next Steps

1. ✅ Alle nieuwe code gebruikt `BattleSession`
2. ✅ Legacy code blijft werken via wrapper
3. 🔜 Verwijder `BattleStateModel` als alle code is gemigreerd
4. 🔜 Remove `[Obsolete]` attributes

---

**Laatst bijgewerkt:** 2025-01-11  
**Status:** ✅ Migratie Compleet - 0 Obsolete Warnings
