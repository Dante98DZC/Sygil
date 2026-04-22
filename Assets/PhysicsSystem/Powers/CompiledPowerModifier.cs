using System.Collections.Generic;
using UnityEngine;
using PhysicsSystem.Core;

namespace PhysicsSystem.Powers
{
    /// <summary>
    /// ISimulationModifier generado en runtime a partir de un CompiledPower.
    /// Lee la data compilada y la aplica al grid cada tick según TimingMode.
    /// </summary>
    public class CompiledPowerModifier : ISimulationModifier
    {
        // ── ISimulationModifier ───────────────────────────────────────────────
        public string Id       { get; }
        public float  Duration { get; private set; }
        public bool   IsExpired => _expired;

        // ── Estado interno ────────────────────────────────────────────────────
        private readonly CompiledPower _power;
        private readonly Vector2Int    _origin;
        private readonly Vector2 _direction;   // normalizado — para Cone y Ray

        private bool  _expired;
        private float _pulseTimer;
        private int   _pulsesFired;

        public CompiledPowerModifier(CompiledPower power, Vector2Int origin, Vector2 direction)
        {
            Id         = power.powerId;
            _power     = power;
            _origin    = origin;
            _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.up;
            Duration   = power.timing.mode == TimingMode.Instant ? 0f : power.timing.duration;
        }

        // ── ISimulationModifier.Tick ──────────────────────────────────────────
        public void Tick(float deltaTime)
        {
            if (_expired) return;

            switch (_power.timing.mode)
            {
                case TimingMode.Instant:
                    _expired = true;
                    break;

                case TimingMode.Duration:
                    Duration = Mathf.Max(0f, Duration - deltaTime);
                    if (Duration == 0f) _expired = true;
                    break;

                case TimingMode.Pulse:
                    Duration = Mathf.Max(0f, Duration - deltaTime);
                    _pulseTimer += deltaTime;
                    if (Duration == 0f) _expired = true;
                    break;
            }
        }

        // ── ISimulationModifier.ApplyProperties ──────────────────────────────
        public void ApplyProperties(PhysicsGrid grid)
        {
            if (_expired && _power.timing.mode != TimingMode.Instant) return;

            // Instant siempre aplica en el primer frame (antes de que Tick lo expire)
            // Pulse solo aplica cuando el intervalo se cumple
            if (_power.timing.mode == TimingMode.Pulse)
            {
                if (_pulseTimer < _power.timing.interval) return;
                _pulseTimer = 0f;
                _pulsesFired++;

                bool infinitePulses = _power.timing.pulseCount == 0;
                if (!infinitePulses && _pulsesFired >= _power.timing.pulseCount)
                    _expired = true;
            }

            var tiles = ResolveTiles(grid);
            foreach (var (pos, distance) in tiles)
                ApplyEffects(grid, pos, distance);
        }

        // ── Resolución de forma ───────────────────────────────────────────────

        private List<(Vector2Int pos, float distance)> ResolveTiles(PhysicsGrid grid)
        {
            var result = new List<(Vector2Int, float)>();

            switch (_power.delivery)
            {
                case DeliveryMode.Instant:
                case DeliveryMode.Projectile:
                    // Projectile: la posición de impacto ya está en _origin (resuelta por el caster)
                    if (grid.InBounds(_origin))
                        result.Add((_origin, 0f));
                    break;

                case DeliveryMode.Area:
                    CollectArea(grid, result);
                    break;

                case DeliveryMode.Cone:
                    CollectCone(grid, result);
                    break;

                case DeliveryMode.Ray:
                    CollectRay(grid, result);
                    break;
            }

            return result;
        }

        private void CollectArea(PhysicsGrid grid, List<(Vector2Int, float)> result)
        {
            int r = _power.shape.radius;
            for (int x = -r; x <= r; x++)
            for (int y = -r; y <= r; y++)
            {
                var pos      = _origin + _power.shape.offset + new Vector2Int(x, y);
                float distance = Mathf.Sqrt(x * x + y * y);
                if (grid.InBounds(pos) && distance <= r)
                    result.Add((pos, distance));
            }
        }

