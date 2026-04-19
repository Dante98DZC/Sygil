using UnityEngine;
using System.Collections.Generic;
using PhysicsSystem.Core;

namespace PhysicsSystem.Config
{
    [CreateAssetMenu(menuName = "PhysicsSystem/MaterialLibrary")]
    public class MaterialLibrary : ScriptableObject
    {
        [SerializeField] private List<MaterialDefinition> definitions = new();
        private Dictionary<MaterialType, MaterialDefinition> _lookup;

        public void Initialize()
        {
            _lookup = new Dictionary<MaterialType, MaterialDefinition>();
            foreach (var def in definitions)
                _lookup[def.materialType] = def;
        }

        public MaterialDefinition Get(MaterialType type) =>
            _lookup.TryGetValue(type, out var def) ? def : null;

        /// <summary>
        /// Inyecta definiciones desde tests sin depender de assets en disco.
        /// Llama Initialize() después para construir el lookup.
        /// </summary>
        internal void SetDefinitionsForTest(MaterialDefinition[] defs)
        {
            definitions = new List<MaterialDefinition>(defs);
        }
    }
}