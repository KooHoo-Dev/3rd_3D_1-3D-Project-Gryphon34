using BossRaid.Data;
using BossRaid.Player;
using UnityEngine;

namespace BossRaid.Flow
{
    // 투기장에 떨어진 소울입니다. HuntManager.Start가 GameData의 droppedSoul 위치에 만들고, 플레이어가 닿으면 돌려줍니다.
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

            // GameData도 비워야 다음 판에 또 생기지 않음 (오브젝트는 씬이 바뀌면 어차피 사라지고 GameData만 남음)
            drop.amount = 0;

            Destroy(gameObject);
        }
    }
}
