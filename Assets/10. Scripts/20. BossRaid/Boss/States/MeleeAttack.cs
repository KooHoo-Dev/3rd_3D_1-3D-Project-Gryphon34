using Study.Utilities;
using UnityEngine;

namespace BossRaid.Boss.States
{
    // 근접 공격 공통: 히트박스 전까지 플레이어 쪽으로 회전, parameter와 같은 id의 히트박스를 켜고 끕니다
    public abstract class MeleeAttack : BossStates
    {
        [SerializeField] private float rotationSpeed = 120.0f;

        private BossHitbox[] hitboxes;
        private bool isRotating = false;

        public override void Enter()
        {
            isRotating = true;
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
        }

        // 애니 이벤트의 parameter(L_Hand, R_Hand)와 id가 같은 히트박스만 켜고 끕니다
        public override void ReceiveAnimEvent(string eventName, string parameter)
        {
            if(eventName == "HitboxOn")
            {
                isRotating = false;
                SetHitbox(parameter, true);
            }
            if(eventName == "HitboxOff") SetHitbox(parameter, false);
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
