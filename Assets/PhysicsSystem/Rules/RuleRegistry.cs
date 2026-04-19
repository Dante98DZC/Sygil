using UnityEngine;
using System;
using System.Collections.Generic;
using PhysicsSystem.Core;
using PhysicsSystem.Config;

namespace PhysicsSystem.Rules
{
    public class RuleRegistry
    {
        private readonly List<IInteractionRule> _rules = new();
        private readonly SimulationConfig       _config;

        // ── Debug hook (solo activo cuando RuleEventLog está en escena) ───────
        // Suscribirse/desuscribirse es responsabilidad de RuleEventLog.
        // En builds sin el logger este evento nunca tiene suscriptores — costo cero.
        public static event Action<RuleID, Vector2Int> OnRuleFired;

        public RuleRegistry(SimulationConfig config) { _config = config; }

        public void AddRule(IInteractionRule rule)
        {
            _rules.Add(rule);
            _rules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        public void RemoveRule(RuleID id) =>
            _rules.RemoveAll(r => r.Id == id);

        public void Evaluate(
            ref TileData           tile,
            TileData[]             neighbors,
            MaterialDefinition[]   neighborDefs,
            MaterialDefinition     def,
            TickType               tickType,
            Vector2Int             pos)
        {
            if (def == null)
            {
                Debug.LogWarning($"[RuleRegistry] def null para material: {tile.material}");
                return;
            }

            int applied = 0;
            foreach (var rule in _rules)
            {
                if (applied >= _config.maxRulesPerTile) break;
                if (rule.TickType != tickType)          continue;
                if (!rule.CanApply(tile, neighbors, def)) continue;

                rule.Apply(ref tile, neighbors, neighborDefs);
                OnRuleFired?.Invoke(rule.Id, pos);
                applied++;
            }
        }
    }
}