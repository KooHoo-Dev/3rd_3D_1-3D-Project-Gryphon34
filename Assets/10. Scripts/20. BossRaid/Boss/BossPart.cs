using BossRaid.Combat;
using UnityEngine;

namespace BossRaid.Boss
{
    // 논리적인 부위 하나(머리, 왼날개, 꼬리 …)의 정보입니다. 부위마다 하나만 둡니다.
    // 직접 맞지 않고, 그 부위의 Hurtbox들이 맞으면 TakeDamage로 넘겨받습니다.
    public class BossPart : MonoBehaviour
    {
        [SerializeField] private string partName;
        [SerializeField] private float maxDurability = 30;
        [SerializeField] private float magnitude = 1.2f;
        [SerializeField] private bool canBroken;

        private Boss boss;
        public object Owner => boss;
        public float CurrentDurability { get; private set; }
        public bool IsBroken { get; private set; }
        public bool CanBroken => canBroken;
        public float CurrentMagnitude => IsBroken ? magnitude : 1;
        
        
        
        private void Awake()
        {
            boss = GetComponentInParent<Boss>();
            CurrentDurability = maxDurability;
        }
        
        public void TakeDamage(AttackInfo info)
        {
            Debug.Log($"[{partName} 맞음]");
            boss.ApplyHit(info, this);
        }

        public void ReduceDurability(float amount)
        {
            CurrentDurability -= amount;
            if (CurrentDurability <= 0)
            {
                CurrentDurability = 0;
                BrokenPart();
            }
        }

        public void BrokenPart()
        {
            IsBroken = true;
            Debug.Log($"{partName} broken!"); 
        }


        [ContextMenu("Test Hit")]
        private void TestHit()
        {
            AttackInfo info = new AttackInfo(30, 10, transform.position);
            TakeDamage(info);
        }
    }
}