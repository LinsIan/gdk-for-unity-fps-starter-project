using UnityEngine;
using Fps.Config;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;


namespace Fps
{
    [WorkerType(WorkerUtils.UnityGameLogic)]
    public class PlayerHealthComponent : MonoBehaviour
    {
        [Require] private HealthComponentReader healthComponentReader;
        [Require] private LogComponentCommandSender logComponentCommandSender;
        [Require] private EntityId entityId;

        public bool IsHealthy()
        {
            if (healthComponentReader == null) return false;
            return (healthComponentReader.Data.Health >= healthComponentReader.Data.MaxHealth);
        }

        //Random Point Test
        private float timer;
        private void Update()
        {
            timer += Time.deltaTime;
            if(timer >= 1)
            {
                logComponentCommandSender.SendDebugLogCommand(entityId, new LogMessage { Message = RandomPoint.Instance.RandomNavmeshLocation().ToString() });
                timer = 0;
            }
        }
    }
}
