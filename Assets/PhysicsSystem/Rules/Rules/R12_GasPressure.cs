using UnityEngine;
using PhysicsSystem.Core;
using PhysicsSystem.States;

namespace PhysicsSystem.Rules.Rules
{
    /// <summary>
    /// R12 — GasPressure (STANDARD)
    ///
    /// Convierte el exceso o déficit de gas respecto al baseline (50 = 1 atm)
    /// en presión diferencial sobre el tile:
    ///
    ///   gasDensity > 50  →  presión positiva  →  puede explotar (R05)
    ///   gasDensity < 50  →  presión negativa  →  puede implosionar (R05)
    ///   gasDensity = 50  →  equilibrio, no genera presión
    ///
    /// La presión acumulada es aditiva — R12 no reemplaza la presión existente,
    /// la incrementa. DecaySystem la devuelve a 0 si no hay fuente activa.
    /// </summary>
    public class R12_GasPressure : IInteractionRule
    {
        public RuleID   Id       => RuleID.R12_GAS_PRESSURE;
        public TickType TickType => TickType.STANDARD;
        public int      Priority => 6;

        private readonly float _pressureCoeff;
        private readonly float _gasBaseline;

        /// <param name="pressureCoeff">Cuánta presión genera cada unidad de exceso de gas (SimulationConfig.pressureFromGasCoeff)</param>
        /// <param name="gasBaseline">Baseline atmosférico (SimulationConfig.gasBaseline)</param>
        public R12_GasPressure(float pressureCoeff = 0.3f, float gasBaseline = 50f)
        {
            _pressureCoeff = pressureCoeff;
            _gasBaseline   = gasBaseline;
        }

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def)
        {
            // Solo actúa cuando el gas se desvía del baseline
            return !Mathf.Approximately(tile.gasDensity, _gasBaseline);
        }

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            float delta = tile.gasDensity - _gasBaseline; // positivo o negativo
            tile.pressure = Mathf.Clamp(tile.pressure + delta * _pressureCoeff, 0f, 100f);
        }
    }
}