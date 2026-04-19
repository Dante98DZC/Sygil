# Sygil — Agents.md

## Registro de cambios v6.1 → v6.2
- ✅ R13-R16 registradas en InitializeForTest (TD-10 resuelto)
- 🆕 Agregado MaterialLayer enum y cambio a PhysicsGrid.GetMaterialDef() (TD-14b)
- 🆕 Tests (RuleTests/TickBehaviorTests) como pendientes (TD-14)
- 🆕 MaterialDef assets 10 nuevos: "más adelante" (TD-13)
- 📋 Próximos pasos actualizados

## Contexto del proyecto

**Sygil** es un juego 2D top-down en Unity donde el mundo corre sobre un backend de simulación física por tiles. El jugador no usa poderes hardcodeados — en cambio, **programa alteraciones a la simulación misma** para crear efectos emergentes.

**Stack:** Unity 2D, C#, NUnit (Unity Test Framework), sin .asmdef — todo en `Assembly-CSharp` / `Assembly-CSharp-Editor`.

**Usa las mejores prácticas en C# para Unity y desarrollo de videojuegos. Genera algoritmos eficientes, con diseños claros y modulares.**

---

## Visión de gameplay

Los poderes del jugador son **modificadores de la simulación**, no efectos visuales predefinidos. Un poder puede:

- **Inyectar una `IInteractionRule` temporal** al `RuleRegistry` — ej: una regla que hace que el GAS se solidifique en radio 3 durante 5 ticks
- **Modificar coeficientes de `MaterialDefinition`** en un área — ej: hacer WOOD incombustible temporalmente, o triplicar el `heatTransferCoeff` de STONE
- **Suprimir o potenciar diffusers** en una zona — ej: congelar la difusión de temperatura en un radio, creando una burbuja térmica
- **Alterar propiedades de tiles directamente** vía `PlayerActionChannel` — ej: inyectar temperatura, electricidad o gasDensity en un punto

El resultado es un sistema donde los efectos son **emergentes**: interactúan con el estado actual del mundo de formas que ni el jugador ni el diseñador pueden predecir completamente. Prender fuego a un tile con agua encima produce vapor; ese vapor sube la gasDensity; si hay gas inflamable cerca, R10 lo ignita. El jugador manipula causas, no efectos.

Los poderes se **diseñan antes de la partida** en un editor visual de nodos. El editor compila el grafo y genera un `CompiledPower` (ScriptableObject). En runtime ese asset es read-only — el jugador no puede modificar la simulación arbitrariamente, solo activar los modificadores que diseñó.

---

## El backend — SimplifiedPhysicsSystem v4.0

Un motor de simulación de materia por tiles (grid 2D). Cada tile tiene **4 capas independientes** (Terrain, Liquid, Atmosphere, Entity) y una propiedad térmica compartida (`temperature`). La simulación corre en 4 frecuencias de tick independientes (`FAST=0.1s`, `STANDARD=0.3s`, `SLOW=0.5s`, `INTEGRITY=1.0s`) y procesa solo tiles activos para eficiencia.

Las interacciones están definidas como **reglas** (`IInteractionRule`) registradas en un `RuleRegistry`. Cada regla declara en qué tick corre, puede modificar el tile actual y sus vecinos, y es completamente testeable en aislamiento.

**Pipeline de cada tick:** frozen snapshot → reglas → difusión → estados derivados → notificación → decay → limpieza.

El sistema usa un **frozen snapshot de vecinos** para garantizar determinismo: cada tile ve el estado del tick anterior, no el estado en curso modificado por otras reglas del mismo tick.

**16 reglas activas:** R01–R11 (sin R12) + R13–R16 ✅ COMPLETAS.

Test suite: 78 EditMode + 10 PlayMode (tests usan API obsoleto — ver TD-14).

---

## Escala del mundo

```
1 tile = 1m × 1m de superficie
```

Un personaje humanoide ocupa exactamente 1 tile. La profundidad de líquido disponible escala con `TileHeight`.

---

## Modelo de mundo — Height-Based Tile System

El grid es un heightmap discreto. Cada tile tiene `TileHeight height`:

```
Tall   =  3  → columna, árbol alto, torre
Wall   =  2  → pared estándar
Low    =  1  → muro bajo, roca, arbusto
Ground =  0  → suelo base transitable
Shallow= -1  → charco, barro
Deep   = -2  → foso profundo, abismo
```

