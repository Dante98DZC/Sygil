// Assets/PhysicsSystem/Rules/Rules/R11_GasProduction.cs
using UnityEngine;
using PhysicsSystem.Core;
using PhysicsSystem.States;

namespace PhysicsSystem.Rules.Rules
{
    /// <summary>
    /// R11 — GasProduction (STANDARD)
    ///
    /// Los procesos térmicos producen gas sobre el tile:
    ///   - Combustión activa (ON_FIRE): humo y gases de combustión
    ///   - Material GAS en groundMaterial: emisión continua desde la fuente
    ///
    /// El gas producido empuja gasDensity por encima del baseline (50),
    /// lo que convierte en presión diferencial positiva (gasDensity > 50).
    ///
    /// v4: usa tile.groundMaterial en lugar de tile.material (propiedad calculada
    /// marcada obsoleta). GAS deprecated sigue en groundMaterial por compatibilidad.
    /// </summary>
    public class R11_GasProduction : IInteractionRule
    {
        public RuleID   Id       => RuleID.R11_GAS_PRODUCTION;
        public TickType TickType => TickType.STANDARD;
        public int      Priority => 5;

        private readonly float _productionRate;
        private readonly float _gasCap;

        /// <param name="productionRate">Gas producido por tick cuando ON_FIRE (SimulationConfig.gasProductionRate)</param>
        /// <param name="gasCap">Límite superior de gasDensity (normalmente 100f)</param>
        public R11_GasProduction(float productionRate = 5f, float gasCap = 100f)
        {
            _productionRate = productionRate;
            _gasCap         = gasCap;
        }

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def)
        {
            bool onFire = (tile.derivedStates & StateFlags.ON_FIRE) != 0;
            bool isGas  = tile.groundMaterial == MaterialType.GAS;
            return onFire || isGas;
        }

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            bool onFire = (tile.derivedStates & StateFlags.ON_FIRE) != 0;

            // Combustión activa produce más gas que una fuente pasiva
            float rate = onFire ? _productionRate : _productionRate * 0.5f;

            tile.gasDensity = Mathf.Clamp(tile.gasDensity + rate, 0f, _gasCap);
        }
    }
}