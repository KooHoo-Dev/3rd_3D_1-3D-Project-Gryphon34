using UnityEngine;

namespace BossRaid.Boss.States
{
    public class Breath : Attack
    {
        public override StateName Name => StateName.BreathState;
        
        public override string AnimatorTrigger => "BreathAttack";

        [SerializeField] private ParticleSystem flame;
        [SerializeField] private ParticleSystem ember;
        [SerializeField] private float speedBreath = 0.5f;
        [SerializeField] private CapsuleCollider flameCollider;
        [SerializeField] private float flameGrowSpeed = 25f;
        [SerializeField] private AudioSource breathSource; // Boss.audioSource를 빌리지 않고 이 오브젝트의 AudioSource로
        [SerializeField] private AudioClip breathClip;

        private bool isStretch = false;
        private float flameMaxLength;
        private float flameStartZ;

        public override void Initialize(Boss boss)
        {
            base.Initialize(boss);
            flameMaxLength = flameCollider.height;
            flameStartZ = flameCollider.center.z - flameCollider.height / 2;
        }

        public override void Enter()
        {
            base.Enter();
            animator.SetFloat("SpeedBreath", speedBreath);
            isStretch = false;
            SetFlameLength(0);
        }

        protected override void Update()
        {
            base.Update();
            if (isStretch)
            {
                float newLength = flameCollider.height + flameGrowSpeed * Time.deltaTime;
                newLength = Mathf.Min(newLength, flameMaxLength);
                SetFlameLength(newLength);
            }
        }
        
        public override void ReceiveAnimEvent(string eventName, string parameter)
        {
            base.ReceiveAnimEvent(eventName, parameter);

            ParticleSystem effect = GetEffect(parameter);
            if (effect == null) return;

            if (eventName == "EffectOn")
            {
                if(parameter == "Flame")
                {
                    animator.SetFloat("SpeedBreath", 1f);
                    isStretch = true;
                    PlayBreathSound();
                }
                effect.Play();
            }
            if (eventName == "EffectOff") effect.Stop();
        }

        public override void Exit()
        {
            base.Exit();
            flame.Stop();
            ember.Stop();
        }

        private ParticleSystem GetEffect(string id)
        {
            if (id == "Ember") return ember;
            if (id == "Flame") return flame;
            return null;
        }

        private void PlayBreathSound()
        {
            if (breathSource == null || breathClip == null) return;
            breathSource.PlayOneShot(breathClip);
        }

        private void SetFlameLength(float length)
        {
            flameCollider.height = length;
            Vector3 c = flameCollider.center;
            c.z = flameStartZ + length / 2;
            flameCollider.center = c;
        }
    }
}