Paso y visión son propiedades de `MaterialDefinition`: `blocksMovement`, `blocksVision`, `slowsMovement`, `movementCost`.

**Invariante I9:** `height_is_geometry_not_physics` — TileHeight no entra en diffusion ni reglas físicas. Por ahora todo el mapa usa altura flat (Ground=0).

---

## TileData v4

```csharp
public struct TileData
{
    // ── TERRAIN ──────────────────────────────────────────────────────────────
    MaterialType groundMaterial;      // STONE, EARTH, WOOD, ICE, SAND, ASH…
    TileHeight   height;
    float        structuralIntegrity; // [0..100]
    float        electricEnergy;      // [0..100]

    // ── LIQUID ────────────────────────────────────────────────────────────────
    MaterialType liquidMaterial;      // WATER, LAVA, MUD, MOLTEN_METAL, MOLTEN_GLASS…
    float        liquidVolume;        // litros [0 .. LiquidCapacity]

    // ── ATMOSPHERE ────────────────────────────────────────────────────────────
    MaterialType gasMaterial;         // STEAM, SMOKE, CO2, ROCK_GAS…
    float        gasDensity;          // [0..100]. 50 = 1 atm (baseline)
                                      // presión diferencial = gasDensity - baseline

    // ── SHARED ────────────────────────────────────────────────────────────────
    float        temperature;         // [0..100] — atraviesa todas las capas

    // ── ENTITY ────────────────────────────────────────────────────────────────
    int          primaryEntityID;     // 0 = vacío. Exclusivo: Structure/Creature/Player
    int[]        secondaryEntityIDs;  // No exclusivos: Item, Effect. Puede ser null

    // ── GEOMETRY (derivada) ───────────────────────────────────────────────────
    StateFlags   derivedStates;

    // ── INTERNO ───────────────────────────────────────────────────────────────
    bool dirty, wasEmpty;

    // ── HELPERS ───────────────────────────────────────────────────────────────
    float LiquidCapacity => height switch
    {
        TileHeight.Deep    => 1000f,
        TileHeight.Shallow =>  500f,
        TileHeight.Ground  =>  200f,
        _                  =>    0f   // Low, Wall, Tall → sin líquido libre
    };

    // Compatibilidad v3 — [Obsolete], usar capas directamente
    [Obsolete] MaterialType material { get; set; }
}
```

**Eliminados en v4:** `pressure`, `humidity`, `fluidMaterial`  
**Renombrado:** `fluidMaterial` → `liquidMaterial`  
**Nuevos:** `liquidVolume`, `LiquidCapacity`, `primaryEntityID`, `secondaryEntityIDs`

### Modelo de presión
`pressure` ya no existe como campo. La presión diferencial se **deriva**: `gasDensity - gasBaseline (50)`. Todos los umbrales de explosión/implosión leen `gasDensity` directamente.

### Modelo de humedad
`humidity` eliminado. El concepto está representado por:
- `liquidVolume > 0` → líquido físico presente en el tile
- `gasMaterial == STEAM` + `gasDensity` → vapor de agua en atmósfera

---

## Capas y categorías de entidad

```
EntityCategory
├── Structure  → exclusivo (árbol, pared de madera, totem — bloquea movimiento)
├── Creature   → exclusivo (mobs, NPCs)
├── Player     → exclusivo
├── Item       → no exclusivo (N por tile)
└── Effect     → no exclusivo (trampa, runa, portal)
```

Dos entidades `exclusiveTile=true` nunca comparten tile. `primaryEntityID` almacena la exclusiva, `secondaryEntityIDs[]` las no exclusivas.

**WOOD como material vs entidad:**
- `groundMaterial = WOOD` → suelo o pared de madera. Las reglas físicas (R01) lo queman directamente.
- Árbol = `SimulationEntity` con `physicsProfile = MatDef_Wood`. Tiene HP, puede caer. Reacciona a temperatura alta en su tile — las reglas físicas no lo queman directamente.

---

## MaterialType — enumeración completa

```csharp
public enum MaterialType
{
    EMPTY=0, WOOD=1, METAL=2, STONE=3, WATER=4,
    GAS=5,   // deprecated — mantener por compatibilidad
    EARTH=6, GLASS=7,
    ICE=8, ASH=9, SAND=10,
    LAVA=11, MOLTEN_METAL=12, MOLTEN_GLASS=13, MUD=14,
    STEAM=15, SMOKE=16, CO2=17, ROCK_GAS=18,
}
```

