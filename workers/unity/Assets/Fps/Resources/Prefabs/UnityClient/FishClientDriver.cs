using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Fps.Config;
using Fps.SchemaExtensions;
using Improbable;

namespace Fps
{
    public enum EFishState
    {
        SWIM,
        DEAD
    }

    [WorkerType(WorkerUtils.UnityClient)]
    public class FishClientDriver : MonoBehaviour
    {
        [Require] private PositionReader position;
        [Require] private ClientRotationWriter rotation;
        [Require] private HealthComponentReader health;

        private Animation.FishAnimator animator;

        private EFishState eState;

        private void OnEnable()
        {
            animator = GetComponent<Animation.FishAnimator>();
            position.OnUpdate += OnPositionUpdate;
            rotation.OnUpdate += OnRotationUpdate;
            health.OnUpdate += OnHealthComponentUpdate;
            eState = EFishState.SWIM;
        }

        private void OnPositionUpdate(Position.Update update)
        {
            transform.position = position.Data.Coords.ToUnityVector();
        }

        private void OnRotationUpdate(ClientRotation.Update update)
        {
            float pitch = rotation.Data.Latest.Pitch.ToFloat1k();
            float roll = rotation.Data.Latest.Roll.ToFloat1k();
            float paw = rotation.Data.Latest.Yaw.ToFloat1k();
            transform.rotation = Quaternion.Euler(pitch, roll, paw);
        }

        private void OnHealthComponentUpdate(HealthComponent.Update update)
        {
            if(health.Data.Health <= 0)
            {
                eState = EFishState.DEAD;
                animator.PlayAnimation(eState);
            }
        }
    }
}
