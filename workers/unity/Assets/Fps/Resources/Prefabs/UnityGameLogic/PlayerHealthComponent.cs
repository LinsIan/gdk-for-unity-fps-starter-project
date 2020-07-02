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
        [Require] private EntityId entityId;
        //[Require] private LogComponentCommandSender commandSender;

        public bool IsHealthy()
        {
            if (healthComponentReader == null) return false;
            return (healthComponentReader.Data.Health >= healthComponentReader.Data.MaxHealth);
        }

        //float timer = 0;
        //private void Update()
        //{
        //    timer += Time.deltaTime;
        //    if(timer >= 1)
        //    {
        //        timer = 0;
        //        string message = "level : " + RandomPoint.Instance.levelpos.ToString() + ", Mesh : " + RandomPoint.Instance.meshpos.ToString();
        //        commandSender.SendDebugLogCommand(new LogComponent.DebugLog.Request(entityId, new LogMessage { Message = message }));
        //    }
        //}
    }
}