## MaterialLayer — enum para acceder a capas específicas

```csharp
public enum MaterialLayer
{
    Primary,   // .material obsoleto (compatibilidad v3)
    Ground,    // groundMaterial
    Liquid,    // liquidMaterial
    Gas        // gasMaterial
}
```

**Cambio en PhysicsGrid (TD-14b):**
```csharp
public MaterialDefinition GetMaterialDef(Vector2Int pos, MaterialLayer layer = MaterialLayer.Primary)
{
    var mat = layer switch
    {
        MaterialLayer.Ground => _grid[pos.x, pos.y].groundMaterial,
        MaterialLayer.Liquid => _grid[pos.x, pos.y].liquidMaterial,
        MaterialLayer.Gas    => _grid[pos.x, pos.y].gasMaterial,
        _                    => _grid[pos.x, pos.y].material, // Primary: obsoleto
    };
    return _library.Get(mat);
}
```

Reglas que deben pasar su capa al llamar `GetMaterialDef`:
- **R13 Melting** → `MaterialLayer.Ground`
- **R14 Freezing** → `MaterialLayer.Liquid`
- **R15 Boiling** → `MaterialLayer.Liquid`
- **R16 Condensation** → `MaterialLayer.Gas`

---

## MaterialDefinition v3

Campos de calentamiento: `meltingPoint`, `liquidForm`, `boilingPoint`, `gasForm`  
Campos de enfriamiento: `condensationPoint`, `condensedForm`, `freezingPoint`, `solidForm`  
Campos de combustión: `ignitionTemperature`, `burnInto`, `smokeForm`  
Otros: `collapseInto`, `integrityBase`, coeficientes de difusión, navegación  
Obsoletos: `hasMeltingPoint`, `meltingTemperature`, `meltInto`

### Valores por material

| Material | meltingPoint | liquidForm | boilingPoint | gasForm | freezingPoint | solidForm | condensationPoint | condensedForm | ignitionTemperature | burnInto | smokeForm |
|---|---|---|---|---|---|---|---|---|---|---|---|
| WOOD | — | — | — | — | — | — | — | — | 70 | ASH | SMOKE |
| METAL | 90 | MOLTEN_METAL | — | — | — | — | — | — | 0 | — | — |
| STONE | 95 | LAVA | — | — | — | — | — | — | 0 | — | — |
| WATER | — | — | 80 | STEAM | 10 | ICE | — | — | 0 | — | — |
| GLASS | 85 | MOLTEN_GLASS | — | — | — | — | — | — | 0 | — | — |
| SAND | 85 | MOLTEN_GLASS | — | — | — | — | — | — | 0 | — | — |
| ICE | 10 | WATER | — | — | — | — | — | — | 0 | — | — |
| LAVA | — | — | — | ROCK_GAS | 20 | STONE | — | — | 0 | — | — |
| MOLTEN_METAL | — | — | — | — | 60 | METAL | — | — | 0 | — | — |
| MOLTEN_GLASS | — | — | — | — | 55 | GLASS | — | — | 0 | — | — |
| STEAM | — | — | — | — | — | — | 80 | WATER | 0 | — | — |
| SMOKE | — | — | — | — | — | — | 0 | — | 60 | — | — |

---

## Reglas — estado completo v4

### R01 Combustion (STANDARD) ✅ v4
`ignitionTemperature > 0 && temperature > ignitionTemperature && flammabilityCoeff > 0.5`.  
Escribe: `temperature +5f`, `gasDensity +3f`, `structuralIntegrity -2f`.  
Eliminados en v4: `humidity` y `pressure`.

### R02 Evaporation (INTEGRITY) ✅ v4 — TD-11 candidata a eliminar
Fallback para WATER sin `boilingPoint` configurado. `liquidMaterial == WATER && temperature > 80`.  
Produce STEAM en `gasMaterial`, presuriza vecinos via `gasDensity`.

### R03 ElectricPropagation / R04 ElectricWater — sin cambios en v4

### R05 PressureExplosion (INTEGRITY) ✅ v4
`gasDensity > 80` → explosión. `gasDensity < 20` → implosión en materiales frágiles.  
Lee/escribe `gasDensity` en vez de `pressure`.

