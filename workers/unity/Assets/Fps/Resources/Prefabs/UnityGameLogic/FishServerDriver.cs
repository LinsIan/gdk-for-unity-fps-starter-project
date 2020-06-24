using UnityEngine;
using UnityEngine.AI;
using Fps.Config;
using Fps.WorkerConnectors;
using Improbable.Gdk.Subscriptions;
using Fps.SchemaExtensions;
using Improbable;

namespace Fps
{
    public class FishServerDriver : MonoBehaviour
    {
        [Require] private PositionWriter positionWriter;
        [Require] private ClientRotationWriter rotationWriter;
        [Require] private HealthComponentReader health;

        private const float MovementRadius = 50f;
        private const float NavMeshSnapDistance = 5f;
        private const float MinRemainingDistance = 0.3f;

        private EFishState eState;
        private NavMeshAgent agent;
        private Bounds worldBounds;
        private Vector3 anchorPoint;


        private void OnEnable()
        {
            agent = GetComponent<NavMeshAgent>();
            health.OnHealthModifiedEvent += OnHealthModified;
            agent.Warp(transform.position);
            eState = EFishState.IDLE;
        }

        private void Start()
        {
            eState = EFishState.SWIM;
            anchorPoint = transform.position;
            worldBounds = FindObjectOfType<GameLogicWorkerConnector>().Bounds;
        }

        private void Update()
        {
            if(eState == EFishState.SWIM)
            {
                Swimming();
            }
        }

        private void Swimming()
        {
            if (!agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(transform.position, out var hit, 0.5f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }
            }
            else if (agent.remainingDistance < MinRemainingDistance || agent.pathStatus == NavMeshPathStatus.PathInvalid ||
                !agent.hasPath)
            {
                SetRandomDestination();
            }
            else
            {
                UpdateTransform();
            }
        }

        private void UpdateTransform()
        {
            Vector3 pos = transform.position;
            pos.y += 1000;
            positionWriter?.SendUpdate(new Position.Update { Coords = Coordinates.FromUnityVector(pos) });

            var rotationUpdate = new RotationUpdate
            {
                Pitch = transform.rotation.eulerAngles.x.ToInt1k(),
                Yaw = transform.rotation.eulerAngles.y.ToInt1k(),
                Roll = transform.rotation.eulerAngles.z.ToInt1k()
            };

            rotationWriter?.SendUpdate(new ClientRotation.Update
            {
                Latest = rotationUpdate
            });
        }

        private void OnHealthModified(HealthModifiedInfo info)
        {
            if (info.Died)
            {
                eState = EFishState.DEAD;
            }
        }

        public void SetRandomDestination()
        {
            var destination = anchorPoint + Random.insideUnitSphere * MovementRadius;
            destination.y = anchorPoint.y;
            if (NavMesh.SamplePosition(destination, out var hit, NavMeshSnapDistance, NavMesh.AllAreas))
            {
                if (worldBounds.Contains(hit.position))
                {
                    agent.isStopped = false;
                    agent.nextPosition = transform.position;
                    agent.SetDestination(hit.position);
                }
            }
        }
    }
}
