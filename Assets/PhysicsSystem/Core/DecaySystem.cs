// Assets/PhysicsSystem/Core/DecaySystem.cs
using System.Collections.Generic;
using UnityEngine;
using PhysicsSystem.Config;
using PhysicsSystem.States;

namespace PhysicsSystem.Core
{
    /// <summary>
    /// Aplica decay por propiedad cada SLOW tick, después de diffusion (I8).
    /// Es el único árbitro de si un tile es estable — PhysicsGrid.TryDeactivateTile
    /// no tiene condiciones propias.
    ///
    /// gasDensity decae hacia gasBaseline (50 = 1 atm) en ambas direcciones:
    ///   > 50 → baja hacia 50 (gas se dispersa)
    ///   < 50 → sube hacia 50 (vacío se rellena de atmósfera)
    /// Modela entropía: los gradientes tienden al equilibrio.
    ///
    /// v4: pressure y humidity eliminados. Solo decaen: temperature, gasDensity,
    /// electricEnergy, structuralIntegrity (indirecto via R07).
    /// </summary>
    public class DecaySystem
    {
        private readonly SimulationConfig _config;
        private readonly MaterialLibrary  _library;

        private readonly HashSet<Vector2Int> _electricSourceTiles = new();

        public DecaySystem(SimulationConfig config, MaterialLibrary library)
        {
            _config  = config;
            _library = library;
        }

        // ── Electric source tracking ─────────────────────────────────────────

        public void RegisterElectricSource(Vector2Int pos) =>
            _electricSourceTiles.Add(pos);

        public void ClearElectricSources() =>
            _electricSourceTiles.Clear();

        // ── Main entry point ─────────────────────────────────────────────────

        public void Apply(PhysicsGrid grid)
        {
            var snapshot = new List<Vector2Int>(grid.ActiveTiles);

            foreach (var pos in snapshot)
            {
                ref var tile = ref grid.GetTile(pos);

                // Def se obtiene del terreno base; el líquido y gas tienen sus propias reglas
                var def = _library.Get(tile.groundMaterial);

                ApplyDecay(ref tile, pos);

                if (IsStable(ref tile, def))
                    grid.TryDeactivateTile(pos);
            }
        }

        // ── Decay logic ──────────────────────────────────────────────────────

        private void ApplyDecay(ref TileData tile, Vector2Int pos)
        {
            // Temperatura: protegida si ON_FIRE (DerivedStateComputer ya corrió)
            bool onFire = (tile.derivedStates & StateFlags.ON_FIRE) != 0;
            if (!onFire)
                tile.temperature = Mathf.Max(0f, tile.temperature - _config.decayTemperature);

            // gasDensity decae hacia el baseline atmosférico en ambas direcciones.
            // MoveTowards garantiza que nunca sobrepasa el objetivo.
            tile.gasDensity = Mathf.MoveTowards(
                tile.gasDensity,
                _config.gasBaseline,
                _config.decayGasDensity);

            // Electricidad: zeroing si no hubo fuente este tick
            if (!_electricSourceTiles.Contains(pos))
                tile.electricEnergy = 0f;
        }

        // ── Deactivation logic ───────────────────────────────────────────────

        /// <summary>
        /// Árbitro único de estabilidad.
        ///
        /// gasDensity es estable cuando está dentro de la tolerancia del baseline (50),
        /// no de 0 — un tile con gasDensity=50 es el estado de reposo normal.
        ///
        /// Un tile es "vacío" cuando las tres capas de material están en EMPTY.
        /// </summary>
        private bool IsStable(ref TileData tile, MaterialDefinition def)
        {
            float t = _config.deactivationTolerance;

            if (tile.temperature    > t) return false;
            if (tile.electricEnergy > t) return false;

            // gasDensity: estable si está cerca del baseline, no de 0
            if (Mathf.Abs(tile.gasDensity - _config.gasBaseline) > t) return false;

            // liquidVolume: estable si el tile está seco
            if (tile.liquidVolume > t) return false;

            // structuralIntegrity: inestable solo si está POR DEBAJO del baseline.
            float integrityBaseline = def != null ? def.integrityBase : 0f;
            if (tile.structuralIntegrity < integrityBaseline - t) return false;

            // Tile vacío que antes no lo era → una transición pendiente de procesar
            bool allEmpty = tile.groundMaterial == MaterialType.EMPTY &&
                            tile.liquidMaterial  == MaterialType.EMPTY &&
                            tile.gasMaterial     == MaterialType.EMPTY;
            if (allEmpty && !tile.wasEmpty) return false;

            return true;
        }
    }
}