using Improbable.Gdk.Core;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Fps.Health
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class ServerHealthModifierSystem : ComponentSystem
    {

        private WorkerSystem workerSystem;
        private CommandSystem commandSystem;
        private ComponentUpdateSystem componentUpdateSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            workerSystem = World.GetExistingSystem<WorkerSystem>();
            commandSystem = World.GetExistingSystem<CommandSystem>();
            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();

        }

        protected override void OnUpdate()
        {
            //抓有沒有收到補血的指令
            var requests = commandSystem.GetRequests<HealthComponent.ModifyHealth.ReceivedRequest>();
            if (requests.Count == 0)
            {
                return;
            }

            var healthComponentData = GetComponentDataFromEntity<HealthComponent.Component>();
            //具有讀寫權的entity
            var healthDic = new Dictionary<EntityId, HealthComponent.Component>();

            EntityQuery query = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadWrite<HealthComponent.Component>(),
                ComponentType.ReadOnly<HealthComponent.HasAuthority>()
            );

            Entities.With(query).ForEach((ref SpatialEntityId entityId, ref HealthComponent.Component health) =>
            {
                healthDic.Add(entityId.EntityId, health);
            });

            for (var i = 0; i < requests.Count; i++)
            {
                ref readonly var request = ref requests[i];
                var entityId = request.EntityId;

                if(!healthDic.ContainsKey(entityId))
                {
                    continue;
                }

                if (!workerSystem.TryGetEntity(entityId, out var entity))
                {
                    continue;
                }

                var health = healthComponentData[entity];

                var modifier = request.Payload;

                // Skip if already dead and still getting damage
                if (health.Health <= 0 && modifier.Amount < 0)
                {
                    continue;
                }

                var healthModifiedInfo = new HealthModifiedInfo
                {
                    Modifier = modifier,
                    HealthBefore = health.Health
                };

                health.Health = Mathf.Clamp(health.Health + modifier.Amount, 0, health.MaxHealth);
                healthModifiedInfo.HealthAfter = health.Health;

                if (health.Health <= 0)
                {
                    healthModifiedInfo.Died = true;
                }

                componentUpdateSystem.SendEvent(new HealthComponent.HealthModified.Event(healthModifiedInfo), entityId);
                healthComponentData[entity] = health;
            }
        }
    }
}
