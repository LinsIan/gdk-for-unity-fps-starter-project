using Fps.SchemaExtensions;
using Improbable.Gdk.Subscriptions;
using Improbable.Worker.CInterop;
using Improbable;
using UnityEngine;

namespace Fps.Movement
{
    public class ProxyMovementDriver : GroundCheckingDriver
    {
#pragma warning disable 649
        [Require] private ServerMovementReader server;
        [Require] private ClientRotationReader client;
#pragma warning restore 649

        [SerializeField]
        private RotationConstraints rotationConstraints = new RotationConstraints
        {
            XAxisRotation = true,
            YAxisRotation = true,
            ZAxisRotation = true
        };

        private LinkedEntityComponent LinkedEntityComponent;
        private Vector3 origin;

        //Rotation Variables
        private float timeLeftToRotate;
        private float lastFullTime;
        private Quaternion source;
        private Quaternion target;
        private bool hasRotationLeft;

        private void OnEnable()
        {
            LinkedEntityComponent = GetComponent<LinkedEntityComponent>();
            origin = LinkedEntityComponent.Worker.Origin;
            origin.y = transform.position.y - server.Data.Latest.Position.ToVector3().y;
            
            server.OnLatestUpdate += OnServerUpdate;
            client.OnLatestUpdate += OnClientUpdate;

            server.OnAuthorityUpdate += OnAuthorityUpdate;

            OnClientUpdate(client.Data.Latest);
            OnServerUpdate(server.Data.Latest);
            if(server.Authority != Authority.Authoritative)
            {
                Debug.Log(gameObject.name + ":" + origin);
            }
        }

        private void OnClientUpdate(RotationUpdate rotation)
        {
            var x = rotationConstraints.XAxisRotation ? rotation.Pitch.ToFloat1k() : 0;
            var y = rotationConstraints.YAxisRotation ? rotation.Yaw.ToFloat1k() : 0;
            var z = rotationConstraints.ZAxisRotation ? rotation.Roll.ToFloat1k() : 0;

            UpdateRotation(Quaternion.Euler(x, y, z), rotation.TimeDelta);
        }

        private void OnServerUpdate(ServerResponse movement)
        {
            if (server.Authority == Authority.Authoritative)
            {
                return;
            }
            Interpolate(movement.Position.ToVector3() + origin, movement.TimeDelta);

            if (Vector3.Distance(transform.position, movement.Position.ToVector3() + origin) > 1.5f)
            {
                transform.position = movement.Position.ToVector3() + origin;
            }

        }

        private void OnAuthorityUpdate(Authority authority)
        {
            if(authority == Authority.Authoritative)
            {
                //Interpolate(server.Data.Latest.Position.ToVector3() + origin, server.Data.Latest.TimeDelta);
                transform.position = server.Data.Latest.Position.ToVector3() + origin;
            }
        }

        private void UpdateRotation(Quaternion targetQuaternion, float timeDelta)
        {
            hasRotationLeft = true;
            lastFullTime = timeLeftToRotate = timeDelta;
            target = targetQuaternion;
            source = transform.rotation;
        }

        protected override void Update()
        {
            base.Update();
            if (!hasRotationLeft)
            {
                return;
            }

            if (Time.deltaTime < timeLeftToRotate)
            {
                transform.rotation =
                    Quaternion.Lerp(source, target, 1 - timeLeftToRotate / lastFullTime);
                timeLeftToRotate -= Time.deltaTime;
            }
            else
            {
                transform.rotation = target;
                hasRotationLeft = false;
            }
        }
    }
}
