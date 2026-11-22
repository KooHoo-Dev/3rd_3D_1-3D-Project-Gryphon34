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
        [SerializeField] private int fromStage; // 이 단계부터 깨짐 (0·1 = 1단계부터, 2 = 2단계부터). 그 전 단계에선 내구도가 안 깎임
        [SerializeField] private float stage2Durability; // 2단계 내구도 (0 = maxDurability 그대로). 1단계부터 깨지는 부위를 2단계에서만 쉽게

        private Boss boss;
        public object Owner => boss;
        public string PartName => partName;
        public float CurrentDurability { get; private set; }
        public bool IsBroken { get; private set; }
        public bool CanBroken => canBroken;
        public bool CanBreakAt(int stage) => canBroken && fromStage <= stage; // 지금 단계(Boss.StageNumber)에서 깰 수 있나
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

        // 단계가 정해질 때 Boss.SetStage가 부름: 2단계고 stage2Durability가 있으면 그 값, 아니면 maxDurability (이미 깨진 부위는 그대로)
        public void SetStage(int stage)
        {
            if (IsBroken) return;
            CurrentDurability = stage >= 2 && stage2Durability > 0 ? stage2Durability : maxDurability;
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
            CurrentDurability = 0; // 체크포인트로 바로 부술 때도 0
            Debug.Log($"{partName} broken!"); 
        }


        // 임시(TestCheats F8): 내구도를 한 대만 맞으면 깨지는 값으로. 깨진 부위는 그대로
        public void TestNearBreak()
        {
            if (IsBroken) return;
            CurrentDurability = Mathf.Min(CurrentDurability, 1f);
        }

        [ContextMenu("Test Hit")]
        private void TestHit()
        {
            AttackInfo info = new AttackInfo(30, 10, transform.position);
            TakeDamage(info);
        }
    }
}