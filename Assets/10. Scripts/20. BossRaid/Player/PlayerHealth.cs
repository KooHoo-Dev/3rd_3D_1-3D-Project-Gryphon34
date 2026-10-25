using BossRaid.Combat;
using UnityEngine;

namespace BossRaid.Player
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        private static readonly int Die = Animator.StringToHash("Die");
        public object Owner => this;

        [SerializeField] private float maxHealth = 100f;

        public float CurrentHealth { get; private set; }

        public bool IsDead => (CurrentHealth <= 0);
        
        private Animator animator;
        private PlayerController playerController;
        private void Awake()
        {
            CurrentHealth = maxHealth;
            animator = GetComponent<Animator>();
            playerController = GetComponent<PlayerController>();
        }
        
        public void TakeDamage(AttackInfo info)
        {
            if (IsDead) return;
            CurrentHealth -= info.Damage;
            if(CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                Dead();
                return;
            }
            Debug.Log($"[Player] {info.Damage} 만큼 맞음. 현재 체력 : {CurrentHealth} / {maxHealth}");
        }

        private void Dead()
        {
            playerController.StopMovement();
            playerController.enabled = false;
            animator.SetTrigger(Die);
            Debug.Log($"[Player] 플레이어 is dead");
        }

        [ContextMenu("Test Player Hit")]
        public void PlayerHit()
        {
            AttackInfo info = new AttackInfo(30, 0, transform.position);
            TakeDamage(info);
        }
    }
}