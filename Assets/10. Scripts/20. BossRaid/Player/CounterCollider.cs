using System.Collections.Generic;
using BossRaid.Boss;
using BossRaid.Combat;
using UnityEngine;

namespace BossRaid.Player
{
    // 안 씀 (상쇄는 11/02 뺌. 상자는 상쇄 클립만 켜서 입력을 막으면 안 켜짐)
    // 상쇄 상자. 같은 오브젝트의 WeaponCollider가 보스 몸(Hurtbox)을 약하게 때리고, 이건 보스 히트박스만 봅니다
    public class CounterCollider : MonoBehaviour
    {
        [SerializeField] private LayerMask mask;               // 보스 히트박스 레이어
        [SerializeField] private float counterFatigue = 10f;   // 상쇄 성공 때 보스 피로를 깎는 양
        [SerializeField] private float hitStopTime = 0.2f;     // 상쇄 성공 때 히트스탑 (콤보는 GreatSword의 값)

        private HashSet<BossHitbox> hitboxHash = new HashSet<BossHitbox>(); // 이번 상쇄에서 이미 처리한 히트박스
        private PlayerController player; // 성공하면 다음 타 차지 보너스
        private Animator playerAnimator;  // 히트스탑으로 멈출 플레이어 애니메이션

        private void Awake()
        {
            player = GetComponentInParent<PlayerController>();
            playerAnimator = GetComponentInParent<Animator>();
        }

        private void OnEnable()
        {
            hitboxHash.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (LayerMaskExtensions.Contains(mask, other.gameObject) == false) return;
            // 보스 몸(Hurtbox)은 항상 켜져 있어서 몸에 닿은 걸로 치면 안 됨
            if (other.TryGetComponent(out BossHitbox hitbox) == false) return;
            if (hitboxHash.Contains(hitbox)) return;
            hitboxHash.Add(hitbox);

            if (hitbox.Counterable == false) return; // 실패 → 몸에 그대로 맞음

            hitbox.SetCountered(); // 이번 공격은 맞아도 피격 모션 없음

            // BossRaid.Player 안이라 그냥 Boss라고 쓰면 BossRaid.Boss 네임스페이스로 읽힘
            BossRaid.Boss.Boss boss = hitbox.GetComponentInParent<BossRaid.Boss.Boss>();
            if (boss != null) boss.ReduceFatigue(counterFatigue);

            // 히트스탑: 플레이어와 보스 애니메이션만 잠깐 멈춤 (WeaponCollider와 같은 방식)
            HitStop.Play(playerAnimator, boss != null ? boss.Animator : null, hitStopTime);

            // 불꽃·금속음: 상쇄 상자에서 가장 가까운 히트박스 겉면 = 맞부딪친 자리
            if (player != null && player.Sword != null) player.Sword.PlayCounterEffect(other.ClosestPoint(transform.position));

            if (player != null) player.SetComboBonus(player.CounterBonusLevel);
            Debug.Log("[Counter] 상쇄 성공 → 다음 한 타 차지 보너스");
        }
    }
}
