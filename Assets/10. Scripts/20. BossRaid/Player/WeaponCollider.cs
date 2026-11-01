using System.Collections.Generic;
using BossRaid.Combat;
using BossRaid.Weapon;
using UnityEngine;

namespace BossRaid.Player
{
    public class WeaponCollider : MonoBehaviour
    {
        [SerializeField] private LayerMask mask;
        [SerializeField] private float motionValue = 0.1f;   // 이 동작의 데미지 비율 (무기 공격력에 곱함)
        [SerializeField] private float motionFatigue = 5f;   // 이 동작이 보스 피로도를 깎는 기본량

        private HashSet<object> bossHash = new HashSet<object>();
        private float swordDamage;  // 무기 공격력 x 차지 배율 (GreatSword가 넣어줌)
        private float fatigueRate;  // 차지 배율 (GreatSword가 넣어줌)

        private GreatSword sword;         // 히트스탑 시간을 읽음 (플레이 중에 바꿔도 바로 적용되게 맞힐 때마다 읽음)
        private Animator playerAnimator;  // 히트스탑으로 멈출 플레이어 애니메이션

        private void Awake()
        {
            sword = GetComponentInParent<GreatSword>();
            playerAnimator = GetComponentInParent<Animator>();
        }

        public void SetAttackValue(float swordDamage, float fatigueRate)
        {
            this.swordDamage = swordDamage;
            this.fatigueRate = fatigueRate;
        }

        public void ResetHash()
        {
            bossHash.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (LayerMaskExtensions.Contains(mask, other.gameObject) == false) return;
            if (other.TryGetComponent(out IDamageable damageable) == false) return;
            if (bossHash.Contains(damageable.Owner)) return;
            bossHash.Add(damageable.Owner);

            AttackInfo info = new AttackInfo(swordDamage * motionValue, motionFatigue * fatigueRate, other.transform.position);
            damageable.TakeDamage(info);

            // 히트스탑: 플레이어와 맞은 보스(Owner)의 애니메이션만 잠깐 멈춤
            Animator targetAnimator = (damageable.Owner as Component)?.GetComponent<Animator>();
            if (sword == null) return;
            HitStop.Play(playerAnimator, targetAnimator, sword.HitStopTime);
            CameraShake.Play(sword.ShakeStrength, sword.ShakeTime);

            // 타격 소리·이펙트: 칼에서 가장 가까운 Hurtbox 겉면 = 맞은 자리, 칼 → 그 자리 = 들어온 방향
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            sword.PlayHitEffect(hitPoint, hitPoint - transform.position);
        }
    }
}