### R06 PressureRelease (INTEGRITY) ✅ v4
`gasDensity > 80` + vecino débil → libera presión hacia el vecino más débil.  
Lee/escribe `gasDensity` en vez de `pressure`.

### R07 StructuralCollapse (INTEGRITY) — sin cambios en v4

### R08 SlowEvaporation (STANDARD) ✅ v4 — renombrada de HumidityVaporization
`liquidVolume >= 10 && temperature > 50` → evapora 5L/tick, `gasDensity +2`, produce STEAM.  
Reemplaza la lógica de `humidity` eliminada.

### R09 HeatSuppression (STANDARD) ✅ v4
`temperature > 70 && liquidVolume > 50` → enfría proporcional al volumen, consume 10L/tick.

### R10 GasIgnition (STANDARD) ✅ v4
`gasDensity > 60 && temperature > 60` → libera calor, neto `gasDensity -5`.  
Eliminado: escritura en `pressure`.

### R11 GasProduction (STANDARD) ✅ v4
ON_FIRE o `groundMaterial == GAS` → `gasDensity +rate`.  
Usa `groundMaterial` en vez de la propiedad calculada `material`.

### R12 GasPressure — **ELIMINADA en v4**
Redundante: `gasDensity` es directamente el proxy de presión.

### R13 Melting (INTEGRITY) ✅ v4
`groundMaterial` sólido con `temperature >= meltingPoint && LiquidCapacity > 0`.  
→ `liquidMaterial = liquidForm`, `liquidVolume = LiquidCapacity * 0.5f`, `groundMaterial = EMPTY`.  
Consume 5 de temperatura.

### R14 Freezing (INTEGRITY) ✅ v4
`liquidMaterial` líquido con `temperature <= freezingPoint && liquidVolume > 0`.  
→ `groundMaterial = solidForm`, `liquidMaterial = EMPTY`, `liquidVolume = 0`.  
Libera 3 de temperatura.

### R15 Boiling (INTEGRITY) ✅ v4
`liquidMaterial` líquido con `temperature >= boilingPoint && liquidVolume > 0`.  
→ `gasMaterial = gasForm`, `liquidMaterial = EMPTY`, `liquidVolume = 0`, `gasDensity +15`.  
Si ya hay gas: solo sube `gasDensity`. Consume 8 de temperatura. Presuriza vecinos via `gasDensity`.  
Absorbe el caso WATER→STEAM de R02.

### R16 Condensation (INTEGRITY) ✅ v4
`gasMaterial` gas con `temperature <= condensationPoint && LiquidCapacity > 0`.  
→ `liquidMaterial = condensedForm`, `liquidVolume = min(20, capacity)`, `gasMaterial = EMPTY`, `gasDensity -10`.  
Si ya hay líquido: suma volumen. Libera 2 de temperatura.

---

## SimulationConfig

```csharp
float propertyCap          = 100f;
float gasBaseline          = 50f;
float gasProductionRate    = 5f;
float decayTemperature     = 2.0f;
float decayGasDensity      = 1.0f;
float deactivationTolerance = 2.0f;
float maxRulesPerTile      = 3;
// Obsoletos en v4 (sin efecto — limpiar en TD-15):
float pressureFromGasCoeff, decayPressure, decayHumidity;
```

---

## DecaySystem v4

Decae por SLOW tick: `temperature`, `gasDensity` (hacia `gasBaseline`), `electricEnergy`.  
`IsStable` chequea: `temperature`, `electricEnergy`, `gasDensity` (vs baseline), `liquidVolume`, `structuralIntegrity`.  
Obtiene `def` de `groundMaterial`. Detecta tile vacío comparando las tres capas explícitamente.  
**Eliminado:** decay de `pressure` y `humidity`.

---

## EngineNotifier v4

### TilePropertySnapshot
```csharp
readonly struct TilePropertySnapshot
{
    float Temperature;
    float ElectricEnergy;
    float GasDensity;
    float StructuralIntegrity;
    float LiquidVolume;        // nuevo en v4
    // Pressure, Humidity — ELIMINADOS
}
```
`DominantIntensity` normaliza `LiquidVolume` contra 1000 (Deep capacity máxima).  
`OnMaterialChanged` emite capa dominante (`tile.material` obsoleto) para compat con SimulationRenderer. TODO Render-2a: migrar a capas explícitas.

---

