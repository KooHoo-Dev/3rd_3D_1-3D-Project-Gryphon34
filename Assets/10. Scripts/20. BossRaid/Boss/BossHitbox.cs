using System.Collections.Generic;
using BossRaid.Combat;
using UnityEngine;

namespace BossRaid.Boss
{
    public class BossHitbox : MonoBehaviour
    {
        [SerializeField] private LayerMask mask;
        [SerializeField] private float damage = 20;
        [SerializeField] public string id;
        [SerializeField] private HitReaction reaction; // 이 히트박스에 맞은 플레이어의 피격 모션
        [SerializeField] private bool counterable; // 상쇄 가능 (물기·앞발 할퀴기만 켬)
        [SerializeField] private AttackTag attackTag; // 공격 종류 (가벼운 공격 = 태클로 버팀, 가드 불가, 화염)
        [SerializeField] private bool rageFire; // 2단계 HP 비율 아래에서 불이 붙는 근접 공격 (철산고는 끔)

        public bool Counterable => counterable;

        private float fatigueDamage = 0;
        private bool isCountered; // 이번 공격이 상쇄당함 → 맞혀도 피격 모션 없음

        private HashSet<object> playerHash = new HashSet<object>();
        private Transform source; // 공격한 쪽 = 부모 보스 몸. 불덩이 폭발처럼 보스 밑이 아니면 자기 자신
        private Boss boss; // 단계 공격력 배율을 읽음. 불덩이 폭발은 SetBoss로 받음 (없으면 ×1)

        private void Awake()
        {
            boss = GetComponentInParent<Boss>();
            source = boss != null ? boss.transform : transform;
        }

        // 보스 밑이 아닌 히트박스(불덩이 폭발)가 만들어진 직후 쏜 보스를 받음. source는 그대로 (가드 방향 = 폭발 자리)
        public void SetBoss(Boss owner)
        {
            boss = owner;
        }

        private void OnEnable()
        {
            ResetHash();
            isCountered = false;
        }

        public void SetCountered()
        {
            isCountered = true;
        }
        
        public void ResetHash()
        {
            playerHash.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (LayerMaskExtensions.Contains(mask, other.gameObject) == false) return;
            if (other.TryGetComponent(out IDamageable damageable) == false) return;
            if (playerHash.Contains(damageable.Owner)) return;
            playerHash.Add(damageable.Owner);

            float finalDamage = damage * (boss != null ? boss.AttackRate : 1f);
            bool ignite = attackTag == AttackTag.Fire || (rageFire && boss != null && boss.IsRageFire);
            AttackInfo info = new AttackInfo(finalDamage, fatigueDamage, other.transform.position, isCountered ? HitReaction.None : reaction, 1f, attackTag, source, ignite);
            damageable.TakeDamage(info);
        }
    }
}