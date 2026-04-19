using System;

namespace PhysicsSystem.States
{
    [Flags]
    public enum StateFlags
    {
        NONE               = 0,
        ON_FIRE            = 1 << 0,
        ELECTRIFIED        = 1 << 1,
        PRESSURIZED        = 1 << 2,
        FLOODED            = 1 << 3,
        CONTAMINATED       = 1 << 4,
        STRUCTURALLY_WEAK  = 1 << 5,
        VOLATILE           = 1 << 6,
        COLLAPSED          = 1 << 7
    }
}