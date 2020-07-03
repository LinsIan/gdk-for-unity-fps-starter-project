using Improbable.Gdk.Core;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Fps
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class ScoreModifierSystem : ComponentSystem
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
            var requests = commandSystem.GetRequests<ScoreComponent.ModifyScore.ReceivedRequest>();
            if(requests.Count == 0)
            {
                return;
            }

            var ScoreComponentData = GetComponentDataFromEntity<ScoreComponent.Component>();
            //具有讀寫權的
            var scoreDic = new Dictionary<EntityId, ScoreComponent.Component>();

            EntityQuery query = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadWrite<ScoreComponent.Component>(),
                ComponentType.ReadOnly<ScoreComponent.HasAuthority>()
            );

            Entities.With(query).ForEach((ref SpatialEntityId entityId, ref ScoreComponent.Component score) =>
            {
                scoreDic.Add(entityId.EntityId, score);
            });

            for (int i = 0; i < requests.Count; i++)
            {
                ref readonly var request = ref requests[i];
                var entityId = request.EntityId;

                if(!scoreDic.ContainsKey(entityId))
                {
                    continue;
                }

                if (!workerSystem.TryGetEntity(entityId, out var entity))
                {
                    continue;
                }

                var score = ScoreComponentData[entity];
                var modifier = request.Payload;

                score.Score += modifier.Amount;

                ScoreComponentData[entity] = score;
            }
        }
    }
}
