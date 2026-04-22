# Sygil — AGENTS.md

## Quick Start
- Open in Unity Editor to run tests (Window → General → Test Runner)
- No command-line test runner — uses Unity Test Framework
- **73 EditMode tests** — all passing

## Critical API (v4 — agents will guess wrong!)

**TileData v4 uses layers, NOT `.material`:**
```csharp
tile.groundMaterial   // terrain: STONE, EARTH, WOOD...
tile.liquidMaterial  // fluids: WATER, LAVA, MUD...
tile.gasMaterial    // atmosphere: STEAM, SMOKE...
tile.liquidVolume   // liters, not humidity
tile.gasDensity    // proxy for pressure (no pressure field)
tile.soilMoisture   // absorbed water in porous soil
tile.isAtmosphereOpen // gas can dissipate
tile.derivedStates  // StateFlags: ON_FIRE, ELECTRIFIED, etc.
```
- `.material` property is `[Obsolete]` — don't use it
- Rules access specific layers via `rule.SourceLayer`
- `isAtmosphereOpen` flag for gas dissipation
- `soilMoisture` stores absorbed water in porous materials
- `derivedStates` contains `StateFlags.ON_FIRE` for visual feedback

## Architecture
- **Rules** → `IInteractionRule` in `RuleRegistry`
- **Diffusion** → separate strategies per property
- **4 tick types**: FAST(0.1s), STANDARD(0.3s), SLOW(0.5s), INTEGRITY(1.0s)
- **17 rules**: R01-R17 (R13-R16 = phase transitions, R17 = filtration)

## Test Files
- `RuleTests.cs` (EditMode) — 73 tests passing ✅

## Atmosphere-Based Diffusion System (v6.3)

**Problem solved:** Hardcoded 50 baseline caused instability. Now atmosphere is a real system.

### Configuration (SimulationConfig)
```csharp
atmosphereGas = MaterialType.AIR          // gas type that fills atmosphere
atmosphereDensity = 50f                   // baseline density (1 atm)
atmosphereTemperature = 23f              // baseline temperature (°C)
atmosphereDiffusionRate = 0.25f          // exchange rate per tick
```

### How It Works

**Gas Diffusion (GravityDiffusion):**
- Tiles with `isAtmosphereOpen = true` diffuse with atmosphere
- Exchange formula: `exchange = (gasDensity - atmosphereDensity) * atmosphereDiffusionRate`
- Gas naturally tends toward atmosphereDensity (50) without hardcoded decay

**Temperature Diffusion (GradientDiffusion):**
- Tiles open to atmosphere diffuse temperature toward atmosphereTemperature (23°C)
- Exchange formula: `exchange = (temperature - atmosphereTemperature) * atmosphereDiffusionRate`

### RebuildAtmosphereFlags()
- Must be called after generating world (`TestWorldGenerator.GenerateMap()`)
- Sets `isAtmosphereOpen = true` for tiles above any solid in their column
- Gas/temperature diffuse naturally with atmosphere on open tiles

### DecaySystem Changes
- Tiles **NOT** open to atmosphere: decay toward atmosphereDensity
- Tiles **open** to atmosphere: no decay (diffusion handles it)
- Stability check uses atmosphereTemperature for temperature

## Liquid Flow Stop Conditions
1. **Volume too low** (`sourceVal <= 0.5f`) — skip diffusion
2. **Neighbor at capacity** (`neighborVal >= neighborCapacity - 0.1f`) — no transfer
3. **Height-based bias** — fluid flows toward lower TileHeight

## Render System (6-Layer Tilemap)
- **BackgroundMap** (Order -3): dark background — fills empty space
- **GroundTilemap** (Order -2): base layer — always visible
- **LiquidTilemap** (Order -1): transparent overlay — opacity scales with liquidVolume
- **GasTilemap** (Order 0): transparent overlay — opacity scales with gasDensity
- **OverlayMap** (Order 1): diagnostic overlay — temperature/gas visualization separate from real data
- **AmbientMap** (Order 2): ambient temperature tint — always visible at 15% opacity

### Dirty Flag System
- `SimulationRenderer` uses `dirtySet` (HashSet<Vector2Int>) to batch cell updates
- `LateUpdate()` processes dirty cells once per frame, not immediately on change
- Maximum 1 `DrawCell()` per unique cell per frame

### Timer Configuration
- CollectStats: **1.0s** (was 0.1s) — reduces iteration by 10x
- Ambient tint: **0.5s** — subtle visual, doesn't need frequent updates
- Overlay: on-demand toggle

Setup in Unity:
1. Create 6 Tilemap GameObjects under Sygil
2. Set TilemapRenderer sorting order: Background=-3, Ground=-2, Liquid=-1, Gas=0, Overlay=1, Ambient=2
3. Create WhiteTile.asset (1x1 white sprite)
4. Assign all tilemaps + whiteTile to SimulationRenderer Inspector

## Fire Visual Response
- R01_Combustion sets `StateFlags.ON_FIRE` in `derivedStates`
- SimulationRenderer draws fire overlay with `_fireColor` and `_fireOpacityScale`
- Fire opacity scales with temperature (hotter = more visible)

## Water Model (Option A - Porous Ground)
When water is placed on land:
```
groundMaterial = EARTH      // porous soil that absorbs water
liquidMaterial = WATER    // surface water
liquidVolume = 100L      // accumulation
soilMoisture = 20L       // absorbed water
```

## Energy Conservation System
- **Fusión (R13):** Consumes `latentHeatOfFusion` energy → cools tile
- **Freezing (R14):** Releases `latentHeatOfFusion` energy → heats tile
- **Boiling (R15):** Consumes `latentHeatOfVaporization` energy → cools tile
- **Condensation (R16):** Releases `latentHeatOfVaporization` energy → heats tile
- **Gas dissipation:** In open atmosphere (`isAtmosphereOpen=true`), gas diffuses naturally

## Material Latent Heat Values
| Material | latentHeatOfFusion | latentHeatOfVaporization |
|----------|--------------------|-------------------------|
| ICE      | 80                 | —                        |
| WATER    | 80                 | 100                      |
| STONE    | 150                | 200                      |
| METAL    | 120                | 180                      |
| GLASS    | 100                | 150                      |

## Key Methods
- `PhysicsGrid.RebuildAtmosphereFlags()` — recalcula `isAtmosphereOpen`
- `GravityDiffusion.Diffuse()` — gas diffusion + atmosphere exchange
- `GradientDiffusion.Diffuse()` — temperature diffusion + atmosphere exchange
- `DecaySystem.IsStable()` — checks atmosphere-based equilibrium

See `AGENTS_Context.md` for detailed implementation plan.
