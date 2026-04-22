using UnityEngine;
using PhysicsSystem.Core;
using PhysicsSystem.Config;
using PhysicsSystem.Rules;
using System.Collections.Generic;

#if UNITY_DEBUG
using System.Diagnostics;
#endif

namespace PhysicsSystem.Diffusion
{
    public enum GradientProperty { Temperature, ElectricEnergy }

    public class GradientDiffusion : IDiffusionStrategy
    {
        private readonly GradientProperty _property;
        private readonly GradientConfig _config;
        private static readonly Dictionary<Vector2Int, float> _snapshotCache = new(128);

        public TickType TickType { get; }

        public GradientDiffusion(GradientProperty property)
        {
            _property = property;
            _config = property == GradientProperty.ElectricEnergy
                ? new GradientConfig(coeff: d => d.electricTransferCoeff)
                : new GradientConfig(coeff: d => d.heatTransferCoeff);

            TickType = property == GradientProperty.ElectricEnergy
                ? TickType.FAST
                : TickType.SLOW;
        }

        public void Diffuse(PhysicsGrid grid, MaterialLibrary lib, SimulationConfig config)
        {
            if (grid.ActiveTiles.Count == 0) return;

            var activeTiles = new List<Vector2Int>(grid.ActiveTiles);
            const float FIXED_DELTA_TIME = 0.016f;
            int maxTiles = config.maxDiffusionTilesPerTick;

            if (activeTiles.Count > maxTiles)
                activeTiles.RemoveRange(maxTiles, activeTiles.Count - maxTiles);

            float atmTemperature = config.atmosphereTemperature;
            float atmDiffusionRate = config.atmosphereDiffusionRate;
            const float maxTransferPerTick = 10f;
            const float minThreshold = 1f;

            _snapshotCache.Clear();
            foreach (var pos in activeTiles)
                _snapshotCache[pos] = GetValue(grid.GetTile(pos));

            foreach (var pos in activeTiles)
            {
                float sourceVal = _snapshotCache[pos];
                if (sourceVal <= 0f && _property == GradientProperty.Temperature) continue;

                ref var tile = ref grid.GetTile(pos);
                var def = lib.Get(tile.groundMaterial);
                if (def == null) continue;

                float coeff = _config.GetCoeff(def);

                foreach (var npos in grid.GetNeighborPositions(pos))
                {
                    ref var neighbor = ref grid.GetTile(npos);
                    var nDef = lib.Get(neighbor.groundMaterial);
                    if (nDef == null) continue;

                    float neighborVal = _snapshotCache.TryGetValue(npos, out float sv) ? sv : GetValue(neighbor);

                    float deltaT = sourceVal - neighborVal;
                    float absDeltaT = Mathf.Abs(deltaT);
                    if (absDeltaT < minThreshold) continue;

                    float combinedCoeff = Mathf.Min(coeff, _config.GetCoeff(nDef));

                    float transfer = deltaT * combinedCoeff * absDeltaT * 0.1f;
                    transfer = Mathf.Clamp(transfer, -maxTransferPerTick, maxTransferPerTick);

                    AddValue(ref tile, -transfer);
                    AddValue(ref neighbor, transfer);
                    grid.MarkDirty(npos);
                }

                if (_property == GradientProperty.Temperature && tile.isAtmosphereOpen)
                {
                    float atmDiff = sourceVal - atmTemperature;
                    float absAtmDiff = Mathf.Abs(atmDiff);
                    if (absAtmDiff > minThreshold)
                    {
                        float exchange = atmDiff * atmDiffusionRate * absAtmDiff * 0.1f;
                        exchange = Mathf.Clamp(exchange, -maxTransferPerTick, maxTransferPerTick);
                        if (Mathf.Abs(exchange) > 0.01f)
                            AddValue(ref tile, -exchange);
                    }
                }
            }
        }

        private float GetValue(TileData t) =>
            _property == GradientProperty.Temperature ? t.temperature : t.electricEnergy;

        private void AddValue(ref TileData t, float delta)
        {
            if (_property == GradientProperty.Temperature)
                t.temperature = Mathf.Clamp(t.temperature + delta, 0f, 100f);
            else
                t.electricEnergy = Mathf.Clamp(t.electricEnergy + delta, 0f, 100f);
        }

#if UNITY_DEBUG
        [Conditional("DEBUG")]
        private void LogTransfer(Vector2Int pos, Vector2Int npos, float amount)
        {
            string propName = _property == GradientProperty.Temperature ? "Temp" : "Electric";
            Debug.Log($"[Diffusion] {propName}: {pos} → {npos}: {amount:F2}");
        }
#endif

        internal readonly struct GradientConfig
        {
            private readonly System.Func<MaterialDefinition, float> _coeff;

            public GradientConfig(System.Func<MaterialDefinition, float> coeff) => _coeff = coeff;
            public float GetCoeff(MaterialDefinition def) => _coeff(def);
        }
    }
}