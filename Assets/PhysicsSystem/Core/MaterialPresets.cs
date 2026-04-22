// Assets/PhysicsSystem/Core/MaterialPresets.cs
// Configuraciones de referencia para los 20 MaterialType del sistema.
//
// USO: Estas son plantillas de partida para crear ScriptableObjects en el Editor.
//      No se usan en runtime — los datos reales vienen de los assets en disco.
//      Útil para: inicialización de tests, scripts de generación de assets, documentación viva.
//
// TABLA RESUMEN DE CAPAS Y TRANSICIONES:
//
//  Material      Layer    Heating →          Cooling ←         Arde
//  ──────────    ──────   ────────────────   ────────────────  ─────
//  STONE         Ground   —                  —                 No
//  WOOD          Ground   —                  —                 Sí (→ASH, →SMOKE)
//  METAL         Ground   80°→MOLTEN_METAL   —                 No
//  ICE           Ground   30°→WATER          —                 No
//  SAND          Ground   85°→MOLTEN_GLASS   —                 No
//  EARTH         Ground   —                  —                 No
//  GLASS         Ground   85°→MOLTEN_GLASS   —                 No
//  ASH           Ground   —                  —                 No
//  WATER         Liquid   80°→STEAM          30°→ICE           No
//  LAVA          Liquid   —                  50°→STONE         No
//  MUD           Liquid   —                  —                 No
//  MOLTEN_METAL  Liquid   —                  80°→METAL         No
//  MOLTEN_GLASS  Liquid   —                  85°→GLASS         No
//  STEAM         Gas      —                  80°→WATER         No
//  SMOKE         Gas      —                  —                 No
//  CO2           Gas      —                  —                 No
//  ROCK_GAS      Gas      —                  —                 Sí
//  AIR           Gas      —                  —                 No

using UnityEngine;

namespace PhysicsSystem.Core
{
    /// <summary>
    /// Factory de configuraciones de referencia para los 20 MaterialType.
    /// Los valores de temperatura usan la escala interna [0..100] del sistema.
    /// </summary>
    public static class MaterialPresets
    {
        // ── Ground — Sólidos ──────────────────────────────────────────────────

        public static MaterialDefinition Stone(MaterialDefinition def)
        {
            def.materialType    = MaterialType.STONE;
            def.layer           = MaterialLayer.Ground;
            def.heatTransferCoeff = 0.3f;
            def.blocksMovement  = true;
            def.blocksVision    = true;
            def.slowsMovement   = false;
            def.movementCost    = 1f;

            def.heatingTransition = PhaseTransition.None;       // no se funde a temperatura de juego
            def.coolingTransition = PhaseTransition.None;

            def.combustion  = CombustionData.NonFlammable;
            def.structural  = new StructuralData
            {
                integrityBase        = 100f,
                collapseInto         = MaterialType.ASH,
                electricTransferCoeff = 0f,
                flammabilityCoeff    = 0f
            };
            return def;
        }

        public static MaterialDefinition Wood(MaterialDefinition def)
        {
            def.materialType    = MaterialType.WOOD;
            def.layer           = MaterialLayer.Ground;
            def.heatTransferCoeff = 0.2f;
            def.blocksMovement  = true;
            def.blocksVision    = true;

            def.heatingTransition = PhaseTransition.None;       // WOOD no se funde, arde
            def.coolingTransition = PhaseTransition.None;

            def.combustion  = CombustionData.Wood;              // arde a 45°, deja ASH, produce SMOKE
            def.structural  = new StructuralData
            {
                integrityBase        = 60f,
                collapseInto         = MaterialType.ASH,
                electricTransferCoeff = 0f,
                flammabilityCoeff    = 0.8f
            };
            return def;
        }

        public static MaterialDefinition Metal(MaterialDefinition def)
        {
            def.materialType    = MaterialType.METAL;
            def.layer           = MaterialLayer.Ground;
            def.heatTransferCoeff = 0.9f;
            def.blocksMovement  = true;
            def.blocksVision    = true;

            def.heatingTransition = PhaseTransition.Heating(80f, MaterialType.MOLTEN_METAL, latentHeat: 6f);
            def.coolingTransition = PhaseTransition.None;

            def.combustion  = CombustionData.NonFlammable;
            def.structural  = new StructuralData
            {
                integrityBase        = 80f,
                collapseInto         = MaterialType.EMPTY,
                electricTransferCoeff = 0.95f,
                flammabilityCoeff    = 0f
            };
            return def;
        }

        public static MaterialDefinition Ice(MaterialDefinition def)
        {
            def.materialType    = MaterialType.ICE;
            def.layer           = MaterialLayer.Ground;
            def.heatTransferCoeff = 0.5f;
            def.blocksMovement  = true;
            def.blocksVision    = false;     // el hielo es translúcido

            def.heatingTransition = PhaseTransition.Heating(30f, MaterialType.WATER, latentHeat: 5f);
            def.coolingTransition = PhaseTransition.None;

            def.combustion  = CombustionData.NonFlammable;
            def.structural  = new StructuralData
            {
                integrityBase        = 40f,
                collapseInto         = MaterialType.WATER,  // colapso estructural → charco
                electricTransferCoeff = 0.1f,
                flammabilityCoeff    = 0f
            };
            return def;
        }

