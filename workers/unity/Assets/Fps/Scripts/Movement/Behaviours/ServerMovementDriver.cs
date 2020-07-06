using Fps.SchemaExtensions;
using Improbable;
using Improbable.Gdk.Subscriptions;
using Improbable.Worker.CInterop;
using UnityEngine;

namespace Fps.Movement
{
    public class ServerMovementDriver : CharacterControllerMotor
    {
#pragma warning disable 649
        [Require] private ServerMovementWriter server;
        [Require] private ClientMovementReader client;
        [Require] private PositionWriter spatialPosition;
#pragma warning restore 649
        
        [SerializeField] private float spatialPositionUpdateHz = 1.0f;
        [SerializeField, HideInInspector] private float spatialPositionUpdateDelta;

        private Vector3 lastPosition;
        private Vector3 origin;
        private float lastSpatialPositionTime;

        // Cache the update delta values.
        private void OnValidate()
        {
            if (spatialPositionUpdateHz > 0.0f)
            {
                spatialPositionUpdateDelta = 1.0f / spatialPositionUpdateHz;
            }
            else
            {
                spatialPositionUpdateDelta = 1.0f;
                Debug.LogError("The Spatial Position Update Hz must be greater than 0.");
            }
        }

        private void OnEnable()
        {
            var linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            origin = linkedEntityComponent.Worker.Origin;

            client.OnLatestUpdate += OnClientUpdate;
            //server.OnAuthorityUpdate += OnMovementAuthorityUpdate;
        }

        private void OnClientUpdate(ClientRequest request)
        {
            // Move the player by the given delta.
            Move(request.Movement.ToVector3());
            
            var positionNoOffset = transform.position - origin;

            // Send the update using the new position.
            var response = new ServerResponse
            {
                Position = positionNoOffset.ToVector3Int(),
                IncludesJump = request.IncludesJump,
                Timestamp = request.Timestamp,
                TimeDelta = request.TimeDelta
            };
            var update = new ServerMovement.Update { Latest = response };
            server.SendUpdate(update);
            var positionUpdate = new Position.Update { Coords = Coordinates.FromUnityVector(positionNoOffset) };
            spatialPosition.SendUpdate(positionUpdate);
        }

        //確認失去寫入權
        //private void OnMovementAuthorityUpdate(Authority authority)
        //{
        //    if(authority == Authority.AuthorityLossImminent)
        //    {
        //        GetComponent<PlayerHealthComponent>().SendMessage("失去寫入權");
        //        server.AcknowledgeAuthorityLoss();
        //    }
        //}
    }
}
