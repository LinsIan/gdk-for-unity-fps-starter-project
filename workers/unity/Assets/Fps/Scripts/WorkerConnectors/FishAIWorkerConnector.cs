using Fps.Config;
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
using Fps.Metrics;


namespace Fps.WorkerConnectors
{
    public class FishAIWorkerConnector : WorkerConnectorBase
    {
        public bool DisableRenderers = true;
        public Bounds Bounds { get; private set; }

        NavMeshSurface navMeshSurface;

        protected async void Start()
        {
            Application.targetFrameRate = 30;

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

            //Create fish
            var randomPoint = RandomPoint.Instance;
            var healthPickupCreatingSystem = Worker.World.GetOrCreateSystem<HealthPickupCreatingSystem>();
            healthPickupCreatingSystem.WorldScale = worldSize / 4;
            randomPoint.workerPosition = transform.position;
            randomPoint.mapScale = worldSize / 4f;
            healthPickupCreatingSystem.CreateFish();
        }

        private IConnectionHandlerBuilder GetConnectionHandlerBuilder()
        {
            IConnectionFlow connectionFlow;
            ConnectionParameters connectionParameters;
            
            var workerId = CreateNewWorkerId(WorkerUtils.FishAI);

            if (Application.isEditor)
            {
                connectionFlow = new ReceptionistFlow(workerId);
                connectionParameters = CreateConnectionParameters(WorkerUtils.FishAI);
            }
            else
            {
                connectionFlow = new ReceptionistFlow(workerId, new CommandLineConnectionFlowInitializer());
                connectionParameters = CreateConnectionParameters(WorkerUtils.FishAI,
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

            // Metrics
            world.GetOrCreateSystem<MetricSendSystem>();

            // fish
            var fishSystem = world.GetOrCreateSystem<HealthPickupCreatingSystem>();
            fishSystem.worker = Worker;
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