## Sistema de poderes — RUNTIME COMPLETO, editor pendiente

```
EDITOR (futuro)                 RUNTIME (implementado)
───────────────────────────────────────────────────────
PowerGraph (nodos + edges)
       ↓
  PowerCompiler
       ↓
  CompiledPower  ──────────────→  PowerCaster
  (ScriptableObject)                  ↓
                          SimulationModifierRegistry
                                      ↓
                             CompiledPowerModifier
                                      ↓
                                 PhysicsGrid
```

`CompiledPower` es read-only en runtime (I10). El jugador diseña poderes antes de la partida; en runtime solo los activa.

---

## Sistema de render

### Render-1 — SimulationRenderer
Suscrito a `OnMaterialChanged`. Dibuja grid en `Start()` con 1 frame delay. Runtime: solo redibuja tiles con cambio de material. Tiles EMPTY omitidos.  
Usa capa dominante (`tile.material` obsoleto) — migrar en Render-2a.

### Render-2b — PropertyOverlayRenderer + OverlayColorizer

| Tecla | Modo | Fuente en v4 |
|---|---|---|
| 0 | Off | — |
| 1 | Temperatura | `temperature` |
| 2 | Presión (gas) | `gasDensity` |
| 3 | Líquido | `liquidVolume` |
| 4 | Electricidad | `electricEnergy` |
| 5 | Gas | `gasDensity` |
| 6 | Daño estructural | `structuralIntegrity` invertida |
| 7 | Estados derivados | `derivedStates` |
| 8 | Actividad | ActiveTiles (debug) |
| 9 | Combinado | mezcla aditiva |

**Pendiente:** actualizar `PropertyOverlayRenderer` y `OverlayColorizer` para modos 2 y 3 (TD-17).

### Render-2a (pendiente)
Sprite base por material + tint/scale por `TileHeight` + activar `LiquidDiffusion`.

---

## Sistema de debug

`SimulationDebugHUD` — P pausa/reanuda, F/S/L/I step por tipo de tick.  
`TileInspector` — propiedades exactas del tile bajo cursor. Click fija/desfija.  
`RuleEventLog` — últimos N disparos de reglas. C limpia. `_pauseOnIgnition` pausa en R01/R10.  
`TilemapDebugger2` — debug temporal, candidata a eliminar.

---

## Sistema de cámara

`CameraController` adjunto a Main Camera (Orthographic).  
Click medio / click derecho + arrastrar → pan. Rueda → zoom centrado en cursor.

---

## Arquitectura de entidades — DISEÑADA, pendiente implementar

`primaryEntityID : int` (0 = vacío). `secondaryEntityIDs : int[]` (null = sin secundarias).  
`EntityRegistry` → `Dictionary<int, SimulationEntity>`.  
`EntityCategory`: Structure, Creature, Player (exclusivos) | Item, Effect (no exclusivos).  
Cada entidad tiene `physicsProfile : MaterialDefinition`, `OnSimulationUpdate(TileData)`, `Tick()`.

---

## SimulationEngine.InitializeForTest() ✅ COMPLETO

**Registradas (R01–R16):**
```csharp
// Combustion & electricity
_ruleRegistry.AddRule(new Rules.Rules.R01_Combustion());
_ruleRegistry.AddRule(new Rules.Rules.R03_ElectricPropagation());
_ruleRegistry.AddRule(new Rules.Rules.R04_ElectricWater());
_ruleRegistry.AddRule(new Rules.Rules.R09_HeatSuppression());
_ruleRegistry.AddRule(new Rules.Rules.R10_GasIgnition());

// Pressure & structure
_ruleRegistry.AddRule(new Rules.Rules.R05_PressureExplosion());
_ruleRegistry.AddRule(new Rules.Rules.R06_PressureRelease());
_ruleRegistry.AddRule(new Rules.Rules.R07_StructuralCollapse());
_ruleRegistry.AddRule(new Rules.Rules.R11_GasProduction(...));
_ruleRegistry.AddRule(new Rules.Rules.R12_GasPressure(...));

// Humidity
_ruleRegistry.AddRule(new Rules.Rules.R08_HumidityVaporization());

// Phase transitions: solid ↔ liquid ↔ gas
_ruleRegistry.AddRule(new Rules.Rules.R02_Evaporation());
_ruleRegistry.AddRule(new Rules.Rules.R13_Melting());
_ruleRegistry.AddRule(new Rules.Rules.R14_Freezing());
_ruleRegistry.AddRule(new Rules.Rules.R15_Boiling());
_ruleRegistry.AddRule(new Rules.Rules.R16_Condensation());
```

