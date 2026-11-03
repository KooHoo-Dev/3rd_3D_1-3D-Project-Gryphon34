using System;
using BossRaid.Combat;
using UnityEngine;

namespace BossRaid.Player
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        private static readonly int Die = Animator.StringToHash("Die");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int ReactionHash = Animator.StringToHash("Reaction");
        public object Owner => this;

        [SerializeField] private float maxHealth = 100f;

        [Header("소리 (크기·피치·2D/3D는 AudioSource에서)")]
        [SerializeField] private AudioSource audioSource; // 플레이어 루트처럼 항상 켜진 오브젝트 (꺼지면 소리가 끊김)
        [SerializeField] private AudioClip[] hitSounds;   // 가드·태클 없이 맞은 순간 + 죽는 순간

        public float MaxHealth => maxHealth + (isHuman ? 25 : 0); // 사람 상태면 +25. maxHealth(인스펙터)는 안 바꿈 → 몇 번 불러도 안 쌓임
        public float CurrentHealth { get; private set; }
        public bool IsInvincible { get; set; }

        public bool IsDead => (CurrentHealth <= 0);
        private float FireReduce => playerController.Loadout != null ? playerController.Loadout.FireReduce : 0f; // 방어구 + 요리 불 저항
        
        private bool isHuman; // 사람 상태 (Progress.isHuman을 SetMaxHealth로 받아 옴. PlayerHealth는 Progress를 모름)
        private float damageReduce; // 방어구의 받는 피해 감소 (0.15 = 15% 덜 아픔). PlayerLoadout이 넣어 줌

        private Animator animator;
        private PlayerController playerController;
        private Rigidbody playerRigidbody;
        public event Action OnDeath;
        public event Action OnHitTaken; // 가드·태클 없이 맞은 순간 (죽은 때는 빼고). TutorialHint가 셈

        private void Awake()
        {
            CurrentHealth = MaxHealth;
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
                Debug.Log($"맞았지만 무적");
                return; // 무적이면 로그 찍고 무시
            }
            // 지금 상태(가드·태클) × 공격 태그로 결과를 정함. 막거나 버티면 피격 모션 없음
            float damage = info.Damage;
            bool isGuarded = playerController.TryGuard(info, ref damage);
            bool isArmored = isGuarded == false && playerController.TryTackleArmor(info, ref damage);
            damage *= 1f - damageReduce;
            if (info.Ignite) damage *= 1f - FireReduce; // 불이 붙는 공격은 직격에도 불 저항
            CurrentHealth -= damage;
            if(CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                Dead();
                return;
            }
            if (isGuarded == false && isArmored == false)
            {
                OnHitTaken?.Invoke();
                PlayRandomSound(hitSounds);
            }
            // 가드로 막으면 안 붙음. 태클로 버티면 붙음
            if (info.Ignite && isGuarded == false && playerController.Burn != null) playerController.Burn.Ignite();
            if (isGuarded == false && isArmored == false && info.Reaction != HitReaction.None) // None = 상쇄당한 공격. 피해만 받고 모션 없음
            {
                animator.SetInteger(ReactionHash, (int)info.Reaction);
                animator.SetTrigger(HitHash);
            }
            Debug.Log($"[Player] {damage} 만큼 맞음 (원래 {info.Damage}). 현재 체력 : {CurrentHealth} / {MaxHealth}");
        }

        // 화염 상태의 지속 피해 (PlayerBurn이 매 프레임). 피격이 아님: 무적이어도 깎이고 모션 없음
        public void TakeBurnDamage(float amount)
        {
            if (IsDead) return;
            CurrentHealth -= amount;
            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                Dead();
            }
        }

        public void TakeHeal(float amount)
        {
            if (IsDead) return;
            CurrentHealth += amount;
            if (CurrentHealth > MaxHealth) CurrentHealth = MaxHealth;
        }

        // 씬 시작(저장된 값)과 인간성을 쓰는 순간에 부름. 최대 HP를 다시 정하고 HP를 전부 채움
        public void SetMaxHealth(bool useHumanity)
        {
            isHuman = useHumanity;
            CurrentHealth = MaxHealth;
        }

        // 끼고 있는 방어구의 받는 피해 감소. PlayerLoadout이 씬 시작과 장비를 바꿀 때 부름
        public void SetDamageReduce(float reduce)
        {
            damageReduce = reduce;
        }

        private void Dead()
        {
            playerController.StopMovement();
            playerController.enabled = false;
            playerRigidbody.isKinematic = true;
            animator.SetTrigger(Die);
            PlayRandomSound(hitSounds); // 죽는 한 방은 위에서 return 해서 피격음 줄까지 안 감 → 여기서 냄 (화염으로 죽어도 남)
            OnDeath?.Invoke();
            Debug.Log($"[Player] 플레이어 is dead");
        }

        private void PlayRandomSound(AudioClip[] clips)
        {
            if (audioSource == null || clips == null || clips.Length == 0) return;

            AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Length)];
            if (clip != null) audioSource.PlayOneShot(clip);
        }

        [ContextMenu("Test Player Hit")]
        public void PlayerHit()
        {
            AttackInfo info = new AttackInfo(30, 0, transform.position);
            TakeDamage(info);
        }
        

        private void TestHit(HitReaction reaction)
        {
            TakeDamage(new AttackInfo(0, 0, transform.position, reaction));
        }
    }
}