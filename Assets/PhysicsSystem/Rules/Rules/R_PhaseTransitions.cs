// Assets/PhysicsSystem/Rules/Rules/R_PhaseTransitions.cs
using UnityEngine;
using PhysicsSystem.Core;

namespace PhysicsSystem.Rules.Rules
{
    // ── R13 Melting — sólido → líquido ────────────────────────────────────────

    /// <summary>
    /// Un sólido en groundMaterial se funde cuando su temperatura supera
    /// heatingTransition.triggerTemperature. El líquido resultante pasa a liquidMaterial con un
    /// volumen inicial proporcional a la capacidad del tile.
    /// El suelo queda EMPTY (la materia se convirtió en líquido).
    /// </summary>
    public class R13_Melting : IInteractionRule
    {
        public RuleID        Id          => RuleID.R13_MELTING;
        public TickType      TickType    => TickType.INTEGRITY;
        public int           Priority    => 3;
        public MaterialLayer SourceLayer => MaterialLayer.Ground;

        private const float MeltFillFraction = 0.5f;

        private readonly float _minTemperature;
        private readonly float _maxTemperature;

        private MaterialType _resultMaterial;
        private float        _latentHeat;

        public R13_Melting(float minTemp, float maxTemp)
        {
            _minTemperature = minTemp;
            _maxTemperature = maxTemp;
        }

