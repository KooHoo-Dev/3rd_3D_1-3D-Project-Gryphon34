using System;
using UnityEngine;

namespace BossRaid.Boss
{
    // 보스 한 단계의 값 (Boss 인스펙터 stages 칸 하나). 0번 = 1단계. 단계별 패턴은 DecideState 줄의 fromStage
    [Serializable]
    public class StageValue
    {
        [SerializeField] private float maxHp = 100;
        [SerializeField] private float maxFatigue = 30;
        [SerializeField] private float attackRate = 1f;       // 보스 히트박스 피해에 곱함 (2단계 ×1.3)
        [SerializeField] private Material bodyMaterial;       // 몸 머티리얼 (비면 프리팹 그대로)
        [SerializeField] private float rageFireHpRate; // HP가 이 비율 아래면 rageFire 히트박스에 불이 붙음 (0 = 없음)

        public float MaxHp => maxHp;
        public float MaxFatigue => maxFatigue;
        public float AttackRate => attackRate;
        public Material BodyMaterial => bodyMaterial;
        public float RageFireHpRate => rageFireHpRate;
    }
}
