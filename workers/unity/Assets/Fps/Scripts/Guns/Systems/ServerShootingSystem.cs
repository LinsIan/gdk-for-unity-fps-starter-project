using Improbable.Gdk.Core;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Fps.Guns
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class ServerShootingSystem : ComponentSystem
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
            var events = componentUpdateSystem.GetEventsReceived<ShootingComponent.Shots.Event>();
            if (events.Count == 0)
            {
                return;
            }
            //觀測的到的
            var gunDataForEntity = GetComponentDataFromEntity<GunComponent.Component>();
            //具有讀寫權的
            var gunDic = new Dictionary<EntityId, GunComponent.Component>();

            EntityQuery query = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadWrite<GunComponent.Component>(),
                ComponentType.ReadOnly<GunComponent.HasAuthority>()
            );

            Entities.With(query).ForEach((ref SpatialEntityId entityId, ref GunComponent.Component gun) =>
            {
                gunDic.Add(entityId.EntityId, gun);
            });

            for (var i = 0; i < events.Count; ++i)
            {
                ref readonly var shotEvent = ref events[i];
                
                var shotInfo = shotEvent.Event.Payload;
                if (!shotInfo.HitSomething || !shotInfo.HitEntityId.IsValid() || !shotInfo.ShooterEntityId.IsValid())
                {
                    continue;
                }

                var shooterSpatialID = shotInfo.ShooterEntityId;

                if(!gunDic.ContainsKey(shooterSpatialID))
                {
                    continue;
                }

                if (!workerSystem.TryGetEntity(shooterSpatialID, out var shooterEntity))
                {
                    continue;
                }
                //if (!gunDataForEntity.Exists(shooterEntity))
                //{
                //    continue;
                //}

                var gunComponent = gunDataForEntity[shooterEntity];

                var damage = GunDictionary.Get(gunComponent.GunId).ShotDamage;

                var modifyHealthRequest = new HealthComponent.ModifyHealth.Request(
                    shotInfo.HitEntityId,
                    new HealthModifier
                    {
                        Amount = -damage,
                        Origin = shotInfo.HitOrigin,
                        AppliedLocation = shotInfo.HitLocation,
                        Owner = shotInfo.HitEntityId,
                        ModifierId = shotInfo.ShooterEntityId,
                    }
                );
                commandSystem.SendCommand(modifyHealthRequest);
            }
        }
    }
}
