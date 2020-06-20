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

namespace Fps.WorkerConnectors
{
    public class GameLogicWorkerConnector : WorkerConnectorBase
    {
        public bool DisableRenderers = true;
        public Bounds Bounds { get; private set; }


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
            world.GetOrCreateSystem<ServerShootingSystem>();

            // Metrics
            world.GetOrCreateSystem<MetricSendSystem>();

            // Health
            world.GetOrCreateSystem<ServerHealthModifierSystem>();
            world.GetOrCreateSystem<HealthRegenSystem>();
        }

        protected override async Task LoadWorld()
        {
            var worldSize = await GetWorldSize();

            if (worldSize <= 0)
            {
                throw new ArgumentException("Received a world size of 0 or less.");
            }

            LevelInstance = await MapBuilder.GenerateMap(mapTemplate, worldSize, transform, Worker.WorkerType);

            //Create Healthpickup
            var healthPickupCreatingSystem = Worker.World.GetOrCreateSystem<HealthPickupCreatingSystem>();
            healthPickupCreatingSystem.WorldScale = worldSize / 4;
            healthPickupCreatingSystem.CreateHealthPickups();
        }

        public async Task<Bounds> GetWorldBounds()
        {
            var worldSize = await GetWorldSize();
            return new Bounds(Worker.Origin, MapBuilder.GetWorldDimensions(worldSize));
        }
    }
}
