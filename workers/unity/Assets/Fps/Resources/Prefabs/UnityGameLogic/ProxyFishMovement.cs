using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
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
#pragma warning disable 649
        [Require] private PositionReader position;
        [Require] private ClientRotationReader rotation;
        [Require] private FishComponentReader fishComponentReader;

        [Require] private LogComponentCommandSender commandSender;
#pragma warning disable 649
        
        private NavMeshAgent agent;
        
        private void OnEnable()
        {
            position.OnUpdate += OnPositionUpdate;
            rotation.OnUpdate += OnRotationUpdate;
            fishComponentReader.OnDestinationUpdate += OnDestinationUpdate;
            agent = GetComponent<NavMeshAgent>();
            if(fishComponentReader.Authority != Authority.Authoritative)
            {
                agent.enabled = false;
            }
        }

        private void OnPositionUpdate(Position.Update update)
        {
            if (position.Authority == Authority.Authoritative)
            {
                return;
            }
            Vector3 pos = position.Data.Coords.ToUnityVector();
            transform.position = pos;
            agent.Warp(pos);
        }
        
        private void OnRotationUpdate(ClientRotation.Update update)
        {
            if (position.Authority == Authority.Authoritative)
            {
                return;
            }
            float pitch = rotation.Data.Latest.Pitch.ToFloat1k();
            float roll = rotation.Data.Latest.Roll.ToFloat1k();
            float yaw = rotation.Data.Latest.Yaw.ToFloat1k();
            transform.rotation = Quaternion.Euler(pitch, yaw, roll);
        }

        private void OnDestinationUpdate(Vector3Int destination)
        {
            if (position.Authority == Authority.Authoritative)
            {
                return;
            }
            Vector3 deviation = transform.position - position.Data.Coords.ToUnityVector();
            Vector3 pos = position.Data.Coords.ToUnityVector();
            transform.position = pos;
            agent.Warp(pos);
            agent.SetDestination(destination.ToVector3());
        }

    }

}

