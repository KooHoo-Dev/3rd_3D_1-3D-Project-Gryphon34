// ─────────────────────────────────────────────
// AnimStateEventSender — 애니메이터 상태의 진행률에 맞춰 신호를 보내는 StateMachineBehaviour
// 학습 포인트
//  - 상용 FBX의 애니메이션 이벤트를 고치지 않고도, 상태 진행률(normalizedTime)로 '재장전 끝' 같은 시점을 알릴 수 있습니다
//  - 상태에 들어올 때 IsComplete를 지워, 같은 신호가 한 번 재생에 한 번만 나가게 합니다
// 조작
//  - 없음
// ─────────────────────────────────────────────
using System;
using UnityEngine;

namespace BossRaid
{
    public class AnimStateEventSender : StateMachineBehaviour
    {
        [Serializable]
        public class AnimStateEvent
        {
            [SerializeField] private string name;
            [SerializeField] [Range(0f, 1f)] private float time;
            [SerializeField] private string parameter;

            public string Name => name;
            public float NormalizedTime => time;
            public string Parameter => parameter;
            public bool IsComplete { get; set; }

            public void SetTime(float newTime)
            {
                time = Mathf.Clamp01(newTime);
            }
        }

        [Header("상태 진행률(0~1)에 맞춰 보낼 신호")]
        [SerializeField] private AnimStateEvent[] events;

        [Tooltip("켜면 칸이 한 바퀴 돌 때마다 신호를 다시 보냄 (반복 클립·블렌드 트리. 예: 발소리)")]
        [SerializeField] private bool repeatEveryLoop;

        private AnimStateEventListener eventListener;
        private int currentLoop; // 지금 몇 바퀴째인지 (normalizedTime의 정수 부분)

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // 상태에 들어올 때마다 지난 재생에서 보낸 기록을 지웁니다
            for (int i = 0; i < events.Length; i++)
            {
                events[i].IsComplete = false;
            }
            currentLoop = Mathf.FloorToInt(stateInfo.normalizedTime);

            if (eventListener == null) eventListener = animator.GetComponent<AnimStateEventListener>();
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            float time = stateInfo.normalizedTime;

            if (repeatEveryLoop)
            {
                int loop = Mathf.FloorToInt(time);
                if (loop != currentLoop)
                {
                    // 바퀴가 넘어감: 프레임이 경계를 건너뛰어 지난 바퀴에서 못 보낸 것을 먼저 보내고 기록을 지움
                    SendPassedEvents(1f);
                    for (int i = 0; i < events.Length; i++)
                    {
                        events[i].IsComplete = false;
                    }
                    currentLoop = loop;
                }
                time -= loop; // 이번 바퀴 안에서의 진행률 (0~1)
            }

            SendPassedEvents(time);
        }

        // 진행률이 time을 지났는데 아직 안 보낸 신호를 보냅니다
        private void SendPassedEvents(float time)
        {
            for (int i = 0; i < events.Length; i++)
            {
                var eventData = events[i];
                if (eventData.IsComplete) continue;
                if (eventData.NormalizedTime > time) continue; // 아직 보낼 시점이 아님

                eventData.IsComplete = true;
                eventListener.OccursAnimStateEvent(eventData.Name, eventData.Parameter);
            }
        }

        public bool TryGetAnimStateEventByName(string targetName, out AnimStateEvent animStateEvent)
        {
            
            for (int i = 0; i < events.Length; i++)
            {
                if(events[i].Name.Equals(targetName) == false) continue;
                animStateEvent = events[i];
                return true;
            }

            animStateEvent = null;
            return false;
        }
    }
}
