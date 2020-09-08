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
    [WorkerType(WorkerUtils.FishAI)]
    public class ProxyFishMovement : MonoBehaviour
    {
#pragma warning disable 649
        [Require] private PositionReader position;
        [Require] private ClientRotationReader rotation;
        [Require] private FishComponentReader fishComponentReader;
#pragma warning disable 649

        public Authority m_Authority;

        private NavMeshAgent agent;
        private float offsetY = 0;
        private bool IsDead;

        private void OnEnable()
        {
            position.OnUpdate += OnPositionUpdate;
            rotation.OnUpdate += OnRotationUpdate;
            fishComponentReader.OnDestinationUpdate += OnDestinationUpdate;
            fishComponentReader.OnAuthorityUpdate += OnAuthUpdate;
            fishComponentReader.OnStateUpdate += OnStateUpdate;
            agent = GetComponent<NavMeshAgent>();
            if(fishComponentReader.Authority != Authority.Authoritative)
            {
                agent.isStopped = true;
            }
            offsetY = transform.position.y - position.Data.Coords.ToUnityVector().y;
            IsDead = false;
            m_Authority = fishComponentReader.Authority;

            foreach (var childRenderer in gameObject.GetComponentsInChildren<Renderer>())
            {
                childRenderer.enabled = false;
            }
        }

        private void OnAuthUpdate(Authority authority)
        {
            m_Authority = authority;
            Debug.Log(authority);
            if(authority == Authority.Authoritative ) //位置校正
            {
                /*var pos = transform.position + transform.forward * Time.deltaTime * agent.speed * 3;
                agent.Warp(pos);
                transform.position = pos;
                agent.isStopped = false;*/
                Vector3 pos = position.Data.Coords.ToUnityVector();
                pos.y += offsetY;
                agent.Warp(pos);
                transform.position = pos;
                IsDead = false;
            }
        }

        private void OnPositionUpdate(Position.Update update)
        {
            if (position.Authority == Authority.Authoritative)
            {
                return;
            }

            Vector3 pos = position.Data.Coords.ToUnityVector();
            pos.y += offsetY;
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
            //Vector3 pos = position.Data.Coords.ToUnityVector();
            //transform.position = pos;
            //agent.Warp(pos);
            agent.SetDestination(destination.ToVector3());
        }

        private void OnStateUpdate(EFishState state)
        {
            if(state == EFishState.DEAD)
            {
                IsDead = true;
            }
        }

    }

}