**R12_GasPressure** NO eliminada — sigue registrada (usa gasDensity directamente).
**R13-R16 ✅ COMPLETAS** — registradas y funcionales.

---

## Estructura de archivos completa

```
Assets/PhysicsSystem/
  Core/
    TileData.cs              ✅ v4
    TileHeight.cs            ✅
    DecaySystem.cs           ✅ v4
    MaterialDefinition.cs    ✅ v3
    HeightmapGenerator.cs    ✅
    PhysicsGrid.cs           ✅
    SimulationEngine.cs      ✅ (R13-R16 PENDIENTE registrar)
  Rules/
    IInteractionRule.cs      ✅
    RuleRegistry.cs          ✅
    Rules/
      R01_Combustion.cs      ✅ v4
      R02_Evaporation.cs     ✅ v4 — TD-11
      R03, R04, R07          ✅
      R05_PressureExplosion  ✅ v4
      R06_PressureRelease    ✅ v4
      R08_SlowEvaporation    ✅ v4 (renombrada)
      R09_HeatSuppression    ✅ v4
      R10_GasIgnition        ✅ v4
      R11_GasProduction      ✅ v4
      R12_GasPressure        ❌ ELIMINADA
      R_PhaseTransitions.cs  ✅ v4 R13–R16 — sin registrar aún
  Diffusion/
    IDiffusionStrategy.cs    ✅
    GradientDiffusion.cs     ✅
    GravityDiffusion.cs      ✅
    PressureDiffusion.cs     ⚠ pendiente revisar (difundía pressure eliminado)
    LiquidDiffusion.cs       ⬜ PENDIENTE (flujo por heightmap — Render-2a)
  States/
    StateFlags.cs            ✅
    DerivedStateComputer.cs  ✅
  Player/
    PlayerActionChannel.cs   ✅
    PlayerActions.cs         ✅
    CameraController.cs      ✅
  Bridge/
    TileUpdateEvent.cs       ✅
    EngineNotifier.cs        ✅ v4
  Config/
    SimulationConfig.cs      ✅ (campos obsoletos pendiente limpiar — TD-15)
    MaterialLibrary.cs       ✅
  Renderer/
    TileVisualDefinition.cs  ✅
    TileVisualLibrary.cs     ✅
    SimulationRenderer.cs    ✅ (migrar a capas explícitas — Render-2a)
    Overlay/
      OverlayMode.cs         ✅
      OverlayColorizer.cs    ⚠ pendiente v4 (TD-17)
      PropertyOverlayRenderer.cs ⚠ pendiente v4 (TD-17)
  Powers/
    ISimulationModifier.cs          ✅
    SimulationModifierRegistry.cs   ✅
    CompiledPower.cs                ✅
    CompiledPowerModifier.cs        ✅
    PowerChannel.cs                 ✅
    PowerCaster.cs                  ✅
  Debug/
    SimulationDebugHUD.cs    ✅
    TileInspector.cs         ✅
    RuleEventLog.cs          ✅
    TilemapDebugger2.cs      ⚠ candidata a eliminar
  Tests/
    PhysicsSystemTest.cs     ✅ (legacy)
    TestWorldGenerator.cs    ✅
    Editor/
      RuleTests.cs           ⚠ pendiente actualizar para v4
    PlayMode/
      TickBehaviorTests.cs   ✅ (10/10 — pendiente verificar tras v4)
```

---

## Namespaces

```
PhysicsSystem.Core / .Rules / .Rules.Rules / .Diffusion
.States / .Player / .Bridge / .Config / .Powers / .Tests
.Renderer / .Renderer.Overlay / .DebugTools
```

---

## Invariantes

