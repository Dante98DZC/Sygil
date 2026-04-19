using UnityEngine;
using PhysicsSystem.Core;
using PhysicsSystem.States;

namespace PhysicsSystem.Bridge
{
    public struct TileUpdateEvent
    {
        public Vector2Int position;
        public StateFlags previousStates;
        public StateFlags currentStates;
        public MaterialType previousMaterial;
        public MaterialType currentMaterial;
    }
}