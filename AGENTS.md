# Sygil — AGENTS.md

## Quick Start
- Open in Unity Editor to run tests (Window → General → Test Runner)
- No command-line test runner — uses Unity Test Framework

## Critical API (v4 — agents will guess wrong!)

**TileData v4 uses layers, NOT `.material`:**
```csharp
tile.groundMaterial   // terrain: STONE, EARTH, WOOD...
tile.liquidMaterial  // fluids: WATER, LAVA, MUD...
tile.gasMaterial    // atmosphere: STEAM, SMOKE...
tile.liquidVolume   // liters, not humidity
tile.gasDensity    // proxy for pressure (no pressure field)
```
- `.material` property is `[Obsolete]` — don't use it
- Rules R13-R16 access specific layers

**PhysicsGrid bug (TD-14b — pending fix):**
```csharp
// Current (broken):
GetMaterialDef(pos) → uses obsolete .material

// Expected (pending fix):
GetMaterialDef(pos, MaterialLayer.Ground) // etc
```
- R13 Melting needs Ground layer
- R14 Freezing needs Liquid layer
- R15 Boiling needs Liquid layer
- R16 Condensation needs Gas layer

## Architecture
- **Rules** → `IInteractionRule` in `RuleRegistry`
- **Diffusion** → separate strategies per property
- **4 tick types**: FAST(0.1s), STANDARD(0.3s), SLOW(0.5s), INTEGRITY(1.0s)
- **16 rules**: R01-R16 (R13-R16 are phase transitions)

## Test Files
- `RuleTests.cs` (EditMode) — use obsolete API, needs v4 update
- `TickBehaviorTests.cs` (PlayMode) — 10/10 pass but use obsolete API

## Pending Changes (from AgentContext.md)
1. Fix `PhysicsGrid.GetMaterialDef()` — add MaterialLayer param
2. Update tests to v4 API

## Reference
- Full context: `.vscode/LLMAgent/AgentContext.md` (v6.2)
- Rules: `Assets/PhysicsSystem/Rules/Rules/R_PhaseTransitions.cs`