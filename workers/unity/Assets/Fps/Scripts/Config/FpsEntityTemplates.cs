using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Fps.Guns;
using Fps.Health;
using Fps.Respawning;
using Fps.SchemaExtensions;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.QueryBasedInterest;
using Pickups;
using UnityEngine;

namespace Fps.Config
{
    public static class FpsEntityTemplates
    {
        public static EntityTemplate Spawner(Coordinates spawnerCoordinates)
        {
            var position = new Position.Snapshot(spawnerCoordinates);
            var metadata = new Metadata.Snapshot("PlayerCreator");

            var template = new EntityTemplate();
            template.AddComponent(position, WorkerUtils.UnityGameLogic);
            template.AddComponent(metadata, WorkerUtils.UnityGameLogic);
            template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new PlayerCreator.Snapshot(), WorkerUtils.UnityGameLogic);

            return template;
        }

        private static T DeserializeArguments<T>(byte[] serializedArguments)
        {
            using (var memoryStream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter();
                memoryStream.Write(serializedArguments, 0, serializedArguments.Length);
                memoryStream.Seek(0, SeekOrigin.Begin);
                return (T) binaryFormatter.Deserialize(memoryStream);
            }
        }

        public static EntityTemplate Player(EntityId entityId, string workerId, byte[] args)
        {
            var client = EntityTemplate.GetWorkerAccessAttribute(workerId);
            var (spawnPosition, spawnYaw, spawnPitch) = SpawnPoints.GetRandomSpawnPoint();

            var serverResponse = new ServerResponse
            {
                Position = spawnPosition.ToVector3Int()
            };

            var rotationUpdate = new RotationUpdate
            {
                Yaw = spawnYaw.ToInt1k(),
                Pitch = spawnPitch.ToInt1k()
            };

            var arg = DeserializeArguments<PlayerArguments>(args);

            var pos = new Position.Snapshot { Coords = Coordinates.FromUnityVector(spawnPosition) };
            var serverMovement = new ServerMovement.Snapshot { Latest = serverResponse };
            var clientMovement = new ClientMovement.Snapshot { Latest = new ClientRequest() };
            var clientRotation = new ClientRotation.Snapshot { Latest = rotationUpdate };
            var shootingComponent = new ShootingComponent.Snapshot();
            var gunComponent = new GunComponent.Snapshot { GunId = PlayerGunSettings.DefaultGunIndex };
            var gunStateComponent = new GunStateComponent.Snapshot { IsAiming = false };
            var healthComponent = new HealthComponent.Snapshot
            {
                Health = PlayerHealthSettings.MaxHealth,
                MaxHealth = PlayerHealthSettings.MaxHealth,
            };

            var healthRegenComponent = new HealthRegenComponent.Snapshot
            {
                CooldownSyncInterval = PlayerHealthSettings.SpatialCooldownSyncInterval,
                DamagedRecently = false,
                RegenAmount = PlayerHealthSettings.RegenAmount,
                RegenCooldownTimer = PlayerHealthSettings.RegenAfterDamageCooldown,
                RegenInterval = PlayerHealthSettings.RegenInterval,
                RegenPauseTime = 0,
            };

            var template = new EntityTemplate();
            template.AddComponent(pos, WorkerUtils.UnityGameLogic);
            template.AddComponent(new Metadata.Snapshot { EntityType = "Player" }, WorkerUtils.UnityGameLogic);
            template.AddComponent(serverMovement, WorkerUtils.UnityGameLogic);
            template.AddComponent(clientMovement, client);
            template.AddComponent(clientRotation, client);
            template.AddComponent(shootingComponent, client);
            template.AddComponent(gunComponent, WorkerUtils.UnityGameLogic);
            template.AddComponent(gunStateComponent, client);
            template.AddComponent(healthComponent, WorkerUtils.UnityGameLogic);
            template.AddComponent(healthRegenComponent, WorkerUtils.UnityGameLogic);
            template.AddComponent(new ScoreComponent.Snapshot { Score = 0, Name = arg.PlayerName }, WorkerUtils.UnityGameLogic);
            template.AddComponent(new LogComponent.Snapshot(), client);

            PlayerLifecycleHelper.AddPlayerLifecycleComponents(template, workerId, WorkerUtils.UnityGameLogic);

            const int serverRadius = 150;
            var clientRadius = workerId.Contains(WorkerUtils.MobileClient) ? 60 : 150;

            // Position, Metadata, OwningWorker and ServerMovement are included in all queries, since these
            // components are required by the GameObject creator.

            // HealthComponent is needed by the LookAtRagdoll script for respawn behaviour.
            // GunComponent is needed by the GunManager script.
            var clientSelfInterest = InterestQuery.Query(Constraint.EntityId(entityId)).FilterResults(new[]
            {
                Position.ComponentId, Metadata.ComponentId, OwningWorker.ComponentId,
                ServerMovement.ComponentId, HealthComponent.ComponentId, GunComponent.ComponentId,
            });

            // ClientRotation is used for rendering other players.
            // GunComponent is required by the GunManager script.
            // GunStateComponent and ShootingComponent are needed for rendering other players' shots.
            var clientRangeInterest = InterestQuery.Query(Constraint.RelativeCylinder(clientRadius)).FilterResults(new[]
            {
                Position.ComponentId, Metadata.ComponentId, OwningWorker.ComponentId,
                ServerMovement.ComponentId, ClientRotation.ComponentId, HealthComponent.ComponentId,
                GunComponent.ComponentId, GunStateComponent.ComponentId, ShootingComponent.ComponentId,
                Pickups.HealthPickup.ComponentId,FishComponent.ComponentId
            });

            //記分板UI需要
            var clientScoreInterest = InterestQuery.Query(Constraint.Component(ScoreComponent.ComponentId)).FilterResults(new[]
            {
                ScoreComponent.ComponentId
            });

            // ClientMovement is used by the ServerMovementDriver script.
            // ShootingComponent is used by the ServerShootingSystem.
            var serverSelfInterest = InterestQuery.Query(Constraint.EntityId(entityId)).FilterResults(new[]
            {
                ClientMovement.ComponentId, ShootingComponent.ComponentId
            });

            // ClientRotation is used for driving player proxies.
            // HealthComponent is required by the VisiblityAndCollision script.
            // ShootingComponent is used by the ServerShootingSystem.
            var serverRangeInterest = InterestQuery.Query(Constraint.RelativeCylinder(serverRadius)).FilterResults(new[]
            {
                Position.ComponentId, Metadata.ComponentId, OwningWorker.ComponentId,
                ServerMovement.ComponentId, ClientRotation.ComponentId, HealthComponent.ComponentId,
                ShootingComponent.ComponentId,FishComponent.ComponentId
            });

            var interest = InterestTemplate.Create()
                .AddQueries<ClientMovement.Component>(clientSelfInterest, clientRangeInterest, clientScoreInterest)
                .AddQueries<ServerMovement.Component>(serverSelfInterest, serverRangeInterest);

            template.AddComponent(interest.ToSnapshot());
            template.SetReadAccess(WorkerUtils.UnityClient, WorkerUtils.UnityGameLogic, WorkerUtils.MobileClient);

            return template;
        }

