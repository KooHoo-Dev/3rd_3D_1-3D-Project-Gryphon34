using UnityEngine;

namespace BossRaid.Boss.States
{
    // 공중 흩뿌리기: 떠서 입으로 불줄기(Fire 히트박스, HitboxOn/Off) + FireField 이벤트마다 플레이어 발밑에 화염 장판 → attackCount번 뒤 착지
    public class AirSpreadFire : FlyAttack
    {
        public override StateName Name => StateName.AirSpreadFireState;

        protected override string AttackTrigger => "AirSpreadFire";

        [SerializeField] private FireField fireFieldPrefab;
        [SerializeField] private ParticleSystem flame;    // 불줄기 (EffectOn/EffectOff)
        [SerializeField] private AudioSource flameSource; // 항상 켜진 오브젝트의 AudioSource (상태 오브젝트는 꺼지면 소리가 끊김)
        [SerializeField] private AudioClip flameClip;

        public override void ReceiveAnimEvent(string eventName, string parameter)
        {
            base.ReceiveAnimEvent(eventName, parameter);

            if (eventName == "EffectOn")
            {
                if (flame != null) flame.Play();
                if (flameSource != null && flameClip != null) flameSource.PlayOneShot(flameClip);
            }
            if (eventName == "EffectOff" && flame != null) flame.Stop();

            if (eventName == "FireField")
            {
                Instantiate(fireFieldPrefab, boss.Player.transform.position, Quaternion.identity);
                CountAttack();
            }
        }

        public override void Exit()
        {
            base.Exit();
            if (flame != null) flame.Stop();
        }
    }
}
