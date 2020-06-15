using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fps.Config;
using Improbable.Gdk.Subscriptions;
using Pickups;

namespace Fps
{
    //標記該MonoBehavior只能在UnityGameLogic這個Worker上面使用
    [WorkerType(WorkerUtils.UnityGameLogic)]
    public class HealthPickupServerBehavior : MonoBehaviour
    {
        //取得且可修改HealthPickup資料
        [Require] private HealthPickupWriter m_HealthPickupWriter;
        [Require] private HealthComponentCommandSender m_HealthCommandRequestSender;

        private Coroutine m_RespawnCoroutine;
        private Collider m_Collider;

        private void OnEnable()
        {
            m_Collider = gameObject.GetComponentInChildren<Collider>();
            if(!m_HealthPickupWriter.Data.IsActive)
            {
                m_Collider.enabled = false;
                m_RespawnCoroutine = StartCoroutine(RespawnHealthPackRoutine());
            }
        }

        private void OnDisable()
        {
            if (m_RespawnCoroutine != null)
            {
                StopCoroutine(m_RespawnCoroutine);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            //在物件disable的時候也會觸發，所以要做null check
            if (m_HealthPickupWriter == null || !other.CompareTag("Player")) return;

            HandleCollisionWithPlayer(other.gameObject);
        }

        private void SetIsActive(bool isActive)
        {
            m_Collider.enabled = isActive;
            //送出需要修改的資訊
            m_HealthPickupWriter?.SendUpdate(new Pickups.HealthPickup.Update
            {
                IsActive = isActive
            });
        }

        private void HandleCollisionWithPlayer(GameObject player)
        {
            var playerSpatialOSComponent = player.GetComponent<LinkedEntityComponent>();
            if (playerSpatialOSComponent == null) return;
      
            //檢查玩家是否滿血
            var playerHealthComponent = player.GetComponent<PlayerHealthComponent>();
            if (playerHealthComponent == null || playerHealthComponent.IsHealthy()) return;

            m_HealthCommandRequestSender.SendModifyHealthCommand(playerSpatialOSComponent.EntityId, new HealthModifier
            {
                Amount = m_HealthPickupWriter.Data.HealthValue
            });

            SetIsActive(false);
            m_RespawnCoroutine = StartCoroutine(RespawnHealthPackRoutine());
        }

        private IEnumerator RespawnHealthPackRoutine()
        {
            yield return new WaitForSeconds(15f);
            SetIsActive(true);
        }
    }


}
