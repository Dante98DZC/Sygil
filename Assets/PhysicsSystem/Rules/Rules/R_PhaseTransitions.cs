// Assets/PhysicsSystem/Rules/Rules/R_PhaseTransitions.cs
using UnityEngine;
using PhysicsSystem.Core;

namespace PhysicsSystem.Rules.Rules
{
    // ── R13 Melting — sólido → líquido ────────────────────────────────────────

    /// <summary>
    /// Un sólido en groundMaterial se funde cuando su temperatura supera
    /// meltingPoint. El líquido resultante pasa a liquidMaterial con un
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

        private MaterialType _liquidForm;
        private float        _latentHeat;

        public R13_Melting(float minTemp, float maxTemp)
        {
            _minTemperature = minTemp;
            _maxTemperature = maxTemp;
        }

        public R13_Melting() : this(0f, 100f) { }

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def)
        {
            if (def == null || def.meltingPoint <= 0f)      return false;
            if (def.matterState != MatterState.Solid)       return false;
            if (tile.groundMaterial == MaterialType.EMPTY)  return false;
            if (tile.temperature < def.meltingPoint)        return false;
            if (def.liquidForm == MaterialType.EMPTY)       return false;
            if (tile.LiquidCapacity <= 0f)                  return false;

            _liquidForm = def.liquidForm;
            _latentHeat = def.latentHeatOfFusion;
            return true;
        }

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            tile.liquidMaterial = _liquidForm;
            tile.liquidVolume   = tile.LiquidCapacity * MeltFillFraction;
            tile.groundMaterial = MaterialType.EMPTY;
            tile.temperature    = Mathf.Clamp(tile.temperature - _latentHeat, _minTemperature, _maxTemperature);
        }
    }

    // ── R14 Freezing — líquido → sólido ──────────────────────────────────────

    /// <summary>
    /// Un líquido en liquidMaterial solidifica cuando su temperatura cae
    /// por debajo de freezingPoint. El sólido resultante pasa a groundMaterial.
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

        private MaterialType _solidForm;
        private float        _latentHeat;

        public R14_Freezing(float minTemp, float maxTemp)
        {
            _minTemperature = minTemp;
            _maxTemperature = maxTemp;
        }

        public R14_Freezing() : this(0f, 100f) { }

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def)
        {
            if (def == null || def.freezingPoint <= 0f)     return false;
            if (def.matterState != MatterState.Liquid)      return false;
            if (tile.liquidMaterial == MaterialType.EMPTY)  return false;
            if (tile.liquidVolume <= 0f)                    return false;
            if (tile.temperature > def.freezingPoint)       return false;
            if (def.solidForm == MaterialType.EMPTY)        return false;

            _solidForm  = def.solidForm;
            _latentHeat = def.latentHeatOfFusion;
            return true;
        }

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            tile.groundMaterial = _solidForm;
            tile.liquidMaterial = MaterialType.EMPTY;
            tile.liquidVolume   = 0f;
            tile.temperature    = Mathf.Clamp(tile.temperature + _latentHeat, _minTemperature, _maxTemperature);
        }
    }

    // ── R15 Boiling — líquido → gas ───────────────────────────────────────────

    /// <summary>
    /// Un líquido en liquidMaterial hierve cuando su temperatura supera boilingPoint.
    /// El gas resultante pasa a gasMaterial y su densidad aumenta.
    /// Si ya hay gas en gasMaterial, la ebullición solo incrementa gasDensity.
    /// Presuriza y transfiere calor a los vecinos.
    /// Absorbe el caso WATER→STEAM que manejaba R02 Evaporation.
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

        private MaterialType _gasForm;
        private float        _latentHeat;

        public R15_Boiling(float minTemp, float maxTemp)
        {
            _minTemperature = minTemp;
            _maxTemperature = maxTemp;
        }

        public R15_Boiling() : this(0f, 100f) { }

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def)
        {
            if (def == null || def.boilingPoint <= 0f)      return false;
            if (def.matterState != MatterState.Liquid)      return false;
            if (tile.liquidMaterial == MaterialType.EMPTY)  return false;
            if (tile.liquidVolume <= 0f)                    return false;
            if (tile.temperature < def.boilingPoint)        return false;
            if (def.gasForm == MaterialType.EMPTY)          return false;

            _gasForm    = def.gasForm;
            _latentHeat = def.latentHeatOfVaporization;
            return true;
        }

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            if (tile.gasMaterial == MaterialType.EMPTY)
            {
                tile.gasMaterial    = _gasForm;
                tile.liquidMaterial = MaterialType.EMPTY;
                tile.liquidVolume   = 0f;
            }

            tile.gasDensity  = Mathf.Clamp(tile.gasDensity + GasDensityGain, 0f, 100f);
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
    /// de condensationPoint. El líquido resultante pasa a liquidMaterial.
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

        private MaterialType _condensedForm;
        private float        _latentHeat;

        public R16_Condensation(float minTemp, float maxTemp)
        {
            _minTemperature = minTemp;
            _maxTemperature = maxTemp;
        }

        public R16_Condensation() : this(0f, 100f) { }

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def)
        {
            if (def == null || def.condensationPoint <= 0f) return false;
            if (def.matterState != MatterState.Gas)         return false;
            if (tile.gasMaterial == MaterialType.EMPTY)     return false;
            if (tile.temperature > def.condensationPoint)   return false;
            if (def.condensedForm == MaterialType.EMPTY)    return false;

            _condensedForm = def.condensedForm;
            _latentHeat    = def.latentHeatOfVaporization;
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
                    tile.liquidMaterial = _condensedForm;
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