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
        [Require] private ClientRotationReader rotation;
        //[Require] private HealthComponentReader health;
        [Require] private FishComponentReader fishComponent;

        private Animation.FishAnimator animator;
        float offsetY = 0;

        private void OnEnable()
        {
            animator = GetComponent<Animation.FishAnimator>();
            position.OnUpdate += OnPositionUpdate;
            rotation.OnUpdate += OnRotationUpdate;
            //health.OnHealthModifiedEvent += OnHealthModified;
            fishComponent.OnStateUpdate += OnStateUpdate;
            offsetY = transform.position.y - position.Data.Coords.ToUnityVector().y;
        }

        private void OnPositionUpdate(Position.Update update)
        {
            Vector3 pos = position.Data.Coords.ToUnityVector();
            pos.y += offsetY;
            transform.position = pos;
        }

        private void OnRotationUpdate(ClientRotation.Update update)
        {
            float pitch = rotation.Data.Latest.Pitch.ToFloat1k();
            float roll = rotation.Data.Latest.Roll.ToFloat1k();
            float yaw = rotation.Data.Latest.Yaw.ToFloat1k();
            transform.rotation = Quaternion.Euler(pitch, yaw, roll);
        }

        private void OnStateUpdate(EFishState state)
        {
            animator.PlayAnimation(state);
        }
        
        //private void OnHealthModified(HealthModifiedInfo info)
        //{
        //    if(info.Died)
        //    {
        //        eState = EFishState.DEAD;
        //        animator.PlayAnimation(eState);
        //    }
        //    else if(eState == EFishState.DEAD)
        //    {
        //        eState = EFishState.SWIM;
        //        animator.PlayAnimation(eState);
        //    }
        //}
    }
}
