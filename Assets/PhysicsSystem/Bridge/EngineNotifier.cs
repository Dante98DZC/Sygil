// Assets/PhysicsSystem/Bridge/EngineNotifier.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using PhysicsSystem.Core;
using PhysicsSystem.States;

namespace PhysicsSystem.Bridge
{
    /// <summary>
    /// Compara snapshot pre-tick vs post-tick y emite eventos de cambio.
    ///
    /// Eventos:
    ///   OnMaterialChanged    — el material del tile cambió (Render-1)
    ///   OnTileStatesChanged  — los derivedStates del tile cambiaron
    ///   OnPropertiesChanged  — al menos una propiedad numérica cambió (Render-2b)
    ///
    /// v4: TilePropertySnapshot elimina Pressure / Humidity y agrega LiquidVolume.
    /// OnMaterialChanged sigue emitiendo el material calculado (capa dominante)
    /// para compatibilidad con SimulationRenderer hasta que se migre a Render-2a.
    /// </summary>
    public class EngineNotifier
    {
        // ── Eventos ──────────────────────────────────────────────────────────

        public event Action<Vector2Int, StateFlags, StateFlags>    OnTileStatesChanged;
        public event Action<Vector2Int, MaterialType, MaterialType> OnMaterialChanged;

        /// <summary>
        /// Dispara cuando cambia cualquier propiedad numérica de un tile.
        /// No incluye cambios de material (esos van a OnMaterialChanged).
        /// </summary>
        public event Action<Vector2Int, TilePropertySnapshot> OnPropertiesChanged;

        // ── Snapshots internos ───────────────────────────────────────────────

        private readonly Dictionary<Vector2Int, StateFlags>           _prevStates    = new();
        private readonly Dictionary<Vector2Int, MaterialType>         _prevMaterials = new();
        private readonly Dictionary<Vector2Int, TilePropertySnapshot> _prevProps     = new();

        private const float PropertyChangeTolerance = 0.1f;

        // ── Snapshot ─────────────────────────────────────────────────────────

        /// <summary>
        /// Captura el estado pre-tick de todos los tiles activos y sus vecinos.
        /// Incluir vecinos evita que Dispatch compare tiles recién activados
        /// durante el tick contra TilePropertySnapshot.Zero.
        /// </summary>
        public void Snapshot(PhysicsGrid grid)
        {
            foreach (var pos in grid.ActiveTiles)
                SnapshotTile(pos, grid);

            foreach (var pos in grid.ActiveTiles)
                foreach (var neighbor in grid.GetNeighborPositions(pos))
                    if (!_prevMaterials.ContainsKey(neighbor))
                        SnapshotTile(neighbor, grid);
        }

        private void SnapshotTile(Vector2Int pos, PhysicsGrid grid)
        {
            var tile = grid.GetTile(pos);
            _prevStates[pos]    = tile.derivedStates;

            // Usar capa dominante para compat con SimulationRenderer (Render-1).
            // TODO Render-2a: migrar a snapshot por capa (groundMaterial / liquidMaterial / gasMaterial).
#pragma warning disable CS0618
            _prevMaterials[pos] = tile.material;
#pragma warning restore CS0618

            _prevProps[pos] = TilePropertySnapshot.From(tile);
        }

        // ── Dispatch ─────────────────────────────────────────────────────────

        public void Dispatch(PhysicsGrid grid)
        {
            foreach (var pos in grid.ActiveTiles)
            {
                var tile = grid.GetTile(pos);

                // ── States ───────────────────────────────────────────────────
                if (_prevStates.TryGetValue(pos, out var prevState) && prevState != tile.derivedStates)
                    OnTileStatesChanged?.Invoke(pos, prevState, tile.derivedStates);

                // ── Material (capa dominante — compat Render-1) ──────────────
#pragma warning disable CS0618
                var prevMat = _prevMaterials.TryGetValue(pos, out var pm) ? pm : MaterialType.EMPTY;
                if (prevMat != tile.material)
                    OnMaterialChanged?.Invoke(pos, prevMat, tile.material);
#pragma warning restore CS0618

                // ── Propiedades numéricas ────────────────────────────────────
                if (OnPropertiesChanged == null) continue;

                var prevP = _prevProps.TryGetValue(pos, out var pp) ? pp : TilePropertySnapshot.Zero;
                var curr  = TilePropertySnapshot.From(tile);

                if (prevP.HasSignificantChange(curr, PropertyChangeTolerance))
                    OnPropertiesChanged.Invoke(pos, curr);
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Snapshot ligero de las propiedades numéricas de un tile.
    /// Struct para evitar allocaciones en el hot path.
    ///
    /// v4: elimina Pressure y Humidity (campos removidos de TileData).
    ///     Agrega LiquidVolume como nuevo indicador de estado de la capa Liquid.
    /// </summary>
    public readonly struct TilePropertySnapshot
    {
        public readonly float Temperature;
        public readonly float ElectricEnergy;
        public readonly float GasDensity;
        public readonly float StructuralIntegrity;
        public readonly float LiquidVolume;

        public static readonly TilePropertySnapshot Zero = default;

        private TilePropertySnapshot(float t, float e, float g, float s, float lv)
        {
            Temperature         = t;
            ElectricEnergy      = e;
            GasDensity          = g;
            StructuralIntegrity = s;
            LiquidVolume        = lv;
        }

        public static TilePropertySnapshot From(TileData tile) => new(
            tile.temperature,
            tile.electricEnergy,
            tile.gasDensity,
            tile.structuralIntegrity,
            tile.liquidVolume
        );

        public bool HasSignificantChange(TilePropertySnapshot other, float tolerance) =>
            Mathf.Abs(Temperature         - other.Temperature)         > tolerance ||
            Mathf.Abs(ElectricEnergy      - other.ElectricEnergy)      > tolerance ||
            Mathf.Abs(GasDensity          - other.GasDensity)          > tolerance ||
            Mathf.Abs(StructuralIntegrity - other.StructuralIntegrity) > tolerance ||
            Mathf.Abs(LiquidVolume        - other.LiquidVolume)        > tolerance;

        /// <summary>
        /// Intensidad dominante normalizada [0..1].
        /// Usada por PropertyOverlayRenderer para el modo combinado (tecla 9).
        /// LiquidVolume se normaliza contra 1000 (Deep capacity máxima).
        /// </summary>
        public float DominantIntensity =>
            Mathf.Max(
                Temperature   / 100f,
                ElectricEnergy / 100f,
                GasDensity    / 100f,
                LiquidVolume  / 1000f
            );
    }
}