        private void CollectCone(PhysicsGrid grid, List<(Vector2Int, float)> result)
        {
            int   r         = _power.shape.radius;
            float halfAngle = _power.shape.angle * 0.5f * Mathf.Deg2Rad;

            for (int x = -r; x <= r; x++)
            for (int y = -r; y <= r; y++)
            {
                var   offset   = new Vector2(x, y);
                float distance = offset.magnitude;
                if (distance > r || distance < 0.01f) continue;

                float dot = Vector2.Dot(_direction, offset.normalized);
                if (dot < Mathf.Cos(halfAngle)) continue;

                var pos = _origin + new Vector2Int(x, y);
                if (grid.InBounds(pos))
                    result.Add((pos, distance));
            }
        }

        private void CollectRay(PhysicsGrid grid, List<(Vector2Int, float)> result)
        {
            int length = _power.shape.length;
            for (int step = 0; step <= length; step++)
            {
                var pos = _origin + Vector2Int.RoundToInt(_direction * step);
                if (!grid.InBounds(pos)) break;

                result.Add((pos, step));

                // Ray se detiene al impactar un tile que bloquea movimiento
                var def = grid.GetMaterialDef(pos);
                if (def != null && def.BlocksMovement && step > 0) break;
            }
        }

        // ── Aplicación de efectos ─────────────────────────────────────────────

        private void ApplyEffects(PhysicsGrid grid, Vector2Int pos, float distance)
        {
            ref var tile = ref grid.GetTile(pos);
            bool anyEmpty = tile.groundMaterial == MaterialType.EMPTY &&
                         tile.liquidMaterial == MaterialType.EMPTY &&
                         tile.gasMaterial == MaterialType.EMPTY;
            if (anyEmpty) return;

            bool anyApplied = false;
            foreach (var effect in _power.effects)
            {
                if (!PassesCondition(effect.condition, tile)) continue;

                float value = ComputeFalloff(effect.baseValue, effect.falloff, distance, _power.shape.radius);
                ApplyToProperty(ref tile, effect.target, value);
                anyApplied = true;
            }

            if (anyApplied)
                grid.MarkDirty(pos);
        }

        private static bool PassesCondition(ConditionData cond, TileData tile)
        {
            if (!cond.active) return true;

            if (cond.checkMaterial)
            {
                bool hasMaterial = tile.groundMaterial == cond.requiredMaterial ||
                                 tile.liquidMaterial == cond.requiredMaterial ||
                                 tile.gasMaterial == cond.requiredMaterial;
                if (!hasMaterial) return false;
            }

            if (cond.checkProperty && ReadProperty(tile, cond.checkedProperty) <= cond.threshold)
                return false;

            return true;
        }

        private static float ComputeFalloff(float baseValue, FalloffCurve falloff, float distance, int radius)
        {
            if (radius <= 0) return baseValue;

            float t = Mathf.Clamp01(distance / radius);
            float multiplier = falloff switch
            {
                FalloffCurve.Flat      => 1f,
                FalloffCurve.Linear    => 1f - t,
                FalloffCurve.Quadratic => (1f - t) * (1f - t),
                _                      => 1f
            };

            return baseValue * multiplier;
        }

        private static void ApplyToProperty(ref TileData tile, PropertyTarget target, float delta)
        {
            switch (target)
            {
                case PropertyTarget.Temperature:
                    tile.temperature        = Mathf.Clamp(tile.temperature        + delta, 0f, 100f); break;
                case PropertyTarget.Pressure:
                    tile.gasConcentration  = Mathf.Clamp(tile.gasConcentration  + delta, 0f, 100f); break;
                case PropertyTarget.Humidity:
                    tile.liquidVolume       = Mathf.Clamp(tile.liquidVolume       + delta, 0f, 100f); break;
                case PropertyTarget.ElectricEnergy:
                    tile.electricEnergy     = Mathf.Clamp(tile.electricEnergy     + delta, 0f, 100f); break;
                case PropertyTarget.GasDensity:
                    tile.gasConcentration  = Mathf.Clamp(tile.gasConcentration  + delta, 0f, 100f); break;
                case PropertyTarget.StructuralIntegrity:
                    tile.structuralIntegrity = Mathf.Clamp(tile.structuralIntegrity + delta, 0f, 100f); break;
            }
        }

        private static float ReadProperty(TileData tile, PropertyTarget target) => target switch
        {
            PropertyTarget.Temperature        => tile.temperature,
            PropertyTarget.Pressure           => tile.gasConcentration,
            PropertyTarget.Humidity           => tile.liquidVolume,
            PropertyTarget.ElectricEnergy     => tile.electricEnergy,
            PropertyTarget.GasDensity         => tile.gasConcentration,
            PropertyTarget.StructuralIntegrity => tile.structuralIntegrity,
            _                                  => 0f
        };
    }
}