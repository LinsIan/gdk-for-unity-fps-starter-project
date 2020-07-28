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
        private WorkerSystem workerSystem;
        private CommandSystem commandSystem;
        private ComponentUpdateSystem componentUpdateSystem;


        public WorkerInWorld worker;
        public int fishCount = 0;
        public int fishNum = 0;

        protected override void OnCreate()
        {
            base.OnCreate();
            workerSystem = World.GetExistingSystem<WorkerSystem>();
            commandSystem = World.GetExistingSystem<CommandSystem>();
            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
        }

        protected override void OnUpdate()
        {
            /*if (RandomPoint.Instance.mapScale == 0) return;

            var NumFlag = worker.GetWorkerFlag("fish_num_per_worker");
            var Num = System.Convert.ToInt32(NumFlag);
            Debug.Log("Num:"+Num);
            if(fishNum != Num)
            {
                for (; fishNum < Num; ++fishNum)
                {
                    var fish = FpsEntityTemplates.NormalFish();
                    var fishrequest = new WorldCommands.CreateEntity.Request(fish);
                    commandSystem.SendCommand(fishrequest);
                }
             }*/
        }
        
        public void CreateHealthPickupsAndFish()
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
            
            for(int i=0; i<31;++i)
            {
               var fish = FpsEntityTemplates.NormalFish();
               var fishrequest = new WorldCommands.CreateEntity.Request(fish);
               commandSystem.SendCommand(fishrequest);
            }

            for(int i=0; i<22; ++i)
            {
                var fish = FpsEntityTemplates.SpeedFish();
                var fishrequest = new WorldCommands.CreateEntity.Request(fish);
                commandSystem.SendCommand(fishrequest);
            }

            for(int i=0; i<2; ++i)
            {
                var octopus = FpsEntityTemplates.Octopus();
                var octopusrequest = new WorldCommands.CreateEntity.Request(octopus);
                commandSystem.SendCommand(octopusrequest);
            }
            
            
        }
    }
}
