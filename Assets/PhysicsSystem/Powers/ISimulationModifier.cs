using PhysicsSystem.Core;
using PhysicsSystem.Rules;

namespace PhysicsSystem.Powers
{
    /// <summary>
    /// Contrato base para todo modificador de simulación.
    /// V1: solo ApplyProperties. ApplyRuleModifiers reservado para poderes avanzados.
    /// </summary>
    public interface ISimulationModifier
    {
        string Id          { get; }
        float  Duration    { get; }   // segundos restantes; -1 = permanente
        bool   IsExpired   { get; }

        /// <summary>Aplica deltas de propiedades al área del modifier.</summary>
        void ApplyProperties(PhysicsGrid grid);

        /// <summary>Tick de vida — llamado por SimulationModifierRegistry cada Update.</summary>
        void Tick(float deltaTime);

        // Reservado V2 — modificadores de reglas temporales.
        // void ApplyRuleModifiers(RuleRegistry registry);
        // void RemoveRuleModifiers(RuleRegistry registry);
    }
}
