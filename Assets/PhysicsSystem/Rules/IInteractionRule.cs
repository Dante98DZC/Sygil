// Assets/PhysicsSystem/Rules/IInteractionRule.cs
using PhysicsSystem.Core;

namespace PhysicsSystem.Rules
{
    public enum TickType { FAST, STANDARD, SLOW, INTEGRITY }

    public enum RuleID
    {
        // Reglas originales
        R01_COMBUSTION,
        R02_EVAPORATION,
        R03_ELECTRIC_PROPAGATION,
        R04_ELECTRIC_WATER,
        R05_PRESSURE_EXPLOSION,
        R06_PRESSURE_RELEASE,
        R07_STRUCTURAL_COLLAPSE,
        R08_HUMIDITY_VAPORIZATION,
        R09_HEAT_SUPPRESSION,
        R10_GAS_IGNITION,
        R11_GAS_PRODUCTION,
        R12_GAS_PRESSURE,

        // Transiciones de estado de materia
        R13_MELTING,
        R14_FREEZING,
        R15_BOILING,
        R16_CONDENSATION,

        // Filtración
        R17_FILTRATION,
    }

    public interface IInteractionRule
    {
        // ── Contract ─────────────────────────────────────────────────────────────
        // Rules trust that def is non-null. RuleRegistry.Evaluate validates
        // def before calling CanApply. Rules should NOT perform null checks.
        // ─────────────────────────────────────────────────────────────

        RuleID   Id       { get; }
        TickType TickType { get; }
        int      Priority { get; }

        MaterialLayer SourceLayer { get; }

        bool CanApply(TileData tile, TileData[] neighbors, MaterialDefinition def);
        void Apply(ref TileData tile, TileData[] neighbors, MaterialDefinition[] neighborDefs);
    }
}