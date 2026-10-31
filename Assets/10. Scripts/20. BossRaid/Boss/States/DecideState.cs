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
            
            public StateName PatternName => patternName;
            public float MinDistance => minDistance;
            public float MaxDistance => maxDistance;
            public float MinAngle => minAngle;
            public float MaxAngle => maxAngle;
            public bool FarDistancePattern => farDistancePattern;
        }
        
        [SerializeField] private Pattern[] patterns;
        [SerializeField] private DetectDistance detect;
        private StateName lastState;
        
        public Pattern[] Patterns => patterns;
        
        
        public override StateName Name => StateName.DecideState;
        
        
        public override void Enter()
        {
            if (boss.Player == null)
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

            foreach (Pattern pattern in patterns)
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
            foreach (Pattern p in patterns)
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