using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Fps.Config;
using Fps.SchemaExtensions;
using Improbable;
using Improbable.Worker.CInterop;


namespace Fps
{
    [WorkerType(WorkerUtils.UnityGameLogic)]
    public class ProxyFishMovement : MonoBehaviour
    {
        [Require] private PositionReader position;
        [Require] private ClientRotationReader rotation;

        private void OnEnable()
        {
            position.OnUpdate += OnPositionUpdate;
            rotation.OnUpdate += OnRotationUpdate;
        }

        private void OnPositionUpdate(Position.Update update)
        {
            if (position.Authority == Authority.Authoritative)
            {
                return;
            }
            Vector3 pos = position.Data.Coords.ToUnityVector();
            pos.y = transform.position.y;
            transform.position = pos;
        }

        private void OnRotationUpdate(ClientRotation.Update update)
        {
            if (rotation.Authority == Authority.Authoritative)
            {
                return;
            }
            float pitch = rotation.Data.Latest.Pitch.ToFloat1k();
            float roll = rotation.Data.Latest.Roll.ToFloat1k();
            float yaw = rotation.Data.Latest.Yaw.ToFloat1k();
            transform.rotation = Quaternion.Euler(pitch, yaw, roll);
        }

    }

}

