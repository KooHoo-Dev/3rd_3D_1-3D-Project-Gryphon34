using UnityEngine;
using UnityEngine.Serialization;

namespace BossRaid.Boss.States
{
    // 공격 공통: 히트박스 전까지 플레이어 쪽으로 회전, parameter와 같은 id의 히트박스를 켜고 끕니다
    public abstract class Attack : BossStates
    {
        [SerializeField, FormerlySerializedAs("rotationSpeed")]
        private float minRotationSpeed = 120.0f; // 거의 마주볼 때 회전 속도 (예전 rotationSpeed 값 그대로)
        [SerializeField] private float maxRotationSpeed; // 등 뒤(180도)일 때 회전 속도. minRotationSpeed보다 작으면(0) 늘 min 속도
        [SerializeField] private float recoveryTime = 2.5f; // 이 공격이 끝난 뒤 IdleState에서 기다리는 시간 (큰 공격일수록 길게)
        [SerializeField] private float windupSpeed = 1f;    // 시작 ~ 첫 HitboxOn까지 애니메이터 AttackSpeed (1 = 안 바꿈). 칸 Speed Multiplier가 AttackSpeed여야 느려짐

        // 칸 이벤트 Sound(이름) → 같은 이름의 클립 중 하나를 무작위로
        [System.Serializable]
        private class AttackSound
        {
            public string name;
            public AudioClip[] clips;
        }

        // 칸 이벤트 Effect(이름) → 같은 이름의 파티클을 Play (자리는 놓아 둔 그대로)
        [System.Serializable]
        private class AttackEffect
        {
            public string name;
            public ParticleSystem effect; // 상태 오브젝트 자식 말고 보스 루트·뼈 아래 (상태가 꺼지면 같이 꺼짐)
        }

        [Header("소리 (크기·피치·2D/3D는 AudioSource에서)")]
        [SerializeField] private AudioSource attackSource; // 항상 켜진 오브젝트의 AudioSource (상태 오브젝트는 꺼지면 소리가 끊김)
        [SerializeField] private AttackSound[] sounds;

        [Header("이펙트")]
        [SerializeField] private AttackEffect[] effects;

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
            if (isRotating) RotateToPlayer(minRotationSpeed, maxRotationSpeed);
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
            if (eventName == "Sound") PlaySound(parameter);
            if (eventName == "Effect") PlayEffect(parameter);
        }

        // Sound 이벤트 말고 코드에서도 부름 (브레스·불덩이·불줄기)
        protected void PlaySound(string soundName)
        {
            foreach (AttackSound sound in sounds)
            {
                if (sound.name != soundName) continue;
                if (attackSource == null || sound.clips == null || sound.clips.Length == 0) return;

                AudioClip clip = sound.clips[Random.Range(0, sound.clips.Length)];
                if (clip != null) attackSource.PlayOneShot(clip);
                return;
            }
            Debug.LogWarning($"[Boss] {name}: 소리 목록에 '{soundName}' 없음");
        }

        private void PlayEffect(string effectName)
        {
            foreach (AttackEffect entry in effects)
            {
                if (entry.name != effectName) continue;
                if (entry.effect != null) entry.effect.Play();
                return;
            }
            Debug.LogWarning($"[Boss] {name}: 이펙트 목록에 '{effectName}' 없음");
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
