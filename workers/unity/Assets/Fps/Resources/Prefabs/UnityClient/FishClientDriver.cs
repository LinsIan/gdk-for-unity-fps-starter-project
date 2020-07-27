using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Fps.Config;
using Fps.SchemaExtensions;
using Improbable;

namespace Fps
{
    [WorkerType(WorkerUtils.UnityClient)]
    public class FishClientDriver : MonoBehaviour
    {
        [Require] private PositionReader position;
        //[Require] private ClientRotationReader rotation;
        [Require] private FishComponentReader fishComponent;
        
        private Animation.FishAnimator animator;
        float offsetY = 0;
        Queue<Vector3> positionQueue;
        Queue<Quaternion> rotationQueue;
        Quaternion targetRotation;
        Vector3 destination;
        Vector3 moveVector;
        float speed = 0;
        float angularSpeed = 0;
        float distance = 0;
        bool isDead;

        float targetAngle = 0;

        private void OnEnable()
        {
            animator = GetComponent<Animation.FishAnimator>();
            position.OnUpdate += OnPositionUpdate;
            //rotation.OnUpdate += OnRotationUpdate;
            fishComponent.OnStateUpdate += OnStateUpdate;
            offsetY = transform.position.y - position.Data.Coords.ToUnityVector().y;
            speed = FishSettings.FishSpeedDic[fishComponent.Data.Type];
            angularSpeed = FishSettings.FishAngularSpeedDic[fishComponent.Data.Type];
            positionQueue = new Queue<Vector3>();
            rotationQueue = new Queue<Quaternion>();
        }

        private void OnPositionUpdate(Position.Update update)
        {
            Vector3 pos = position.Data.Coords.ToUnityVector();
            pos.y += offsetY;
            positionQueue.Enqueue(pos);
        }

        private void OnStateUpdate(EFishState state)
        {
            animator.PlayAnimation(state);
            isDead = (state == EFishState.DEAD);
        }

        private void Update()
        {
            if(!isDead)
            {
                UpdateTransform();
            }
        }

        private void UpdateTransform()
        {
            //Position
            if(distance <= 0)
            {
                GetNextDestination();
            }
            else if(distance != 0)
            {
                float delta = speed * Time.deltaTime;
                distance -= delta;
                Vector3 newDirection = Vector3.RotateTowards(transform.forward, destination - transform.position, 5.0f * angularSpeed/360.0f * Time.deltaTime, 0);
                transform.rotation = Quaternion.LookRotation(newDirection);
                if (distance <= 0)
                {
                    transform.position = destination;
                }
                else
                {
                    transform.position += moveVector * delta;
                }
            }
            
        }

        private void GetNextDestination()
        {
            if (positionQueue.Count <= 0) return;
            do
            {
                destination = positionQueue.Dequeue();
                var dis = destination - transform.position;
                distance = dis.magnitude;
                moveVector = dis.normalized;
            }
            while (positionQueue.Count >= 3);
        }

    }
}
