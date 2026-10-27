using System;
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
        public bool IsInvincible { get; set; }

        public bool IsDead => (CurrentHealth <= 0);
        
        private Animator animator;
        private PlayerController playerController;
        private Rigidbody playerRigidbody;
        public event Action OnDeath;
        
        private void Awake()
        {
            CurrentHealth = maxHealth;
            animator = GetComponent<Animator>();
            playerController = GetComponent<PlayerController>();
            playerRigidbody = GetComponent<Rigidbody>();
            playerRigidbody.isKinematic = false;
        }
        
        public void TakeDamage(AttackInfo info)
        {
            if (IsDead) return;
            if(IsInvincible)
            {
                Debug.Log($"맞았지만 구르기 무적");
                return; // 무적이면 로그 찍고 무시
            }
            CurrentHealth -= info.Damage;
            if(CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                Dead();
                return;
            }
            Debug.Log($"[Player] {info.Damage} 만큼 맞음. 현재 체력 : {CurrentHealth} / {maxHealth}");
        }

        public void TakeHeal(float amount)
        {
            if (IsDead) return;
            CurrentHealth += amount;
            if (CurrentHealth > maxHealth) CurrentHealth = maxHealth;
        }

        private void Dead()
        {
            playerController.StopMovement();
            playerController.enabled = false;
            playerRigidbody.isKinematic = true;
            animator.SetTrigger(Die);
            OnDeath?.Invoke();
            Debug.Log($"[Player] 플레이어 is dead");
        }

#if UNITY_EDITOR
        // 임시 디버그 표시 (D-4 HP 바 만들면 삭제)
        private void OnGUI()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 24 };
            GUI.Label(new Rect(20, 90, 600, 40), $"HP {CurrentHealth:F1} / {maxHealth}", style);
        }
#endif

        [ContextMenu("Test Player Hit")]
        public void PlayerHit()
        {
            AttackInfo info = new AttackInfo(30, 0, transform.position);
            TakeDamage(info);
        }
    }
}