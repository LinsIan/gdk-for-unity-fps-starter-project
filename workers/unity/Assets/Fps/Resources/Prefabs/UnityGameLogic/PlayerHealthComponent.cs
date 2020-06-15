using UnityEngine;
using Fps.Config;
using Improbable.Gdk.Subscriptions;


namespace Fps
{
    [WorkerType(WorkerUtils.UnityGameLogic)]
    public class PlayerHealthComponent : MonoBehaviour
    {
        [Require] private HealthComponentReader m_HealthComponentReader;

        public bool IsHealthy()
        {
            if (m_HealthComponentReader == null) return false;
            return (m_HealthComponentReader.Data.Health >= m_HealthComponentReader.Data.MaxHealth);
        }
    }
}