        public static MaterialDefinition Sand(MaterialDefinition def)
        {
            def.materialType    = MaterialType.SAND;
            def.layer           = MaterialLayer.Ground;
            def.heatTransferCoeff = 0.2f;
            def.blocksMovement  = false;
            def.slowsMovement   = true;
            def.movementCost    = 2f;

            def.heatingTransition = PhaseTransition.Heating(85f, MaterialType.MOLTEN_GLASS, latentHeat: 7f);
            def.coolingTransition = PhaseTransition.None;

            def.combustion  = CombustionData.NonFlammable;
            def.structural  = new StructuralData
            {
                integrityBase        = 20f,
                collapseInto         = MaterialType.EMPTY,
                electricTransferCoeff = 0f,
                flammabilityCoeff    = 0f
            };
            return def;
        }

        public static MaterialDefinition Earth(MaterialDefinition def)
        {
            def.materialType    = MaterialType.EARTH;
            def.layer           = MaterialLayer.Ground;
            def.heatTransferCoeff = 0.15f;
            def.blocksMovement  = true;
            def.blocksVision    = true;

            def.heatingTransition = PhaseTransition.None;
            def.coolingTransition = PhaseTransition.None;

            def.combustion  = CombustionData.NonFlammable;
            def.structural  = new StructuralData
            {
                integrityBase        = 50f,
                collapseInto         = MaterialType.MUD,
                electricTransferCoeff = 0.05f,
                flammabilityCoeff    = 0f
            };
            return def;
        }

        public static MaterialDefinition Glass(MaterialDefinition def)
        {
            def.materialType    = MaterialType.GLASS;
            def.layer           = MaterialLayer.Ground;
            def.heatTransferCoeff = 0.4f;
            def.blocksMovement  = true;
            def.blocksVision    = false;     // el vidrio deja pasar la visión

            def.heatingTransition = PhaseTransition.Heating(85f, MaterialType.MOLTEN_GLASS, latentHeat: 7f);
            def.coolingTransition = PhaseTransition.None;

            def.combustion  = CombustionData.NonFlammable;
            def.structural  = new StructuralData
            {
                integrityBase        = 30f,
                collapseInto         = MaterialType.EMPTY,
                electricTransferCoeff = 0f,
                flammabilityCoeff    = 0f
            };
            return def;
        }

        public static MaterialDefinition Ash(MaterialDefinition def)
        {
            def.materialType    = MaterialType.ASH;
            def.layer           = MaterialLayer.Ground;
            def.heatTransferCoeff = 0.05f;
            def.blocksMovement  = false;
            def.slowsMovement   = true;
            def.movementCost    = 1.5f;

            def.heatingTransition = PhaseTransition.None;
            def.coolingTransition = PhaseTransition.None;

            def.combustion  = CombustionData.NonFlammable;
            def.structural  = new StructuralData
            {
                integrityBase        = 10f,
                collapseInto         = MaterialType.EMPTY,
                electricTransferCoeff = 0f,
                flammabilityCoeff    = 0f
            };
            return def;
        }

        // ── Liquid — Fluidos ──────────────────────────────────────────────────

        public static MaterialDefinition Water(MaterialDefinition def)
        {
            def.materialType    = MaterialType.WATER;
            def.layer           = MaterialLayer.Liquid;
            def.heatTransferCoeff = 0.6f;
            def.blocksMovement  = false;
            def.slowsMovement   = true;
            def.movementCost    = 3f;

            def.heatingTransition = PhaseTransition.Heating(80f, MaterialType.STEAM, latentHeat: 8f);
            def.coolingTransition = PhaseTransition.Cooling(30f, MaterialType.ICE,   latentHeat: 5f);

            def.combustion  = CombustionData.NonFlammable;
            def.fluid = FluidData.Water;
            return def;
        }

        public static MaterialDefinition Lava(MaterialDefinition def)
        {
            def.materialType    = MaterialType.LAVA;
            def.layer           = MaterialLayer.Liquid;
            def.heatTransferCoeff = 0.8f;
            def.blocksMovement  = false;
            def.slowsMovement   = true;
            def.movementCost    = 5f;

            def.heatingTransition = PhaseTransition.None;
            def.coolingTransition = PhaseTransition.Cooling(50f, MaterialType.STONE, latentHeat: 10f);

            def.combustion  = CombustionData.NonFlammable;
            def.fluid = FluidData.Viscous;
            return def;
        }

        public static MaterialDefinition Mud(MaterialDefinition def)
        {
            def.materialType    = MaterialType.MUD;
            def.layer           = MaterialLayer.Liquid;
            def.heatTransferCoeff = 0.25f;
            def.blocksMovement  = false;
            def.slowsMovement   = true;
            def.movementCost    = 4f;

            def.heatingTransition = PhaseTransition.None;   // el barro no hierve normalmente
            def.coolingTransition = PhaseTransition.None;   // solidifica via regla R especial

            def.combustion  = CombustionData.NonFlammable;
            def.fluid = new FluidData
            {
                viscosity              = 0.15f,
                soilAbsorptionRate     = 0f,
                soilSaturationCapacity = 0f
            };
            return def;
        }

