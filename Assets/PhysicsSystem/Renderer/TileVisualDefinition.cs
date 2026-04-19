// Assets/PhysicsSystem/Renderer/TileVisualDefinition.cs
using UnityEngine;
using UnityEngine.Tilemaps;
using PhysicsSystem.Core;

namespace PhysicsSystem.Renderer
{
    [CreateAssetMenu(fileName = "TileVisual_", menuName = "Sygil/Tile Visual Definition")]
    public class TileVisualDefinition : ScriptableObject
    {
        public MaterialType materialType;
        public TileBase tile; // arrastra tu sprite/tile de Unity aquí
    }
}