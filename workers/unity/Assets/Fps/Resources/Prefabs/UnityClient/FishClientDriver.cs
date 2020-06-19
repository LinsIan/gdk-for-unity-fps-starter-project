using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Fps.Config;

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
        [Require] private ClientRotationWriter rotation;
        [Require] private HealthComponentReader health;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}

