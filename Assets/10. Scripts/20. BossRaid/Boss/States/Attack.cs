using Study.Utilities;
using UnityEngine;

namespace BossRaid.Boss.States
{
    // 공격 공통: 히트박스 전까지 플레이어 쪽으로 회전, parameter와 같은 id의 히트박스를 켜고 끕니다
    public abstract class Attack : BossStates
    {
        [SerializeField] private float rotationSpeed = 120.0f;
        [SerializeField] private float recoveryTime = 2.5f; // 이 공격이 끝난 뒤 IdleState에서 기다리는 시간 (큰 공격일수록 길게)
        [SerializeField] private float windupSpeed = 1f;    // 시작 ~ 첫 HitboxOn까지 애니메이터 AttackSpeed (1 = 안 바꿈). 칸 Speed Multiplier가 AttackSpeed여야 느려짐

        public float RecoveryTime => recoveryTime;

        private BossHitbox[] hitboxes;
        private bool isRotating = false;
        private bool isWindupSlow; // AttackSpeed를 낮춰 둠 → HitboxOn·Exit에서 1로

        public override void Enter()
        {
            isRotating = true;
            if (windupSpeed != 1f) SetWindupSlow(true);
        }

        protected override void Update()
        {
            base.Update();
            if (isRotating)
            {
                boss.transform.rotation =
                    Quaternion.RotateTowards(boss.transform.rotation,
                        boss.transform.FlatRotationTo(boss.Player.transform),
                        rotationSpeed * Time.deltaTime);
            }
        }

        public override void Initialize(Boss boss)
        {
            base.Initialize(boss);
            hitboxes = boss.GetComponentsInChildren<BossHitbox>(true);
            SetAllHitboxes(false);
        }

        public override void Exit()
        {
            SetAllHitboxes(false);
            SetWindupSlow(false); // 판정 전에 끊겨도 (지침·죽음) 다음 공격이 느린 채로 시작하지 않게
        }

        // 애니 이벤트의 parameter(L_Hand, R_Hand)와 id가 같은 히트박스만 켜고 끕니다
        public override void ReceiveAnimEvent(string eventName, string parameter)
        {
            if(eventName == "HitboxOn")
            {
                isRotating = false;
                SetWindupSlow(false); // 예비동작 끝 → 휘두르기는 원래 속도
                SetHitbox(parameter, true);
            }
            if(eventName == "HitboxOff") SetHitbox(parameter, false);
            if (eventName == "RotateOff") isRotating = false;
            if (eventName == "RotateOn") isRotating = true;
        }

        // 예비동작만 느리게 (브레스 SpeedBreath, 철산고 HipWaitSpeed와 같은 방식). windupSpeed 1인 공격은 파라미터를 안 건드림
        private void SetWindupSlow(bool slow)
        {
            if (slow == isWindupSlow) return;
            isWindupSlow = slow;
            animator.SetFloat("AttackSpeed", slow ? windupSpeed : 1f);
        }

        // 자식이 플레이어 쪽 회전을 다시 켜고 끔 (공중 착지 때)
        protected void SetRotating(bool rotating)
        {
            isRotating = rotating;
        }

        private void SetHitbox(string id, bool active)
        {
            foreach (BossHitbox hitbox in hitboxes)
            {
                if (hitbox.id == id) hitbox.gameObject.SetActive(active);
            }
        }

        private void SetAllHitboxes(bool active)
        {
            foreach (BossHitbox hitbox in hitboxes) hitbox.gameObject.SetActive(active);
        }
    }
}
