using Unity.Entities;
using UnityEngine;
using System.Collections;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Fps.Config;

namespace Fps.HealthPickup
{
    public class HealthPickupCreatingSystem : ComponentSystem
    {
        public int WorldScale = 6;
        private int AreaCol = 9;
        private int AreaWid = 9;
        private uint HealthAmount = 50;
        private float CreationInterval = 36.0f;
        private float HalfLength = 144.0f;
        private CommandSystem commandSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            commandSystem = World.GetExistingSystem<CommandSystem>();
        }

        protected override void OnUpdate() {}
        
        public void CreateHealthPickups()
        {
            //Vector3 StartPoint = new Vector3();
            //for(int z = 0; z <= (AreaCol-1) * WorldScale; ++z)
            //{
            //    StartPoint.z = HalfLength * WorldScale - z * CreationInterval;
            //    for (int x = 0; x <= (AreaWid-1) * WorldScale; ++x)
            //    {
            //        StartPoint.x = -HalfLength * WorldScale + x*CreationInterval;
            //        var healthPickup = FpsEntityTemplates.HealthPickup(StartPoint, HealthAmount);
            //        var request = new WorldCommands.CreateEntity.Request(healthPickup);
            //        commandSystem.SendCommand(request);
            //    }
            //}

            //fish測試
            for(int i=0; i<20;++i)
            {
               var fish = FpsEntityTemplates.NormalFish();
               var fishrequest = new WorldCommands.CreateEntity.Request(fish);
               commandSystem.SendCommand(fishrequest);
            }

            for(int i=0; i<9; ++i)
            {
                var fish = FpsEntityTemplates.SpeedFish();
                var fishrequest = new WorldCommands.CreateEntity.Request(fish);
                commandSystem.SendCommand(fishrequest);
            }

            for(int i=0; i<1; ++i)
            {
                var octopus = FpsEntityTemplates.Octopus();
                var octopusrequest = new WorldCommands.CreateEntity.Request(octopus);
                commandSystem.SendCommand(octopusrequest);
            }
            

        }
    }
}

