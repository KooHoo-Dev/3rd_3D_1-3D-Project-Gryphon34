using Study.Utilities;
using UnityEngine;

namespace BossRaid.Boss.States
{
    public class HipCheckState:Attack
    {
        public override StateName Name => StateName.HipCheckState;

        public override string AnimatorTrigger
        {
            get
            {
                float singed = Vector3.Dot(boss.transform.right, boss.transform.FlatDirectionTo(boss.Player.transform));

                if (singed < 0f)
                    return "HipCheckLeft";
                else
                    return "HipCheckRight";

            }
        }

        [SerializeField] private float waitTime = 0.5f;
        [SerializeField] private float waitSpeed = 0.1f; // 멈춰 있는 동안의 속도 (0이면 얼음처럼 멈춤)
        [SerializeField] private float slowDownTime = 0.2f; // 1에서 waitSpeed까지 줄어드는 시간

        private bool isWaiting = false;
        private float currentWaitTime = 0f;
        private float currentSpeed = 1f;

        public override void Enter()
        {
            base.Enter();
            currentSpeed = 1f;
            animator.SetFloat("HipWaitSpeed", currentSpeed);
            isWaiting = false;
            currentWaitTime = 0f;
        }

        protected override void Update()
        {
            base.Update();
            if (isWaiting)
            {
                // 뚝 멈추지 않고 slowDownTime 동안 waitSpeed까지 감속
                float slowDownAmount = (1f - waitSpeed) / slowDownTime;
                currentSpeed = Mathf.MoveTowards(currentSpeed, waitSpeed, slowDownAmount * Time.deltaTime);
                animator.SetFloat("HipWaitSpeed", currentSpeed);

                currentWaitTime += Time.deltaTime;
                if (currentWaitTime >= waitTime)
                {
                    isWaiting = false;
                    animator.SetTrigger("HipCheckGo");
                }
            }
        }

        // 준비 칸의 HitWait 이벤트: 한 발 빠진 자세에서 멈추고 시간을 재기 시작
        public override void ReceiveAnimEvent(string eventName, string parameter)
        {
            base.ReceiveAnimEvent(eventName, parameter);
            if (eventName == "HitWait") isWaiting = true; // 감속은 Update에서
        }

        public override void Exit()
        {
            base.Exit();
            animator.SetFloat("HipWaitSpeed", 1f); // 멈춘 채 끊겨도 다음에 멈춰 있지 않게
        }
    }
}