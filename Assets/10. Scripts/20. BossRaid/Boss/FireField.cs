using System.Collections.Generic;
using BossRaid.Player;
using UnityEngine;

namespace BossRaid.Boss
{
    // 화염 장판: 위에 있는 동안 계속 화염 상태 (직격·피격 모션 없음). lifeTime이 지나면 불이 사그라들고 지워짐
    // 들어오고 나간 것을 목록으로 들고 Update에서 붙임 (가만히 서 있으면 Rigidbody가 잠들어 OnTriggerStay가 안 불릴 수 있어서)
    // 플레이어 몸 콜라이더(PlayerBurn과 같은 오브젝트)만 셈. 칼·상쇄 상자 같은 자식 트리거가 나갈 때 목록에서 빠지지 않게
    [RequireComponent(typeof(Collider))]
    public class FireField : MonoBehaviour
    {
        [SerializeField] private float lifeTime;
        [SerializeField] private float fadeTime = 1f;           // 다 된 뒤 남은 불 입자가 사라질 때까지 기다리는 시간
        [SerializeField] private ParticleSystem[] fireEffects;  // 끝날 때 새 입자만 멈춤

        private readonly HashSet<PlayerBurn> burns = new HashSet<PlayerBurn>();
        private Collider fieldCollider;
        private float currentTime;
        private bool isEnded;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void Awake()
        {
            fieldCollider = GetComponent<Collider>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out PlayerBurn burn)) burns.Add(burn);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out PlayerBurn burn)) burns.Remove(burn);
        }

        private void Update()
        {
            if (isEnded) return;

            foreach (PlayerBurn burn in burns) burn.Ignite(); // 안에 있는 동안은 시간·구르기 횟수가 계속 처음으로

            currentTime += Time.deltaTime;
            if (currentTime >= lifeTime) End();
        }

        private void End()
        {
            isEnded = true;
            fieldCollider.enabled = false;
            burns.Clear();
            if (fireEffects != null)
                foreach (ParticleSystem effect in fireEffects) effect.Stop();
            Destroy(gameObject, fadeTime);
        }
    }
}
