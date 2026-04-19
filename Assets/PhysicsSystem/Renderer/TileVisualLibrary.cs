// Assets/PhysicsSystem/Renderer/TileVisualLibrary.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using PhysicsSystem.Core;

namespace PhysicsSystem.Renderer
{
    [CreateAssetMenu(fileName = "TileVisualLibrary", menuName = "Sygil/Tile Visual Library")]
    public class TileVisualLibrary : ScriptableObject
    {
        [SerializeField] private TileVisualDefinition[] _definitions;

        private Dictionary<MaterialType, TileBase> _map;

        public void Initialize()
        {
            _map = new Dictionary<MaterialType, TileBase>(_definitions.Length);
            foreach (var def in _definitions)
            {
                if (def == null)
                {
                    Debug.LogWarning("[TileVisualLibrary] Definición null en el array.");
                    continue;
                }
                if (def.tile == null)
                {
                    Debug.LogWarning($"[TileVisualLibrary] '{def.materialType}' no tiene TileBase asignado.");
                    continue;
                }
                _map[def.materialType] = def.tile;
            }
            Debug.Log($"[TileVisualLibrary] Inicializado: {_map.Count} materiales.");
        }

        public TileBase Get(MaterialType type) =>
            _map.TryGetValue(type, out var tile) ? tile : null;
    }
}