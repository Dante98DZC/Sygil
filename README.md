# Sygil

A Unity 2D top-down game featuring a tile-based physics simulation engine where player abilities modify the simulation itself to create emergent gameplay effects.

## Overview

Sygil is a game where the player's powers are **modifiers of the simulation itself**, not pre-scripted visual effects. A power can:

- **Inject a temporary rule** into the simulation (e.g., a rule that solidifies GAS in a 3-tile radius for 5 ticks)
- **Modify material coefficients** in an area (e.g., make WOOD non-flammable, or triple heat transfer)
- **Suppress or enhance diffusion** in a zone (e.g., freeze thermal diffusion, creating a thermal bubble)
- **Alter tile properties directly** (e.g., inject temperature, electricity, or gas density)

The result is **emergent gameplay**: interactions with the world in ways neither player nor designer can fully predict. Lighting fire on a tile with water above produces steam; steam increases gas density; if flammable gas is nearby, it ignites. The player manipulates causes, not effects.

## Features

### Physics Simulation
- **16 Interaction Rules** — combustion, phase transitions, pressure dynamics, electricity, evaporation
- **4-Layer Tile Model** — terrain (ground), liquid, atmosphere (gas), entities
- **4 Tick Frequencies** — FAST (0.1s), STANDARD (0.3s), SLOW (0.5s), INTEGRITY (1.0s)

### Phase Transitions (R13-R16)
- **R13 Melting** — solid → liquid (groundMaterial melts at meltingPoint)
- **R14 Freezing** — liquid → solid (liquid freezes at freezingPoint)
- **R15 Boiling** — liquid → gas (liquid boils at boilingPoint)
- **R16 Condensation** — gas → liquid (gas condenses at condensationPoint)

### Power System
- **Design Phase** — build powers in a visual node editor (future)
- **Compile Phase** — graph compiles to a `CompiledPower` ScriptableObject
- **Runtime Phase** — player activates pre-designed modifiers; cannot modify arbitrarily
- **Deterministic** — compiled powers are read-only at runtime (invariant I10)

### Debug Tools
- **SimulationDebugHUD** — pause/resume, step by tick type
- **TileInspector** — view exact tile properties under cursor
- **RuleEventLog** — log of rule triggers, ignition pause

## Architecture

### TileData v4

Each tile has 4 independent layers plus shared properties:

```csharp
struct TileData
{
    // Terrain layer (solid floor/walls)
    MaterialType groundMaterial;    // STONE, EARTH, WOOD, ICE, SAND...
    TileHeight height;            // Ground(0), Wall(2), Deep(-2)...
    float structuralIntegrity;   // [0..100]
    float electricEnergy;         // [0..100]

    // Liquid layer
    MaterialType liquidMaterial; // WATER, LAVA, MUD...
    float liquidVolume;            // liters [0..LiquidCapacity]

    // Atmosphere layer
    MaterialType gasMaterial;     // STEAM, SMOKE, CO2...
    float gasDensity;              // [0..100], 50 = 1 atm baseline

    // Shared across layers
    float temperature;            // [0..100]

    // Entities
    int primaryEntityID;          // exclusive: Structure/Creature/Player
    int[] secondaryEntityIDs;     // non-exclusive: Item/Effect
}
```

**Key eliminations in v4:**
- `pressure` — now derived from `gasDensity - gasBaseline`
- `humidity` — now represented by `liquidVolume` + `gasMaterial == STEAM`

### Pipeline

Each simulation tick executes in order:

1. **Frozen Snapshot** — tiles see neighbor state from previous tick (determinism)
2. **Rules** — 16 rules run based on their tick type
3. **Diffusion** — gradient, gravity, pressure strategies spread properties
4. **Derived States** — compute ON_FIRE, WET, etc.
5. **Notification** — emit events to renderer/debug
6. **Decay** — temperature, gasDensity, electricEnergy decay toward baselines
7. **Cleanup** — clear dirty flags, deactivate stable tiles

## Getting Started

### Requirements
- Unity 2021.3 LTS or newer
- 2D template

### Opening the Project
1. Open Unity Hub
2. Add → select `Sygil/` folder
3. Open with desired Unity version

### Running Tests
```
Window → General → Test Runner
```
- **EditMode** — 78 tests (rule unit tests)
- **PlayMode** — 10 tests (behavior integration tests)

### In-Editor Controls
| Key | Action |
|-----|--------|
| P | Pause/resume simulation |
| F | Step FAST tick |
| S | Step STANDARD tick |
| L | Step SLOW tick |
| I | Step INTEGRITY tick |
| 0-9 | Toggle property overlays |

