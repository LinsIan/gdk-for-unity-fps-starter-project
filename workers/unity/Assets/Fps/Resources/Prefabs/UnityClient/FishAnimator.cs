using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Fps.Config;

namespace Fps.Animation
{
    public class FishAnimator : MonoBehaviour
    {
        private const string SWIM_ANIMATION = "Swim";
        private const string DEAD_ANIMATION = "Dead";
        private Animator m_Animator;

        private void OnEnable()
        {
            m_Animator = GetComponent<Animator>();
        }

        public void PlayAnimation(EFishState eState)
        {
            switch(eState)
            {
                case EFishState.SWIM:
                    m_Animator.Play(SWIM_ANIMATION);
                    break;
                case EFishState.DEAD:
                    m_Animator.Play(DEAD_ANIMATION);
                    break;
            }
        }
    }
}
