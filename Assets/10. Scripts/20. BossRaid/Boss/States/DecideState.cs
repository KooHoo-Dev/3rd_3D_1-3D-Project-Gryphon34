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
            
            public StateName PatternName => patternName;
            public float MinDistance => minDistance;
            public float MaxDistance => maxDistance;
        }
        
        [SerializeField] private Pattern[] patterns;
        public Pattern[] Patterns => patterns; // Boss의 거리 기즈모가 읽습니다
        
        public override StateName Name => StateName.DecideState;
        
        
        public override void Enter()
        {
            if (boss.Player == null)
            {
                boss.ChangeState(StateName.IdleState);
                return;
            }

            Debug.Log("Entered DecideState");
            float distance = boss.transform.FlatDistanceTo(boss.Player.transform);
            Debug.Log($"Distance: {distance}");

            List<Pattern> patternList = new List<Pattern>();
            
            foreach (Pattern pattern in patterns)
            {
                if (pattern.MinDistance <= distance && pattern.MaxDistance >= distance)
                {
                    patternList.Add(pattern);
                }
            }
            Debug.Log($"Found {patternList.Count} patterns");

            if (patternList.Count >= 1)
            {
                int result = UnityEngine.Random.Range(0, patternList.Count);
                Pattern pattern = patternList[result];
                boss.ChangeState(pattern.PatternName);
            }
            else boss.ChangeState(StateName.ChaseState);
            
        }

        public bool CanAttack(float distance)
        {
            foreach (Pattern p in patterns)
            {
                if (p.MinDistance <= distance && p.MaxDistance >= distance)
                    return true;
            }
            return false;
        }
    }
}