        public R13_Melting() : this(0f, 100f) { }

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def)
        {
            if (def == null) return false;
            if (!def.HasHeatingTransition) return false;
            if (def.MatterState != MatterState.Solid) return false;
            if (tile.groundMaterial == MaterialType.EMPTY) return false;
            if (tile.temperature < def.heatingTransition.triggerTemperature) return false;
            if (tile.LiquidCapacity <= 0f) return false;

            _resultMaterial = def.heatingTransition.resultMaterial;
            _latentHeat = def.heatingTransition.latentHeat;
            return true;
        }

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            tile.liquidMaterial = _resultMaterial;
            tile.liquidVolume   = tile.LiquidCapacity * MeltFillFraction;
            tile.groundMaterial = MaterialType.EMPTY;
            tile.temperature    = Mathf.Clamp(tile.temperature - _latentHeat, _minTemperature, _maxTemperature);
        }
    }

    // ── R14 Freezing — líquido → sólido ──────────────────────────────────────

    /// <summary>
    /// Un líquido en liquidMaterial solidifica cuando su temperatura cae
    /// por debajo de coolingTransition.triggerTemperature. El sólido resultante pasa a groundMaterial.
    /// El volumen de líquido se consume completamente.
    /// </summary>
    public class R14_Freezing : IInteractionRule
    {
        public RuleID        Id          => RuleID.R14_FREEZING;
        public TickType      TickType    => TickType.INTEGRITY;
        public int           Priority    => 3;
        public MaterialLayer SourceLayer => MaterialLayer.Liquid;

        private readonly float _minTemperature;
        private readonly float _maxTemperature;

        private MaterialType _resultMaterial;
        private float        _latentHeat;

        public R14_Freezing(float minTemp, float maxTemp)
        {
            _minTemperature = minTemp;
            _maxTemperature = maxTemp;
        }

        public R14_Freezing() : this(0f, 100f) { }

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def)
        {
            if (def == null) return false;
            if (!def.HasCoolingTransition) return false;
            if (def.MatterState != MatterState.Liquid) return false;
            if (tile.liquidMaterial == MaterialType.EMPTY) return false;
            if (tile.liquidVolume <= 0f) return false;
            if (tile.temperature > def.coolingTransition.triggerTemperature) return false;

            _resultMaterial = def.coolingTransition.resultMaterial;
            _latentHeat = def.coolingTransition.latentHeat;
            return true;
        }

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            tile.groundMaterial = _resultMaterial;
            tile.liquidMaterial = MaterialType.EMPTY;
            tile.liquidVolume   = 0f;
            tile.temperature    = Mathf.Clamp(tile.temperature + _latentHeat, _minTemperature, _maxTemperature);
        }
    }

    // ── R15 Boiling — líquido → gas ───────────────────────────────────────────

    /// <summary>
    /// Un líquido en liquidMaterial hierve cuando su temperatura supera
    /// heatingTransition.triggerTemperature. El gas resultante pasa a gasMaterial.
    /// Si ya hay gas en gasMaterial, la ebullición solo incrementa gasDensity.
    /// Presuriza y transfiere calor a los vecinos.
    /// </summary>
    public class R15_Boiling : IInteractionRule
    {
        public RuleID        Id          => RuleID.R15_BOILING;
        public TickType      TickType    => TickType.INTEGRITY;
        public int           Priority    => 2;
        public MaterialLayer SourceLayer => MaterialLayer.Liquid;

        private const float GasDensityGain     = 15f;
        private const float NeighborDensityGain =  5f;

        private readonly float _minTemperature;
        private readonly float _maxTemperature;

        private MaterialType _resultMaterial;
        private float        _latentHeat;
        private float        _triggerTemperature;

        public R15_Boiling(float minTemp, float maxTemp)
        {
            _minTemperature = minTemp;
            _maxTemperature = maxTemp;
        }

        public R15_Boiling() : this(0f, 100f) { }

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def)
        {
            if (def == null) return false;
            if (!def.HasHeatingTransition) return false;
            if (def.MatterState != MatterState.Liquid) return false;
            if (tile.liquidMaterial == MaterialType.EMPTY) return false;
            if (tile.liquidVolume <= 0f) return false;
            if (tile.temperature < def.heatingTransition.triggerTemperature) return false;

            _resultMaterial = def.heatingTransition.resultMaterial;
            _latentHeat = def.heatingTransition.latentHeat;
            _triggerTemperature = def.heatingTransition.triggerTemperature;
            return true;
        }

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            float evaporationRate = Mathf.Clamp01(
                (tile.temperature - _triggerTemperature) / 20f) * 50f + 10f;

            float volumeToEvaporate = Mathf.Min(tile.liquidVolume, evaporationRate);

            if (tile.gasMaterial == MaterialType.EMPTY)
                tile.gasMaterial = _resultMaterial;

            tile.liquidVolume = Mathf.Clamp(tile.liquidVolume - volumeToEvaporate, 0f, tile.LiquidCapacity);
            if (tile.liquidVolume <= 0f)
                tile.liquidMaterial = MaterialType.EMPTY;

            float gasProduced = volumeToEvaporate * (GasDensityGain / 50f);
            tile.gasDensity = Mathf.Clamp(tile.gasDensity + gasProduced, 0f, 100f);
            tile.temperature = Mathf.Clamp(tile.temperature - _latentHeat, _minTemperature, _maxTemperature);

            for (int i = 0; i < neighbors.Length; i++)
            {
                float htc = neighborDefs[i] != null ? neighborDefs[i].heatTransferCoeff : 0f;
                neighbors[i].gasDensity = Mathf.Clamp(
                    neighbors[i].gasDensity + NeighborDensityGain * htc, 0f, 100f);
            }
        }
    }

    // ── R16 Condensation — gas → líquido ─────────────────────────────────────

    /// <summary>
    /// Un gas en gasMaterial condensa cuando su temperatura cae por debajo
    /// de coolingTransition.triggerTemperature. El líquido resultante pasa a liquidMaterial.
    /// Si ya hay líquido, el gas desaparece (se disuelve) y suma volumen.
    /// </summary>
    public class R16_Condensation : IInteractionRule
    {
        public RuleID        Id          => RuleID.R16_CONDENSATION;
        public TickType      TickType    => TickType.INTEGRITY;
        public int           Priority    => 2;
        public MaterialLayer SourceLayer => MaterialLayer.Gas;

        private const float CondensationVolume  = 20f;
        private const float GasDensityLoss      = 10f;

        private readonly float _minTemperature;
        private readonly float _maxTemperature;

        private MaterialType _resultMaterial;
        private float        _latentHeat;

        public R16_Condensation(float minTemp, float maxTemp)
        {
            _minTemperature = minTemp;
            _maxTemperature = maxTemp;
        }

        public R16_Condensation() : this(0f, 100f) { }

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def)
        {
            if (def == null) return false;
            if (!def.HasCoolingTransition) return false;
            if (def.MatterState != MatterState.Gas) return false;
            if (tile.gasMaterial == MaterialType.EMPTY) return false;
            if (tile.temperature > def.coolingTransition.triggerTemperature) return false;

            _resultMaterial = def.coolingTransition.resultMaterial;
            _latentHeat = def.coolingTransition.latentHeat;
            return true;
        }

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            tile.gasMaterial = MaterialType.EMPTY;
            tile.gasDensity  = Mathf.Clamp(tile.gasDensity - GasDensityLoss, 0f, 100f);

            float capacity = tile.LiquidCapacity;
            if (capacity > 0f)
            {
                if (tile.liquidMaterial == MaterialType.EMPTY)
                {
                    tile.liquidMaterial = _resultMaterial;
                    tile.liquidVolume   = Mathf.Min(CondensationVolume, capacity);
                }
                else
                {
                    tile.liquidVolume = Mathf.Clamp(tile.liquidVolume + CondensationVolume, 0f, capacity);
                }
            }

            tile.temperature = Mathf.Clamp(tile.temperature + _latentHeat, _minTemperature, _maxTemperature);
        }
    }
}