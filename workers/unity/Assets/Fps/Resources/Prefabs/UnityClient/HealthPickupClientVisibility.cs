using UnityEngine;
using Fps.Config;
using Improbable.Gdk.Subscriptions;
using Pickups;

namespace Fps
{
    //標記該MonoBehavior只能在UnityClient這個Worker上面使用
    [WorkerType(WorkerUtils.UnityClient)]
    public class HealthPickupClientVisibility : MonoBehaviour
    {
        //Reader負責取得SpatialOS的資料，Require代表這個物件是必須的
        [Require] private HealthPickupReader m_HealthPickupReader;
        private MeshRenderer m_CubeMeshRender;

        //初始化
        private void OnEnable()
        {
            m_CubeMeshRender = GetComponentInChildren<MeshRenderer>();
            m_HealthPickupReader.OnUpdate += OnHealthPickupComponentUpdated;
            UpdateVisibility();
        }

        //抓到同步過來的資料
        private void UpdateVisibility()
        {
            m_CubeMeshRender.enabled = m_HealthPickupReader.Data.IsActive;
        }

        //有資料更新時觸發
        private void OnHealthPickupComponentUpdated(Pickups.HealthPickup.Update update)
        {
            UpdateVisibility();
        }
    }

}

