using System;
using System.Collections.Generic;
using Study.Utilities;
using UnityEngine;

namespace BossRaid.Boss.States
{
    public class DecideState : BossStates
    {
        [Serializable]
        public class Pattern
        {
            [SerializeField] private StateName patternName;
            [SerializeField] private float minDistance;
            [SerializeField] private float maxDistance;
            [SerializeField] private float minAngle;
            [SerializeField] private float maxAngle;
            [SerializeField] private bool farDistancePattern;
            [SerializeField] private int fromStage; // 이 단계부터 나옴 (0·1 = 1단계부터, 2 = 2단계부터)
            [SerializeField] private bool needWings; // 날아야 하는 패턴 (날개가 전부 깨지면 안 나옴)

            public StateName PatternName => patternName;
            public float MinDistance => minDistance;
            public float MaxDistance => maxDistance;
            public float MinAngle => minAngle;
            public float MaxAngle => maxAngle;
            public bool FarDistancePattern => farDistancePattern;
            public int FromStage => fromStage;
            public bool NeedWings => needWings;
        }
        
        [SerializeField] private Pattern[] patterns;
        [SerializeField] private DetectDistance detect;
        private StateName lastState;
        
        public Pattern[] Patterns => patterns;

        // 지금 나오는 패턴 (fromStage가 지금 단계 이하 + 날개가 필요하면 날 수 있을 때). 플레이 전 기즈모에선 전부
        public IEnumerable<Pattern> AllPatterns
        {
            get
            {
                if (patterns == null) yield break;
                foreach (Pattern pattern in patterns)
                {
                    if (boss == null) yield return pattern;
                    else if (pattern.FromStage <= boss.StageNumber && (pattern.NeedWings == false || boss.CanFly)) yield return pattern;
                }
            }
        }

        
        public override StateName Name => StateName.DecideState;
        
        
        public override void Enter()
        {
            if (boss.Player == null)
            {
                boss.ChangeState(StateName.IdleState);
                return;
            }

            // 지쳤으면 공격 안 고름 (물약 반응·돌기 뒤에 와도) → 대기에서 쉼
            if (boss.IsExhausted)
            {
                boss.ChangeState(StateName.IdleState);
                return;
            }

            Debug.Log("Entered DecideState");
            GetInfo(out float distance, out float angle);
            Debug.Log($"Distance: {distance}");
            Debug.Log($"Angle: {angle}");
            
            List<Pattern> patternList = new List<Pattern>();
            List<Pattern> tempPatternList = new List<Pattern>();

            foreach (Pattern pattern in AllPatterns)
            {
                if (IsInRange(pattern, distance, angle)) patternList.Add(pattern);
            }
            
            Debug.Log($"Found {patternList.Count} patterns");

            // 지난번과 다른 후보가 있으면 그것만, 지난번 것뿐이면 연속 허용
            foreach (Pattern pattern in patternList)
                if (pattern.PatternName != lastState) tempPatternList.Add(pattern);

            List<Pattern> pickList = tempPatternList.Count >= 1 ? tempPatternList : patternList;

            if (pickList.Count >= 1)
            {
                int result = UnityEngine.Random.Range(0, pickList.Count);
                Pattern pattern = pickList[result];
                lastState = pattern.PatternName;
                if (pattern.FarDistancePattern) detect.ResetFarTime(); // 도망 한 번 → 불덩이 한 번
                boss.ChangeState(pattern.PatternName);
            }
            else boss.ChangeState(StateName.ChaseState);
            
        }

        private void GetInfo(out float distance, out float angle)
        {
            distance = boss.transform.FlatDistanceTo(boss.Player.transform);
            angle = Vector3.SignedAngle(boss.transform.forward, boss.transform.FlatDirectionTo(boss.Player.transform), Vector3.up);
        }

        public bool CanAttack()
        {
            GetInfo(out float distance, out float angle);
            foreach (Pattern p in AllPatterns)
            {
                if (IsInRange(p, distance, angle))
                    return true;
            }
            return false;
        }

        private bool IsInRange(Pattern pattern, float distance, float angle)
        {
            if (pattern.FarDistancePattern && detect.IsPlayerMovingAway == false) return false;
            return (pattern.MinDistance <= distance && pattern.MaxDistance >= distance &&
                    pattern.MinAngle <= angle && pattern.MaxAngle >= angle);
        }
    }
}