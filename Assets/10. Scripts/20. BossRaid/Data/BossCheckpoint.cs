using System;
using System.Collections.Generic;

namespace BossRaid.Data
{
    // 죽어도 이어서 싸우는 체크포인트 (몬헌 기회처럼). 보스 HP 구간(75/50/25%)을 넘은 채로 죽으면 그 HP에서 다시 시작
    // 다시 싸우기를 다 쓰고 또 죽거나, 보스를 잡거나, 다른 단계에 들어가면 비움
    [Serializable]
    public class BossCheckpoint
    {
        public int stage;       // 체크포인트 단계 (0 = 없음). 같은 단계 투기장에서만 이어짐
        public int hpCount;     // 넘은 체력 구간 수 (HuntManager.givenHpCount). 보스 HP = MaxHp × (1 − hpCount × HpRate)
        public int deathCount;  // 이 체크포인트로 죽은 횟수. HuntManager의 maxRetry를 넘으면 비움
        public List<string> brokenParts = new(); // 부서진 부위 이름 (BossPart.PartName). 다시 들어가도 부서진 채로

        public bool IsActive => stage > 0;

        public void Clear()
        {
            stage = 0;
            hpCount = 0;
            deathCount = 0;
            brokenParts.Clear();
        }
    }
}
