using BossRaid.Data;
using BossRaid.Player;
using UnityEngine;

namespace BossRaid.Flow
{
    [RequireComponent(typeof(Collider))]
    public class PickupSouls : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;

            Data.Progress progress = GameData.Current;
            DroppedSoul drop = progress.droppedSoul;
            progress.souls += drop.amount;
            Debug.Log($"[소울] 떨어진 소울 {drop.amount} 주움 → 가진 소울 {progress.souls}");

            drop.amount = 0;

            Destroy(gameObject);
        }
    }
}