        public static MaterialDefinition MoltenMetal(MaterialDefinition def)
        {
            def.materialType    = MaterialType.MOLTEN_METAL;
            def.layer           = MaterialLayer.Liquid;
            def.heatTransferCoeff = 0.85f;

            def.heatingTransition = PhaseTransition.None;
            def.coolingTransition = PhaseTransition.Cooling(80f, MaterialType.METAL, latentHeat: 6f);

            def.combustion  = CombustionData.NonFlammable;
            def.fluid = FluidData.Viscous;
            return def;
        }

        public static MaterialDefinition MoltenGlass(MaterialDefinition def)
        {
            def.materialType    = MaterialType.MOLTEN_GLASS;
            def.layer           = MaterialLayer.Liquid;
            def.heatTransferCoeff = 0.5f;

            def.heatingTransition = PhaseTransition.None;
            def.coolingTransition = PhaseTransition.Cooling(85f, MaterialType.GLASS, latentHeat: 7f);

            def.combustion  = CombustionData.NonFlammable;
            def.fluid = new FluidData { viscosity = 0.08f };
            return def;
        }

        // ── Gas ───────────────────────────────────────────────────────────────

        public static MaterialDefinition Steam(MaterialDefinition def)
        {
            def.materialType    = MaterialType.STEAM;
            def.layer           = MaterialLayer.Gas;
            def.heatTransferCoeff = 0.3f;
            def.blocksVision    = false;

            def.heatingTransition = PhaseTransition.None;
            def.coolingTransition = PhaseTransition.Cooling(80f, MaterialType.WATER, latentHeat: 8f);

            def.combustion   = CombustionData.NonFlammable;
            def.atmospheric  = AtmosphericData.Inert;
            def.atmospheric  = new AtmosphericData
            {
                dissipationMultiplier = 1.5f,   // el vapor sube y disipa rápido
                gasPermeabilityCoeff  = 1f,
                isFlammable           = false,
                ignitionTemperature   = 0f
            };
            return def;
        }

        public static MaterialDefinition Smoke(MaterialDefinition def)
        {
            def.materialType    = MaterialType.SMOKE;
            def.layer           = MaterialLayer.Gas;
            def.heatTransferCoeff = 0.1f;
            def.blocksVision    = true;     // humo bloquea visión

            def.heatingTransition = PhaseTransition.None;
            def.coolingTransition = PhaseTransition.None;   // el humo no condensa

            def.combustion  = CombustionData.NonFlammable;
            def.atmospheric = new AtmosphericData
            {
                dissipationMultiplier = 0.8f,   // se disipa lento — bloquea visión más tiempo
                gasPermeabilityCoeff  = 1f,
                isFlammable           = false,
                ignitionTemperature   = 0f
            };
            return def;
        }

        public static MaterialDefinition CO2(MaterialDefinition def)
        {
            def.materialType    = MaterialType.CO2;
            def.layer           = MaterialLayer.Gas;
            def.heatTransferCoeff = 0.2f;
            def.blocksVision    = false;

            def.heatingTransition = PhaseTransition.None;
            def.coolingTransition = PhaseTransition.None;

            def.combustion  = CombustionData.NonFlammable;
            def.atmospheric = new AtmosphericData
            {
                dissipationMultiplier = 0.5f,   // CO2 pesado — se queda en zonas bajas
                gasPermeabilityCoeff  = 1f,
                isFlammable           = false,
                ignitionTemperature   = 0f
            };
            return def;
        }

        public static MaterialDefinition RockGas(MaterialDefinition def)
        {
            def.materialType    = MaterialType.ROCK_GAS;
            def.layer           = MaterialLayer.Gas;
            def.heatTransferCoeff = 0.2f;
            def.blocksVision    = false;

            def.heatingTransition = PhaseTransition.None;
            def.coolingTransition = PhaseTransition.None;

            def.combustion  = new CombustionData
            {
                ignitionTemperature = 40f,
                ashMaterial         = MaterialType.EMPTY,
                smokeMaterial       = MaterialType.CO2
            };
            def.atmospheric = AtmosphericData.Flammable(ignitionTemp: 40f);
            return def;
        }

        public static MaterialDefinition Air(MaterialDefinition def)
        {
            def.materialType    = MaterialType.AIR;
            def.layer           = MaterialLayer.Gas;
            def.heatTransferCoeff = 0.15f;
            def.blocksVision    = false;
            def.blocksMovement  = false;

            def.heatingTransition = PhaseTransition.None;
            def.coolingTransition = PhaseTransition.None;

            def.combustion  = CombustionData.NonFlammable;
            def.atmospheric = new AtmosphericData
            {
                dissipationMultiplier = 2f,     // el aire equilibra rápido
                gasPermeabilityCoeff  = 1f,
                isFlammable           = false,
                ignitionTemperature   = 0f
            };
            return def;
        }
    }
}
