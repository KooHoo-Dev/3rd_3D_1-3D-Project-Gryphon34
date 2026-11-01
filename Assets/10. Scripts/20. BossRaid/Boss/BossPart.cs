using BossRaid.Combat;
using UnityEngine;

namespace BossRaid.Boss
{
    public class BossPart : MonoBehaviour
    {
        [SerializeField] private string partName;
        [SerializeField] private float maxDurability = 30;
        [SerializeField] private float magnitude = 1.2f;
        [SerializeField] private bool canBroken;

        private Boss boss;
        public object Owner => boss;
        public string PartName => partName;
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

        // 이번에 깎아서 깨졌으면 true (Boss가 이걸 보고 OnPartBroken을 보냄)
        public bool ReduceDurability(float amount)
        {
            CurrentDurability -= amount;
            if (CurrentDurability <= 0)
            {
                CurrentDurability = 0;
                BrokenPart();
                return true;
            }
            return false;
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