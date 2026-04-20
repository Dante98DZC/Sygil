using UnityEngine;
using PhysicsSystem.Core;

namespace PhysicsSystem.Config
{
    [CreateAssetMenu(menuName = "PhysicsSystem/SimulationConfig")]
    public class SimulationConfig : ScriptableObject
    {
        [Header("Tick Rates")]
        public float tickFast      = 0.1f;
        public float tickStandard  = 0.3f;
        public float tickSlow      = 0.5f;
        public float tickIntegrity = 1.0f;

        [Header("Limits")]
        public int   maxRulesPerTile = 5;
        public float propertyCap     = 100f;
        public float propertyFloor   = 0f;
        public float minTemperature = 0f;
        public float maxTemperature = 100f;

        [Header("Decay Rates")]
        public float decayTemperature = 2.0f;
        public float decayPressure    = 3.0f;
        public float decayHumidity    = 0.5f;
        public float decayGasDensity  = 1.0f;
        // electricEnergy: no rate — zeroed behaviourally if no source this tick (DecaySystem)
        // structuralIntegrity: rate = 0, no self-repair (spec invariant)

        [Header("Deactivation")]
        [Tooltip("Max distance from baseline for a property to be considered stable")]
        public float deactivationTolerance = 2.0f;

        [Header("Gas & Atmosphere")]
        [Tooltip("DEPRECATED: Use atmosphereDensity instead. Kept for compatibility.")]
        public float gasBaseline           = 50f;

        [Tooltip("How much excess gas (above baseline) converts to pressure per tick.")]
        public float pressureFromGasCoeff  = 0.3f;

        [Tooltip("Gas produced per tick by an actively burning tile (ON_FIRE).")]
        public float gasProductionRate     = 5f;

        [Tooltip("Pressure below which implosion collapses fragile materials.")]
        public float implosionThreshold    = 20f;

        [Tooltip("Structural damage dealt to implosion-vulnerable tiles.")]
        public float implosionDamage       = 40f;

        [Header("Atmosphere")]
        [Tooltip("Gas type that fills the atmosphere (e.g., AIR).")]
        public MaterialType atmosphereGas = MaterialType.AIR;

        [Tooltip("Baseline atmospheric gas density (1 atm equivalent).")]
        public float atmosphereDensity    = 50f;

        [Tooltip("Atmospheric temperature in Celsius.")]
        public float atmosphereTemperature = 23f;

        [Tooltip("Diffusion rate between tiles and atmosphere (0-1).")]
        public float atmosphereDiffusionRate = 0.25f;
    }
}