        public static EntityTemplate HealthPickup(Vector3 posistion, uint healthValue)
        {
            // Create a HealthPickup component snapshot which is initially active and grants "heathValue" on pickup.
            var healthPickupComponent = new Pickups.HealthPickup.Snapshot(true, healthValue);

            var entityTemplate = new EntityTemplate();

            //設置該Template擁有的Component以及有其Write Access的Worker Type
            //Position、Metadata、Persistence都是SpatialOS會用到的資訊
            entityTemplate.AddComponent(new Position.Snapshot(Coordinates.FromUnityVector(posistion)), WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(new Metadata.Snapshot("HealthPickup"), WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(healthPickupComponent, WorkerUtils.UnityGameLogic);

            //設定擁有Read Access的Worker
            entityTemplate.SetReadAccess(WorkerUtils.UnityGameLogic, WorkerUtils.UnityClient);

            //也可以透過指定ID的方式來給予Client端權限(因為不能一個角色所有Client端都有改寫權限)
            //EX: template.AddComponent(clientMovement, EntityTemplate.GetWorkerAccessAttribute(workerId));

            //設置單一興趣條件，條件：半徑25內的所有Entity，對其FilterResults中的這些Component感興趣
            var query = InterestQuery.Query(Constraint.RelativeCylinder(radius: 25)).FilterResults(new[]
            {
                Position.ComponentId, Metadata.ComponentId, OwningWorker.ComponentId, ServerMovement.ComponentId,
                ClientRotation.ComponentId, HealthComponent.ComponentId, ShootingComponent.ComponentId
            });

            //HealthPickup Component有寫入權的Worker，在其半徑25內的所有Entity，對其FilterResults中的這些Component感興趣
            var interest = InterestTemplate.Create().AddQueries<Pickups.HealthPickup.Component>(query);
            entityTemplate.AddComponent(interest.ToSnapshot());


            //多重條件的寫法，多重條件，在半徑25內且有Metadata Component的Entity，對其Position感興趣
            //var query2 = InterestQuery.Query(Constraint.All(Constraint.RelativeCylinder(radius: 25),Constraint.Component(Metadata.ComponentId))).FilterResults(new[]
            //{
            //     Position.ComponentId
            //});

            return entityTemplate;
        }

        public static EntityTemplate NormalFish()
        {
            //資料和讀寫權限設定
            var spawnPosition = RandomPoint.Instance.RandomNavmeshLocation();
            spawnPosition.y += FishSettings.FishOffsetYDic[EFishType.NORMAL];
            var rotationUpdate = new RotationUpdate
            {
                Yaw = 0f.ToInt1k(),
                Pitch = 0f.ToInt1k()
            };

            float MaxHp = FishSettings.FishHealthDic[EFishType.NORMAL];

            var entityTemplate = new EntityTemplate();
            entityTemplate.AddComponent(new Position.Snapshot(Coordinates.FromUnityVector(spawnPosition)), WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(new Metadata.Snapshot("NormalFish"), WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(new ClientRotation.Snapshot(rotationUpdate), WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(new HealthComponent.Snapshot(MaxHp, MaxHp), WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(new FishComponent.Snapshot(EFishType.NORMAL, EFishState.SWIM, spawnPosition.ToVector3Int()), WorkerUtils.UnityGameLogic);

            entityTemplate.SetReadAccess(WorkerUtils.UnityGameLogic, WorkerUtils.UnityClient);

            //興趣範圍設定
            const int serverRadius = 150;
            var query = InterestQuery.Query(Constraint.RelativeCylinder(serverRadius)).FilterResults(new[]
            {
                Position.ComponentId, Metadata.ComponentId, OwningWorker.ComponentId,
                ServerMovement.ComponentId, ClientRotation.ComponentId, HealthComponent.ComponentId,
                GunComponent.ComponentId, GunStateComponent.ComponentId, ShootingComponent.ComponentId,
                Pickups.HealthPickup.ComponentId,FishComponent.ComponentId,
            });

            var interest = InterestTemplate.Create().AddQueries<FishComponent.Component>(query);
            entityTemplate.AddComponent(interest.ToSnapshot());

            return entityTemplate;
        }

        public static EntityTemplate SpeedFish()
        {
            //資料和讀寫權限設定
            var spawnPosition = RandomPoint.Instance.RandomNavmeshLocation();
            spawnPosition.y += FishSettings.FishOffsetYDic[EFishType.SPEED];
            var rotationUpdate = new RotationUpdate
            {
                Yaw = 0f.ToInt1k(),
                Pitch = 0f.ToInt1k()
            };

            float MaxHp = FishSettings.FishHealthDic[EFishType.SPEED];

            var entityTemplate = new EntityTemplate();
            entityTemplate.AddComponent(new Position.Snapshot(Coordinates.FromUnityVector(spawnPosition)), WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(new Metadata.Snapshot("SpeedFish"), WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(new ClientRotation.Snapshot(rotationUpdate), WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(new HealthComponent.Snapshot(MaxHp, MaxHp), WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(new FishComponent.Snapshot(EFishType.SPEED, EFishState.SWIM, spawnPosition.ToVector3Int()), WorkerUtils.UnityGameLogic);

            entityTemplate.SetReadAccess(WorkerUtils.UnityGameLogic, WorkerUtils.UnityClient);

            //興趣範圍設定
            const int serverRadius = 150;
            var query = InterestQuery.Query(Constraint.RelativeCylinder(serverRadius)).FilterResults(new[]
            {
                Position.ComponentId, Metadata.ComponentId, OwningWorker.ComponentId,
                ServerMovement.ComponentId, ClientRotation.ComponentId, HealthComponent.ComponentId,
                GunComponent.ComponentId, GunStateComponent.ComponentId, ShootingComponent.ComponentId,
                Pickups.HealthPickup.ComponentId,FishComponent.ComponentId,
            });

            var interest = InterestTemplate.Create().AddQueries<Position.Component>(query);
            entityTemplate.AddComponent(interest.ToSnapshot());

            return entityTemplate;
        }

        public static EntityTemplate Octopus()
        {
            //資料和讀寫權限設定
            var spawnPosition = RandomPoint.Instance.RandomNavmeshLocation();
            spawnPosition.y += FishSettings.FishOffsetYDic[EFishType.OCTOPUS];
            var rotationUpdate = new RotationUpdate
            {
                Yaw = 0f.ToInt1k(),
                Pitch = 0f.ToInt1k()
            };
            spawnPosition.y += 3;

            float MaxHp = FishSettings.FishHealthDic[EFishType.OCTOPUS];

            var entityTemplate = new EntityTemplate();
            entityTemplate.AddComponent(new Position.Snapshot(Coordinates.FromUnityVector(spawnPosition)), WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(new Metadata.Snapshot("Octopus"), WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(new ClientRotation.Snapshot(rotationUpdate), WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(new HealthComponent.Snapshot(MaxHp, MaxHp), WorkerUtils.UnityGameLogic);
            entityTemplate.AddComponent(new FishComponent.Snapshot(EFishType.OCTOPUS, EFishState.SWIM, spawnPosition.ToVector3Int()), WorkerUtils.UnityGameLogic);

            entityTemplate.SetReadAccess(WorkerUtils.UnityGameLogic, WorkerUtils.UnityClient);

            //興趣範圍設定
            const int serverRadius = 150;
            var query = InterestQuery.Query(Constraint.RelativeCylinder(serverRadius)).FilterResults(new[]
            {
                Position.ComponentId, Metadata.ComponentId, OwningWorker.ComponentId,
                ServerMovement.ComponentId, ClientRotation.ComponentId, HealthComponent.ComponentId,
                GunComponent.ComponentId, GunStateComponent.ComponentId, ShootingComponent.ComponentId,
                Pickups.HealthPickup.ComponentId,FishComponent.ComponentId,
            });

            var interest = InterestTemplate.Create().AddQueries<Position.Component>(query);
            entityTemplate.AddComponent(interest.ToSnapshot());

            return entityTemplate;
        }
    }
}
