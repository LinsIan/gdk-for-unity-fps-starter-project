using Fps.Config;
using Fps.Guns;
using Fps.Health;
using Fps.Metrics;
using Fps.WorldTiles;
using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Worker.CInterop;
using UnityEngine;
using System;
using System.Threading.Tasks;

namespace Fps.WorkerConnectors
{
    public class GameLogicWorkerConnector : WorkerConnectorBase
    {
        public bool DisableRenderers = true;
        public Bounds Bounds { get; private set; }

        protected async void Start()
        {
            Application.targetFrameRate = 30;
            
            await Connect(GetConnectionHandlerBuilder(), new ForwardingDispatcher());
            await LoadWorld();

            Bounds = await GetWorldBounds();

            //動態生成NavMesh
            var randomPoint = RandomPoint.Instance;
            randomPoint.workerPosition = transform.position;
            randomPoint.mapScale = worldSize / 4f;

            if (DisableRenderers)
            {
                foreach (var childRenderer in LevelInstance.GetComponentsInChildren<Renderer>())
                {
                    childRenderer.enabled = false;
                }
            }
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
