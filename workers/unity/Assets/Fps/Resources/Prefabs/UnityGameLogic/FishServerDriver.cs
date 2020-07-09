using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Fps.Config;
using Fps.WorkerConnectors;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Improbable.Worker.CInterop;
using Fps.SchemaExtensions;
using Improbable;

namespace Fps
{
    [WorkerType(WorkerUtils.UnityGameLogic)]
    public class FishServerDriver : MonoBehaviour
    {
#pragma warning disable 649
        [Require] private PositionWriter positionWriter;
        [Require] private ClientRotationWriter rotationWriter;
        [Require] private HealthComponentReader health;
        [Require] private HealthComponentCommandSender healthCommandSender;
        [Require] private FishComponentWriter fishComponentWriter;
        [Require] private ScoreComponentCommandSender scoreCommandSender;
        [Require] private EntityId entityId;
#pragma warning disable 649

        private const float MovementRadius = 50f;
        private const float NavMeshSnapDistance = 5f;
        private const float MinRemainingDistance = 0.3f;

        private EFishState eState;
        private NavMeshAgent agent;
        private Bounds worldBounds;
        private float score;
        private float RespawnTime = 5.0f;
        float offsetY = 0;


        private void OnEnable()
        {
            agent = GetComponent<NavMeshAgent>();
            score = FishSettings.FishScoreDic[fishComponentWriter.Data.Type];
            RespawnTime = FishSettings.FishRespawnTimeDic[fishComponentWriter.Data.Type];
            worldBounds = FindObjectOfType<GameLogicWorkerConnector>().Bounds;
            health.OnHealthModifiedEvent += OnHealthModified;
            eState = EFishState.SWIM;

            agent.enabled = true;
            agent.isStopped = false;
            offsetY = transform.position.y - positionWriter.Data.Coords.ToUnityVector().y;
        }

        private void OnDisable()
        {
            if(eState == EFishState.SWIM)
            {
                var pos = transform.position + transform.forward * 10;
                transform.position = pos;
                agent.Warp(pos);
            }
            

            health.OnHealthModifiedEvent -= OnHealthModified;
            agent.isStopped = true;
        }

        private void Update()
        {
            if (eState == EFishState.SWIM)
            {
                Swimming();
            }
            UpdateTransform();
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
        }

        private void UpdateTransform()
        {
            Vector3 pos = transform.position;

            pos.y = positionWriter.Data.Coords.ToUnityVector().y;

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
                agent.isStopped = true;
                SendScoreCommand(info.Modifier.ModifierId);
                StartCoroutine(WaitForRespawn());
            }
            else if (eState == EFishState.DEAD)
            {
                agent.isStopped = false;
                eState = EFishState.SWIM;
            }
        }

        private void SendScoreCommand(EntityId modifierId)
        {
            var scoreModifier = new ScoreModifier
            {
                Amount = score,
                Owner = modifierId,
            };
            scoreCommandSender.SendModifyScoreCommand(modifierId, scoreModifier);
        }
        
        private void SetRandomDestination()
        {
            var destination = transform.position + Random.insideUnitSphere * MovementRadius;
            destination.y = transform.position.y;
            if (NavMesh.SamplePosition(destination, out var hit, NavMeshSnapDistance, NavMesh.AllAreas))
            {
                if (worldBounds.Contains(hit.position))
                {
                    agent.isStopped = false;
                    agent.nextPosition = transform.position;
                    agent.SetDestination(hit.position);

                    var update = new FishComponent.Update { Destination = (hit.position).ToVector3Int(), Type = fishComponentWriter.Data.Type };
                    fishComponentWriter.SendUpdate(update);
                }
            }
        }

        private IEnumerator WaitForRespawn()
        {
            if (health.Authority != Improbable.Worker.CInterop.Authority.Authoritative) yield break;
            yield return new WaitForSeconds(RespawnTime);
            Respawn();
        }

        private void Respawn()
        {
            //重設Health
            var modifyHealthRequest = new HealthComponent.ModifyHealth.Request(
                    entityId,
                    new HealthModifier
                    {
                        Amount = health.Data.MaxHealth - health.Data.Health,
                        Owner = entityId,
                    }
                );
            healthCommandSender.SendModifyHealthCommand(modifyHealthRequest);

            //重設座標與目標
            var spawnPosition = RandomPoint.Instance.RandomNavmeshLocation();
            spawnPosition.y += FishSettings.FishOffsetYDic[fishComponentWriter.Data.Type];
            positionWriter?.SendUpdate(new Position.Update { Coords = Coordinates.FromUnityVector(spawnPosition) });
            agent.Warp(transform.position);
            transform.position = new Vector3(spawnPosition.x, transform.position.y - offsetY, spawnPosition.z);
            SetRandomDestination();
        }
    }
}
