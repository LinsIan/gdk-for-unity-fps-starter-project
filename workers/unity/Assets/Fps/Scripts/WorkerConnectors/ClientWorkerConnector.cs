using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;
using System.Text;
using Fps.Config;
using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using UnityEngine;
//using UnityEngine.AI;

namespace Fps.WorkerConnectors
{
    public class ClientWorkerConnector : WorkerConnectorBase
    {
        private string playerName;
        private bool isReadyToSpawn;
        private bool wantsSpawn;
        private Action<PlayerCreator.CreatePlayer.ReceivedResponse> onPlayerResponse;

        public bool HasConnected => Worker != null;

        public event Action OnLostPlayerEntity;

        public void Start()
        {
            Application.targetFrameRate = 60;
        }
        
        public async void Connect()
        {
            try
            {
                await Connect(GetConnectionHandlerBuilder(), new ForwardingDispatcher());
                await LoadWorld();
                //var navMeshSurface = GetComponent<NavMeshSurface>();
                //navMeshSurface.BuildNavMesh();
                var randomPoint = RandomPoint.Instance;
                randomPoint.workerPosition = transform.position;
                randomPoint.mapScale = worldSize / 4f;
                isReadyToSpawn = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Dispose();
                Destroy(gameObject);
            }
        }

        public void SpawnPlayer(string playerName, Action<PlayerCreator.CreatePlayer.ReceivedResponse> onPlayerResponse)
        {
            this.onPlayerResponse = onPlayerResponse;
            this.playerName = playerName;
            wantsSpawn = true;
        }

        protected virtual string GetAuthPlayerPrefabPath()
        {
            return "Prefabs/UnityClient/Authoritative/Player";
        }

        protected virtual string GetNonAuthPlayerPrefabPath()
        {
            return "Prefabs/UnityClient/NonAuthoritative/Player";
        }

        protected virtual IConnectionHandlerBuilder GetConnectionHandlerBuilder()
        {
            IConnectionFlow connectionFlow;
            var connectionParams = CreateConnectionParameters(WorkerUtils.UnityClient);
            var workerId = CreateNewWorkerId(WorkerUtils.UnityClient);

            if (Application.isEditor)
            {
                connectionFlow = new ReceptionistFlow(workerId);
            }
            else
            {
                var initializer = new CommandLineConnectionFlowInitializer();

                switch (initializer.GetConnectionService())
                {
                    case ConnectionService.Receptionist:
                        connectionFlow = new ReceptionistFlow(workerId, initializer);
                        break;
                    case ConnectionService.Locator:
                        connectionParams.Network.UseExternalIp = true;
                        connectionFlow = new LocatorFlow(initializer);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return new SpatialOSConnectionHandlerBuilder()
                .SetConnectionFlow(connectionFlow)
                .SetConnectionParameters(connectionParams);
        }

        protected override void HandleWorkerConnectionEstablished()
        {
            var world = Worker.World;

            PlayerLifecycleHelper.AddClientSystems(world, autoRequestPlayerCreation: false);
            PlayerLifecycleConfig.MaxPlayerCreationRetries = 0;

            var entityPipeline = new AdvancedEntityPipeline(Worker, GetAuthPlayerPrefabPath(), GetNonAuthPlayerPrefabPath());
            entityPipeline.OnRemovedAuthoritativePlayer += RemovingAuthoritativePlayer;

            // Set the Worker gameObject to the ClientWorker so it can access PlayerCreater reader/writers
            GameObjectCreationHelper.EnableStandardGameObjectCreation(world, entityPipeline, gameObject);

            world.GetOrCreateSystem<UI.RankingSystem>();
        }

        private void RemovingAuthoritativePlayer()
        {
            Debug.LogWarning($"Player entity got removed while still being connected. Disconnecting...");
            OnLostPlayerEntity?.Invoke();
        }

        private void Update()
        {
            if (wantsSpawn && isReadyToSpawn)
            {
                wantsSpawn = false;
                SendRequest();
            }
        }

        private void SendRequest()
        {
            var arg = new PlayerArguments { PlayerName = playerName };
            var serializedArgs = SerializeArguments(arg);
            //var serializedArgs = Encoding.ASCII.GetBytes(playerName);
            Worker.World.GetExistingSystem<SendCreatePlayerRequestSystem>()
                .RequestPlayerCreation(serializedArguments : serializedArgs, onPlayerResponse);
        }

        private byte[] SerializeArguments(object playerCreationArguments)
        {
            using (var memoryStream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, playerCreationArguments);
                return memoryStream.ToArray();
            }
        }
    }
}

[Serializable]
public class PlayerArguments
{
    public string PlayerName = "Player";
}
