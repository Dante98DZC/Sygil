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
        public RuleID   Id       => RuleID.R13_MELTING;
        public TickType TickType => TickType.INTEGRITY;
        public int      Priority => 3;
        public MaterialLayer SourceLayer => MaterialLayer.Ground;

        // Fracción de LiquidCapacity que ocupa el sólido fundido.
        // Valor conservador: el sólido no llena el tile completamente de líquido.
        private const float MeltFillFraction = 0.5f;

        private MaterialType _liquidForm;

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def)
        {
            if (def == null || def.meltingPoint <= 0f)       return false;
            if (def.matterState != MatterState.Solid)        return false;
            if (tile.groundMaterial == MaterialType.EMPTY)   return false;
            if (tile.temperature < def.meltingPoint)         return false;
            if (def.liquidForm == MaterialType.EMPTY)        return false;
            if (tile.LiquidCapacity <= 0f)                   return false;

            _liquidForm = def.liquidForm;
            return true;
        }

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            // El sólido se convierte en líquido
            tile.liquidMaterial = _liquidForm;
            tile.liquidVolume   = tile.LiquidCapacity * MeltFillFraction;
            tile.groundMaterial = MaterialType.EMPTY;

            // La fusión consume energía térmica (calor latente)
            tile.temperature = Mathf.Clamp(tile.temperature - 5f, 0f, 100f);
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
        public RuleID   Id       => RuleID.R14_FREEZING;
        public TickType TickType => TickType.INTEGRITY;
        public int      Priority => 3;
        public MaterialLayer SourceLayer => MaterialLayer.Liquid;

        private MaterialType _solidForm;

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def)
        {
            if (def == null || def.freezingPoint <= 0f)      return false;
            if (def.matterState != MatterState.Liquid)       return false;
            if (tile.liquidMaterial == MaterialType.EMPTY)   return false;
            if (tile.liquidVolume   <= 0f)                   return false;
            if (tile.temperature    > def.freezingPoint)     return false;
            if (def.solidForm == MaterialType.EMPTY)         return false;

            _solidForm = def.solidForm;
            return true;
        }

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            tile.groundMaterial = _solidForm;
            tile.liquidMaterial = MaterialType.EMPTY;
            tile.liquidVolume   = 0f;

            // La solidificación libera calor latente
            tile.temperature = Mathf.Clamp(tile.temperature + 3f, 0f, 100f);
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
        public RuleID   Id       => RuleID.R15_BOILING;
        public TickType TickType => TickType.INTEGRITY;
        public int      Priority => 2;
        public MaterialLayer SourceLayer => MaterialLayer.Liquid;

        private const float GasDensityGain    = 15f;  // densidad de gas producido
        private const float NeighborDensityGain =  5f;  // presurización de vecinos

        private MaterialType _gasForm;

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def)
        {
            if (def == null || def.boilingPoint <= 0f)       return false;
            if (def.matterState != MatterState.Liquid)       return false;
            if (tile.liquidMaterial == MaterialType.EMPTY)   return false;
            if (tile.liquidVolume   <= 0f)                   return false;
            if (tile.temperature    < def.boilingPoint)      return false;
            if (def.gasForm == MaterialType.EMPTY)           return false;

            _gasForm = def.gasForm;
            return true;
        }

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            if (tile.gasMaterial == MaterialType.EMPTY)
            {
                // El líquido pasa completamente a gas
                tile.gasMaterial  = _gasForm;
                tile.liquidMaterial = MaterialType.EMPTY;
                tile.liquidVolume   = 0f;
            }

            // La ebullición siempre incrementa la densidad de gas (presión)
            tile.gasDensity  = Mathf.Clamp(tile.gasDensity  + GasDensityGain, 0f, 100f);

            // La ebullición consume energía térmica
            tile.temperature = Mathf.Clamp(tile.temperature - 8f, 0f, 100f);

            // El vapor presuriza los vecinos vía gasDensity
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
        public RuleID   Id       => RuleID.R16_CONDENSATION;
        public TickType TickType => TickType.INTEGRITY;
        public int      Priority => 2;
        public MaterialLayer SourceLayer => MaterialLayer.Gas;

        // Volumen inicial del líquido condensado (litros)
        private const float CondensationVolume = 20f;

        private MaterialType _condensedForm;

        public bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def)
        {
            if (def == null || def.condensationPoint <= 0f)  return false;
            if (def.matterState != MatterState.Gas)          return false;
            if (tile.gasMaterial == MaterialType.EMPTY)      return false;
            if (tile.temperature > def.condensationPoint)    return false;
            if (def.condensedForm == MaterialType.EMPTY)     return false;

            _condensedForm = def.condensedForm;
            return true;
        }

        public void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs)
        {
            tile.gasMaterial = MaterialType.EMPTY;
            tile.gasDensity  = Mathf.Clamp(tile.gasDensity - 10f, 0f, 100f);

            float capacity = tile.LiquidCapacity;
            if (capacity > 0f)
            {
                if (tile.liquidMaterial == MaterialType.EMPTY)
                {
                    // Gas condensa en nuevo líquido
                    tile.liquidMaterial = _condensedForm;
                    tile.liquidVolume   = Mathf.Min(CondensationVolume, capacity);
                }
                else
                {
                    // Ya hay líquido — el gas se disuelve y suma volumen
                    tile.liquidVolume = Mathf.Clamp(
                        tile.liquidVolume + CondensationVolume, 0f, capacity);
                }
            }
            // Si LiquidCapacity == 0 (tile sólido) el gas se disuelve sin generar líquido

            // La condensación libera calor latente
            tile.temperature = Mathf.Clamp(tile.temperature + 2f, 0f, 100f);
        }
    }
}