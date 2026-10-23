using System.Reflection;
using BossRaid.Combat;
using UnityEngine;

namespace BossRaid.Boss
{
    public class BossPart : MonoBehaviour, IDamageable
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