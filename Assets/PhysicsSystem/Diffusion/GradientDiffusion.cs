using UnityEngine;
using PhysicsSystem.Core;
using PhysicsSystem.Config;
using PhysicsSystem.Rules;
using System.Collections.Generic;

namespace PhysicsSystem.Diffusion
{
    public enum GradientProperty { Temperature, ElectricEnergy }

    public class GradientDiffusion : IDiffusionStrategy
    {
        private readonly GradientProperty _property;
        private readonly GradientConfig _config;

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
            var activeTiles = new List<Vector2Int>(grid.ActiveTiles);

            float atmTemperature = config.atmosphereTemperature;
            float atmDiffusionRate = config.atmosphereDiffusionRate;

            var snapshot = new Dictionary<Vector2Int, float>(activeTiles.Count);
            foreach (var pos in activeTiles)
                snapshot[pos] = GetValue(grid.GetTile(pos));

            foreach (var pos in activeTiles)
            {
                float sourceVal = snapshot[pos];
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

                    float neighborVal = snapshot.TryGetValue(npos, out float sv) ? sv : GetValue(neighbor);

                    float transfer = (sourceVal - neighborVal)
                                     * Mathf.Min(coeff, _config.GetCoeff(nDef)) * 0.25f;

                    if (transfer <= 0f) continue;

                    AddValue(ref tile, -transfer);
                    AddValue(ref neighbor, transfer);
                    grid.MarkDirty(npos);
                }

                if (_property == GradientProperty.Temperature && tile.isAtmosphereOpen)
                {
                    float atmDiff = sourceVal - atmTemperature;
                    if (Mathf.Abs(atmDiff) > 0.1f)
                    {
                        float exchange = atmDiff * atmDiffusionRate;
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
    }

    internal readonly struct GradientConfig
    {
        private readonly System.Func<MaterialDefinition, float> _coeff;

        public GradientConfig(System.Func<MaterialDefinition, float> coeff) => _coeff = coeff;
        public float GetCoeff(MaterialDefinition def) => _coeff(def);
    }
}