```
I1:  simulation_never_reads_derivedStates
I2:  rules_write_properties_only (R13-R16 escriben material layers — excepción documentada)
I3:  player_actions_write_properties_only
I4:  all_property_writes_clamped [0..100] (liquidVolume: [0..LiquidCapacity])
I5:  engine_systems_never_write_to_TileData
I6:  no_MonoBehaviour_in_Core_or_Rules
I7:  RuleRegistry_is_the_only_rule_caller
I8:  decay_runs_after_diffusion_always
I9:  height_is_geometry_not_physics
I10: CompiledPower_is_readonly_at_runtime
I11: OverlayColorizer_is_stateless
I12: new_rules_write_layer_slots_directly (groundMaterial/liquidMaterial/gasMaterial)
I13: pressure_is_derived — leer gasDensity directamente, no existe como campo
I14: humidity_is_derived — usar liquidVolume y gasDensity(STEAM), no existe como campo
```

---

## Deuda técnica

```
TD-01: ClampTile centralizado en PhysicsGrid — prioridad media
TD-02: clamp_all en vecinos de R02,R03,R05,R06,R07 — prioridad alta
TD-03: condición formal electricEnergy source en R03 — prioridad baja
TD-04: frozen snapshot Dictionary → TileData[,] pre-allocado al escalar — prioridad baja
TD-05: logs de debug temporales — limpiar antes de build
TD-06: R07 trigger de fusión redundante con R13 — simplificar a solo colapso por integridad
TD-07: LiquidDiffusion — flujo de liquidVolume hacia TileHeight menor (Render-2a)
TD-08: PropertyDeltaModifier.cs obsoleto — eliminar
TD-09: RuleTests — MakeDef de WOOD necesita ignitionTemperature=70f
TD-10: ✅ RESUELTO — R13-R16 ya registradas en InitializeForTest()
TD-11: R02 Evaporation — eliminar cuando todos los MatDef tengan boilingPoint
TD-12: TilemapDebugger2.cs — eliminar
TD-13: MaterialLibrary — crear 10 MatDef nuevos en Unity (ICE, ASH, SAND, LAVA, MOLTEN_METAL, MOLTEN_GLASS, MUD, STEAM, SMOKE, CO2) — "más adelante"
TD-14: RuleTests.cs + TickBehaviorTests.cs — usan API obsoleto .material en lugar de capas ⬅ PENDIENTE
TD-14b: PhysicsGrid.GetMaterialDef() — usa API obsoleto .material, debe usar parámetro layer ⬅ PENDIENTE
TD-15: SimulationConfig — limpiar pressureFromGasCoeff, decayPressure, decayHumidity
TD-16: PressureDiffusion.cs — revisar/eliminar (difundía pressure eliminado)
TD-17: PropertyOverlayRenderer + OverlayColorizer — modos 2→gasDensity, 3→liquidVolume
TD-18: SimulationRenderer — migrar OnMaterialChanged a capas explícitas (Render-2a)
```

---

## Próximos pasos en orden

```
1–8.  ✅ (ver historial — hasta migración completa a v4)
9.  ✅ Registrar R13-R16 COMPLETO
10. Corregir PhysicsGrid.GetMaterialDef() — agregar parámetro MaterialLayer ⬅ PENDIENTE
11. Actualizar tests (RuleTests.cs, TickBehaviorTests.cs) al API v4        ⬅ PENDIENTE
12. Crear MaterialDef assets nuevos en Unity (10 tipos)          — "más adelante"
13. Render-2a — sprite por material + tint/scale + LiquidDiffusion
14. Entidades — EntityRegistry, SimulationEntity, EntityCategory
15. Árbol de prueba — entidad que reacciona a temperatura del tile
16. Editor poderes — PowerGraph, PowerCompiler, nodos visuales
17. Tests capa 3 — integración con cadenas de reacción reales
```

---

## Tensiones de diseño resueltas

```
T1:  altura flat por ahora — heightmap en datos sin renderizar
T2:  entidades móviles — diseño después de entidades estáticas
T3:  poderes = modificadores de simulación, no inyección arbitraria — V1 confirmado
T4:  Render-2a pendiente — sprite base + modificador visual por altura
T5:  WOOD es MaterialType; árbol = entidad con physicsProfile=MatDef_Wood
T6:  transiciones de material — resueltas con R13-R16
T7:  capas de material — resueltas en v4 (Terrain/Liquid/Atmosphere/Entity)
T8:  pressure como campo — eliminado; derivado de gasDensity
T9:  humidity como campo — eliminado; representado por liquidVolume + gasMaterial(STEAM)
T10: escala del tile — 1 tile = 1m²; liquidVolume en litros; LiquidCapacity por TileHeight
T11: entidades exclusivas vs compartidas — primaryEntityID + secondaryEntityIDs[]
```