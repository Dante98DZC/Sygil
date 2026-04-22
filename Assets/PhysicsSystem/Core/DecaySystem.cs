// Assets/PhysicsSystem/Core/DecaySystem.cs
using System.Collections.Generic;
using UnityEngine;
using PhysicsSystem.Config;
using PhysicsSystem.States;

namespace PhysicsSystem.Core
{
    /// <summary>
    /// Aplica decay por propiedad cada SLOW tick, después de diffusion.
    /// Es el único árbitro de si un tile es estable.
    ///
    /// gasConcentration es manejada por difusión con atmósfera (no hay decay forzado).
    /// Modela entropía: los gradientes tienden al equilibrio.
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

                var activeMat = tile.GetActiveMaterial();
                var def = _library.Get(activeMat);

                ApplyDecay(ref tile, pos);

                if (IsStable(ref tile, def))
                    grid.TryDeactivateTile(pos);
            }
        }

        // ── Decay logic ──────────────────────────────────────────────────────

        private void ApplyDecay(ref TileData tile, Vector2Int pos)
        {
            // Temperatura: protegida si ON_FIRE (DerivedStateComputer ya corrió)
            // Temperatura ambiente es manejada por difusión con atmósfera
            bool onFire = (tile.derivedStates & StateFlags.ON_FIRE) != 0;
            if (!onFire)
                tile.temperature = Mathf.Max(0f, tile.temperature - _config.decayTemperature);

            // gasConcentration: sin decay forzado — difusión con atmósfera lo maneja
            // Tiles abiertos tienden a atmosphereConcentration (normalmente 0%)
            if (!tile.isAtmosphereOpen)
            {
                tile.gasConcentration = Mathf.MoveTowards(
                    tile.gasConcentration,
                    _config.atmosphereConcentration,
                    _config.decayGasConcentration);
            }

            // Electricidad: zeroing si no hubo fuente este tick
            if (!_electricSourceTiles.Contains(pos))
                tile.electricEnergy = 0f;
        }

        // ── Deactivation logic ───────────────────────────────────────────────

        /// <summary>
        /// Árbitro único de estabilidad.
        ///
        /// Un tile es estable cuando:
        /// - Su temperatura está cerca de la temperatura atmosférica
        /// - Su gasConcentration está cerca de la concentración atmosférica (solo si NO está abierto a atmósfera)
        /// - No tiene líquido activo
        /// - Su integridad estructural está en baseline
        /// - Las tres capas de material están en EMPTY
        /// </summary>
        private bool IsStable(ref TileData tile, MaterialDefinition def)
        {
            float t = _config.deactivationTolerance;

            if (Mathf.Abs(tile.temperature - _config.atmosphereTemperature) > t) return false;
            if (tile.electricEnergy > t) return false;

            if (!tile.isAtmosphereOpen)
            {
                if (Mathf.Abs(tile.gasConcentration - _config.atmosphereConcentration) > t) return false;
            }

            if (tile.liquidVolume > t) return false;

            float integrityBaseline = def != null ? def.structural.integrityBase : 0f;
            if (tile.structuralIntegrity < integrityBaseline - t) return false;

            bool allEmpty = tile.groundMaterial == MaterialType.EMPTY &&
                            tile.liquidMaterial  == MaterialType.EMPTY &&
                            tile.gasMaterial     == MaterialType.EMPTY;
            if (allEmpty && !tile.wasEmpty) return false;

            return true;
        }
    }
}