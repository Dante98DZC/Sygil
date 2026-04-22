using UnityEngine;
using PhysicsSystem.Core;
using PhysicsSystem.States;

namespace PhysicsSystem.Rules.Rules
{
    /// <summary>
    /// R12 — GasPressure (STANDARD)
    ///
    /// Convierte desviación de concentración de gas en presión diferencial.
    /// Con pressureFromGasCoeff = 0 esta regla está desactivada.
    ///
    ///   gasConcentration > 0  →  presión positiva  →  puede explotar (R05)
    ///   gasConcentration = 0  →  equilibrio, no genera presión
    ///
    /// La presión acumulada es aditiva. DecaySystem la disipa si no hay fuente activa.
    /// </summary>
    public class R12_GasPressure : IInteractionRule
    {
        public RuleID   Id       => RuleID.R12_GAS_PRESSURE;
        public TickType TickType => TickType.STANDARD;
        public int      Priority => 6;
        public MaterialLayer SourceLayer => MaterialLayer.Gas;

        private readonly float _pressureCoeff;
        private readonly float _atmConcentration;

        public R12_GasPressure(float pressureCoeff = 0f, float atmConcentration = 0f)
        {
            _pressureCoeff = pressureCoeff;
            _atmConcentration = atmConcentration;
        }

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def)
        {
            return !Mathf.Approximately(tile.gasConcentration, _atmConcentration);
        }

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            float delta = tile.gasConcentration - _atmConcentration;
            tile.gasConcentration = Mathf.Clamp(tile.gasConcentration + delta * _pressureCoeff, 0f, 100f);
        }
    }
}