### Property Overlays
| Key | Mode | Source |
|-----|------|--------|
| 1 | Temperature | `temperature` |
| 2 | Pressure (gas) | `gasDensity` |
| 3 | Liquid | `liquidVolume` |
| 4 | Electricity | `electricEnergy` |
| 5 | Gas | `gasDensity` |
| 6 | Structural damage | `structuralIntegrity` (inverted) |
| 7 | Derived states | `derivedStates` |
| 8 | Activity | ActiveTiles |
| 9 | Combined | additive blend |

## Project Structure

```
Assets/PhysicsSystem/
├── Core/                       # Engine components
│   ├── SimulationEngine.cs     # Main simulation loop
│   ├── PhysicsGrid.cs          # Tile storage and access
│   ├── TileData.cs            # 4-layer tile structure
│   ├── MaterialDefinition.cs  # Material properties
│   ├── DecaySystem.cs         # Property decay
│   └── HeightmapGenerator.cs   # World generation
│
├── Rules/                     # Physics rules
│   ├── IInteractionRule.cs   # Rule interface
│   ├── RuleRegistry.cs       # Rule management
│   └── Rules/
│       ├── R01_Combustion.cs      # Fire spread
│       ├── R02_Evaporation.cs     # Heat → steam
│       ├── R03_ElectricPropagation.cs
│       ├── R04_ElectricWater.cs
│       ├── R05_PressureExplosion.cs
│       ├── R06_PressureRelease.cs
│       ├── R07_StructuralCollapse.cs
│       ├── R08_HumidityVaporization.cs
│       ├── R09_HeatSuppression.cs
│       ├── R10_GasIgnition.cs
│       ├── R11_GasProduction.cs
│       ├── R12_GasPressure.cs
│       └── R_PhaseTransitions.cs   # R13-R16
│
├── Diffusion/                 # Property spread
│   ├── GradientDiffusion.cs   # heat, electricity
│   ├── GravityDiffusion.cs   # liquids
│   └── PressureDiffusion.cs # gas pressure
│
├── States/                   # Derived computation
│   ├── StateFlags.cs         # ON_FIRE, WET...
│   └── DerivedStateComputer.cs
│
├── Renderer/                 # Visuals
│   ├── SimulationRenderer.cs
│   └── Overlay/
│
├── Powers/                  # Player abilities
│   ├── CompiledPower.cs
│   ├── PowerCaster.cs
│   └── SimulationModifierRegistry.cs
│
├── Debug/                  # Development tools
│   ├── SimulationDebugHUD.cs
│   ├── TileInspector.cs
│   └── RuleEventLog.cs
│
├── Player/                # Input/camera
│   ├── PlayerActions.cs
│   ├── PlayerActionChannel.cs
│   └── CameraController.cs
│
├── Config/                # Settings
│   ├── SimulationConfig.cs
│   └── MaterialLibrary.cs
│
├── Bridge/               # Events
│   ├── EngineNotifier.cs
│   └── TileUpdateEvent.cs
│
└── Tests/                # Test suites
    ├── Editor/            # 78 EditMode tests
    └── PlayMode/          # 10 PlayMode tests
```

## Key Invariants

```
I1:  simulation_never_reads_derivedStates
I2:  rules_write_properties_only (R13-R16 write layers — documented exception)
I3:  player_actions_write_properties_only
I4:  all_property_writes_clamped [0..100]
I5:  engine_systems_never_write_to_TileData
I6:  no_MonoBehaviour_in_Core_or_Rules
I7:  RuleRegistry_is_the_only_rule_caller
I8:  decay_runs_after_diffusion_always
I9:  height_is_geometry_not_physics
I10: CompiledPower_is_readonly_at_runtime
I11: OverlayColorizer_is_stateless
I12: new_rules_write_layer_slots_directly
I13: pressure_is_derived — read gasDensity directly
I14: humidity_is_derived — use liquidVolume + STEAM
```

## Documentation

- **Full context**: `.vscode/LLMAgent/AgentContext.md`
- **Agent instructions**: `AGENTS.md`
- **API warnings**: AGENTS.md includes critical v4 API notes

## Tech Stack

- **Engine**: Unity 2021+ (2D)
- **Language**: C#
- **Testing**: NUnit (Unity Test Framework)
- **No assembly definitions** — all code in `Assembly-CSharp`

## License

MIT — See LICENSE file for details.

## Contributing

1. Open in Unity Editor
2. Run tests before committing (`Window → General → Test Runner`)
3. Follow code style: no comments unless necessary
4. Update AGENTS.md if adding new APIs or changing behavior

---

**Sygil** — Physics-powered emergent gameplay