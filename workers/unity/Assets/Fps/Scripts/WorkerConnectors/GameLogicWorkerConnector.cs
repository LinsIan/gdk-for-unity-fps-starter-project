using Fps.Config;
using Fps.Guns;
using Fps.Health;
using Fps.Metrics;
using Fps.HealthPickup;
using Fps.WorldTiles;
using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Worker.CInterop;
using UnityEngine;
using System;
using System.Threading.Tasks;
using UnityEngine.AI;

namespace Fps.WorkerConnectors
{
    public class GameLogicWorkerConnector : WorkerConnectorBase
    {
        public bool DisableRenderers = true;
        public Bounds Bounds { get; private set; }

        private int worldSize;
        NavMeshSurface navMeshSurface;

        protected async void Start()
        {
            Application.targetFrameRate = 60;

            await Connect(GetConnectionHandlerBuilder(), new ForwardingDispatcher());
            await LoadWorld();

            Bounds = await GetWorldBounds();

            if (DisableRenderers)
            {
                foreach (var childRenderer in LevelInstance.GetComponentsInChildren<Renderer>())
                {
                    childRenderer.enabled = false;
                }
            }
            //動態生成NavMesh
            navMeshSurface = GetComponent<NavMeshSurface>();
            navMeshSurface.BuildNavMesh();

            //Create Healthpickup & fish
            var healthPickupCreatingSystem = Worker.World.GetOrCreateSystem<HealthPickupCreatingSystem>();
            healthPickupCreatingSystem.WorldScale = worldSize / 4;
            RandomPoint.Instance.mapScale = healthPickupCreatingSystem.WorldScale;
            healthPickupCreatingSystem.CreateHealthPickups();

            RandomPoint.Instance.mapPosition = navMeshSurface.navMeshData.position;
            RandomPoint.Instance.workerPosition = transform.position;
        }

        private IConnectionHandlerBuilder GetConnectionHandlerBuilder()
        {
            IConnectionFlow connectionFlow;
            ConnectionParameters connectionParameters;

            var workerId = CreateNewWorkerId(WorkerUtils.UnityGameLogic);

            if (Application.isEditor)
            {
                connectionFlow = new ReceptionistFlow(workerId);
                connectionParameters = CreateConnectionParameters(WorkerUtils.UnityGameLogic);
            }
            else
            {
                connectionFlow = new ReceptionistFlow(workerId, new CommandLineConnectionFlowInitializer());
                connectionParameters = CreateConnectionParameters(WorkerUtils.UnityGameLogic,
                    new CommandLineConnectionParameterInitializer());
            }

            return new SpatialOSConnectionHandlerBuilder()
                .SetConnectionFlow(connectionFlow)
                .SetConnectionParameters(connectionParameters);
        }

        protected override void HandleWorkerConnectionEstablished()
        {
            var world = Worker.World;

            PlayerLifecycleHelper.AddServerSystems(world);
            GameObjectCreationHelper.EnableStandardGameObjectCreation(world);

            // Shooting
            var shootingsystem = world.GetOrCreateSystem<ServerShootingSystem>();

            // Metrics
            world.GetOrCreateSystem<MetricSendSystem>();

            // Health
            world.GetOrCreateSystem<ServerHealthModifierSystem>();
            world.GetOrCreateSystem<HealthRegenSystem>();

            // Score
            world.GetOrCreateSystem<ScoreModifierSystem>();
        }

        protected override async Task LoadWorld()
        {
            worldSize = await GetWorldSize();

            if (worldSize <= 0)
            {
                throw new ArgumentException("Received a world size of 0 or less.");
            }

            LevelInstance = await MapBuilder.GenerateMap(mapTemplate, worldSize, transform, Worker.WorkerType);
        }

        public async Task<Bounds> GetWorldBounds()
        {   
            var worldSize = await GetWorldSize();
            return new Bounds(Worker.Origin, MapBuilder.GetWorldDimensions(worldSize));
        }
    }
}
