using Unity.Entities;
using UnityEngine;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Fps.Config;

namespace Fps.HealthPickup
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class HealthPickupCreatingSystem : ComponentSystem
    {
        private const int AreaCol = 9;
        private const int AreaWid = 9;
        private const int HealthAmount = 50;
        private const float CreationInterval = 36.0f;
        private CommandSystem commandSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            commandSystem = World.GetExistingSystem<CommandSystem>();

            CreateHealthPickups();
        }

        protected override void OnUpdate() {}

        private void CreateHealthPickups()
        {
            //Create HealthPickUps
            var healthPickup = FpsEntityTemplates.HealthPickup(new Vector3(5, 0, 0), 100);
            var request = new WorldCommands.CreateEntity.Request(healthPickup);

            commandSystem.SendCommand(request);
        }
    }
}

