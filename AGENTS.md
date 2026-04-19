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
- `isAtmosphereOpen` flag for gas dissipation

## Architecture
- **Rules** → `IInteractionRule` in `RuleRegistry`
- **Diffusion** → separate strategies per property
- **4 tick types**: FAST(0.1s), STANDARD(0.3s), SLOW(0.5s), INTEGRITY(1.0s)
- **16 rules**: R01-R16 (R13-R16 are phase transitions)

## Test Files
- `RuleTests.cs` (EditMode) — use obsolete API, needs v4 update
- `TickBehaviorTests.cs` (PlayMode) — 10/10 pass but use obsolete API

## Implemented Changes (v2 — COMPLETED)
1. ✓ `PhysicsGrid.GetMaterialDef(pos, MaterialLayer)` — FIXED
2. ✓ Energy conservation in phase transitions (R13-R16) — IMPLEMENTED
3. ✓ Latent heat fields in MaterialDefinition — IMPLEMENTED
4. ✓ Gas dissipation in open atmosphere — IMPLEMENTED (via GravityDiffusion)
5. Pending: Update tests to v4 API

## Energy Conservation System
- **Fusión (R13):** Consumes `latentHeatOfFusion` energy → cools tile
- **Freezing (R14):** Releases `latentHeatOfFusion` energy → heats tile
- **Boiling (R15):** Consumes `latentHeatOfVaporization` energy → cools tile
- **Condensation (R16):** Releases `latentHeatOfVaporization` energy → heats tile
- **Gas dissipation:** In open atmosphere (`isAtmosphereOpen=true`), gas with density < 5 is lost

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
- `GravityDiffusion.ApplyDissipation()` — elimina gas bajo en atmósfera abierta

See `AGENTS_ActionPlan.md` for detailed implementation plan.

## Reference
- Full context: `AGENTS_Context.md` (v6.2)
- Action plan: `AGENTS_ActionPlan.md`
- Unity Editor: `AGENTS_UnityEditor.md`
- Rules: `Assets/PhysicsSystem/Rules/Rules/R_PhaseTransitions.cs`