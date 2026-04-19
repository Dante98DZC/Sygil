using PhysicsSystem.Core;
using PhysicsSystem.Config;
using PhysicsSystem.Rules;

namespace PhysicsSystem.Diffusion
{
    public interface IDiffusionStrategy
    {
        TickType TickType { get; }
        void Diffuse(PhysicsGrid grid, MaterialLibrary lib);
    }
}