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
        public float minTemperature  = 0f;
        public float maxTemperature  = 100f;

        [Header("Decay Rates")]
        public float decayTemperature    = 2.0f;
        public float decayGasConcentration = 1.0f;
        public float decayHumidity       = 0.5f;
        // electricEnergy: zeroed behaviourally if no source this tick (DecaySystem)
        // structuralIntegrity: no self-repair (spec invariant)

        [Header("Deactivation")]
        [Tooltip("Max distance from baseline for a property to be considered stable.")]
        public float deactivationTolerance = 2.0f;

        [Header("Atmosphere")]
        [Tooltip("Gas type that fills the atmosphere (e.g., AIR).")]
        public MaterialType atmosphereGas = MaterialType.AIR;

        [Tooltip("Atmospheric temperature in Celsius.")]
        public float atmosphereTemperature = 23f;

        [Tooltip("Gas concentration outside tiles. 0 = vacuum (default). " +
                 "Raise only for dense-atmosphere levels (e.g. natural gas cave).")]
        [Range(0f, 100f)]
        public float atmosphereConcentration = 0f;

        [Tooltip("Diffusion rate between tiles and atmosphere (0–1).")]
        [Range(0f, 1f)]
        public float atmosphereDiffusionRate = 0.10f;

        [Header("Gas — Concentration")]
        [Tooltip("Gas produced per tick by an actively burning tile (ON_FIRE).")]
        public float gasProductionRate = 5f;

        [Tooltip("Minimum concentration for gas to render. 15 = light haze, 40 = fully opaque.")]
        [Range(0f, 100f)]
        public float gasVisibilityThreshold = 15f;

        [Tooltip("Concentration above which an open tile vents actively to the exterior.")]
        [Range(0f, 100f)]
        public float ventThreshold = 20f;

        [Tooltip("Concentration percentage that escapes per tick during active venting.")]
        [Range(0f, 50f)]
        public float ventRate = 15f;

        [Tooltip("Minimum gas concentration for R10 to evaluate flammable gas ignition.")]
        [Range(0f, 100f)]
        public float gasIgnitionThreshold = 10f;

        [Header("Pressure & Implosion")]
        [Tooltip("How much excess gas (above atmosphere) converts to pressure per tick.")]
        public float pressureFromGasCoeff = 0f;

        [Tooltip("Pressure below which implosion collapses fragile materials.")]
        public float implosionThreshold = 20f;

        [Tooltip("Structural damage dealt to implosion-vulnerable tiles.")]
        public float implosionDamage = 40f;

        [Tooltip("Max tiles processed per diffusion tick. Default 1024 = 32×32 grid.")]
        public int maxDiffusionTilesPerTick = 1024;
    }
}