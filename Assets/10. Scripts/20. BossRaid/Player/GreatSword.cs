using System.Collections.Generic;
using BossRaid.Player;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace BossRaid.Weapon
{
    // "무기 공격력 x 차지 배율"을 계산해서 콤보 콜라이더들에 넘겨주는 대검
    // 동작마다 다른 값(모션값, 모션 피로도)은 각 WeaponCollider가 들고 있습니다.
    public class GreatSword : MonoBehaviour
    {
        [SerializeField] private float attackPower = 100f;
        [SerializeField] private float hitStopTime = 0.1f;
        [SerializeField] private WeaponCollider[] weaponColliders;

        [Header("타격 (비어 있으면 안 함)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField, FormerlySerializedAs("hitSounds")]
        private AudioClip[] attackSounds;
        [SerializeField] private AudioClip[] impactSounds;
        [SerializeField] private TrailRenderer slashTrail;
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private float hitEffectLifeTime = 2f;

        [Header("카메라 흔들림 (맞힌 순간)")]
        [SerializeField] private float shakeStrength = 0.15f;
        [SerializeField] private float shakeTime = 0.2f;

        [Header("차지 빛 (아웃라인, 칸 순서 = 1·2·3단계)")]
        [SerializeField] private Material chargeOutlineMaterial;
        [SerializeField] private Transform bodyRoot;
        [SerializeField] private Color[] chargeColors = { Color.white, new Color(1f, 0.8f, 0.3f), new Color(1f, 0.35f, 0.1f) };
        [SerializeField] private float[] chargeGlows = { 5f, 15f, 40f };
        [SerializeField] private float[] chargeThickness = { 0.004f, 0.006f, 0.009f };
        [SerializeField] private float flashGlowMultiplier = 3f;
        [SerializeField] private float flashTime = 0.15f;
        [SerializeField] private AudioClip[] chargeLevelUpSounds;

        private static readonly int OutlineColorId = Shader.PropertyToID("_Outline_Color");
        private static readonly int GlowIntensityId = Shader.PropertyToID("_Glow_Intensity");
        private static readonly int OutlineThicknessId = Shader.PropertyToID("_Outline_Thickness");

        private Material outlineInstance;
        private Renderer[] glowRenderers;
        private Material[][] originalMaterials;

        public float HitStopTime => hitStopTime;
        public float ShakeStrength => shakeStrength;
        public float ShakeTime => shakeTime;

        /// <summary>
        /// 모은 시간이 몇 단계인지 (0~3). ChargeState가 매 프레임 단계가 올랐는지 볼 때 씁니다.
        /// </summary>
        public int GetChargeLevel(float time)
        {
            return chargeTable.Get(time).Level;
        }

        /// <summary>
        /// 차지 단계가 오른 순간: 몸에 아웃라인(처음이면 붙이기) → 이 단계 색·두께, 세기는 번쩍했다가 이 단계 값으로 + "칭". ChargeState가 부릅니다.
        /// </summary>
        public void ShowChargeLevel(int level)
        {
            int index = level - 1; // 칸은 1단계부터
            if (index < 0) return;

            AudioClip sound = GetAt(chargeLevelUpSounds, index);
            if (sound != null && audioSource != null) audioSource.PlayOneShot(sound);

            if (outlineInstance == null) return;
            if (glowRenderers == null) AttachOutline();

            float glow = GetAt(chargeGlows, index, 0f);
            outlineInstance.SetColor(OutlineColorId, GetAt(chargeColors, index, Color.white));
            outlineInstance.SetFloat(OutlineThicknessId, GetAt(chargeThickness, index, 0f));

            outlineInstance.DOKill();
            outlineInstance.SetFloat(GlowIntensityId, glow * flashGlowMultiplier);
            outlineInstance.DOFloat(glow, GlowIntensityId, flashTime);
        }

        /// <summary>
        /// 차지 빛을 끕니다 (아웃라인 떼기). ChargeState.Exit(떼서 휘두르기, 구르기·피격으로 끊김)에서 부릅니다.
        /// </summary>
        public void StopChargeEffect()
        {
            if (outlineInstance != null) outlineInstance.DOKill();
            if (glowRenderers == null) return;

            for (int i = 0; i < glowRenderers.Length; i++)
            {
                if (glowRenderers[i] != null) glowRenderers[i].sharedMaterials = originalMaterials[i];
            }
            glowRenderers = null;
            originalMaterials = null;
        }

        // 몸의 켜져 있는 렌더러(조립식 파츠 중 보이는 것만)에 머티리얼을 하나 더 붙임 → 원래 모습 + 아웃라인 두 번 그려짐
        private void AttachOutline()
        {
            List<Renderer> list = new List<Renderer>();
            foreach (Renderer r in bodyRoot.GetComponentsInChildren<Renderer>())
            {
                if (r.enabled == false) continue;
                if (r is not SkinnedMeshRenderer && r is not MeshRenderer) continue; // 파티클·트레일 등은 뺌
                list.Add(r);
            }

            glowRenderers = list.ToArray();
            originalMaterials = new Material[glowRenderers.Length][];
            for (int i = 0; i < glowRenderers.Length; i++)
            {
                Material[] original = glowRenderers[i].sharedMaterials;
                originalMaterials[i] = original;

                Material[] withOutline = new Material[original.Length + 1];
                original.CopyTo(withOutline, 0);
                withOutline[original.Length] = outlineInstance;
                glowRenderers[i].sharedMaterials = withOutline; // sharedMaterials라 원래 머티리얼은 복사되지 않음
            }
        }

        // 배열 중 하나를 무작위로 (같은 소리 반복이 덜 거슬림). 칸이 비어 있으면 안 함
        private void PlayRandomSound(AudioClip[] clips)
        {
            if (audioSource == null || clips == null || clips.Length == 0) return;
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip != null) audioSource.PlayOneShot(clip);
        }

        private static T GetAt<T>(T[] array, int index, T fallback = default)
        {
            if (array == null || index < 0 || index >= array.Length) return fallback;
            return array[index];
        }

        private void OnDestroy()
        {
            if (outlineInstance == null) return;
            outlineInstance.DOKill();
            Destroy(outlineInstance); // new Material로 만든 복사본은 직접 지워야 함
        }

        /// <summary>
        /// 공격 소리를 냅니다. AttackState.Enter(버튼을 떼서 휘두르기 시작)에서 부릅니다. 맞을 때는 소리 없음
        /// </summary>
        public void PlayAttackSound()
        {
            PlayRandomSound(attackSounds);
        }

        /// <summary>
        /// 칼끝 잔상을 그리기 시작합니다. AttackState.Enter에서 부릅니다.
        /// </summary>
        public void StartTrail()
        {
            if (slashTrail == null) return;
            slashTrail.Clear(); // 지난 공격에서 남은 잔상 지우기 (안 지우면 자세 잡을 때 칼이 움직인 길까지 이어져 그려짐)
            slashTrail.emitting = true;
        }

        /// <summary>
        /// 칼끝 잔상을 그만 그립니다 (이미 그린 건 Time 동안 서서히 사라짐). AttackState.Exit에서 부릅니다.
        /// </summary>
        public void StopTrail()
        {
            if (slashTrail != null) slashTrail.emitting = false;
        }

        /// <summary>
        /// 맞은 자리에 이펙트 + 맞는 소리를 냅니다. WeaponCollider가 보스를 맞힌 순간 부릅니다.
        /// </summary>
        public void PlayHitEffect(Vector3 hitPoint, Vector3 hitDirection)
        {
            PlayRandomSound(impactSounds);

            if (hitEffectPrefab != null)
            {
                Quaternion rotation = hitDirection.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(hitDirection) : Quaternion.identity;
                GameObject effect = Instantiate(hitEffectPrefab, hitPoint, rotation);
                // 에셋 이펙트 중엔 Play On Awake가 꺼진 것이 있어서(FX_BloodSplat_01) 만든 직후 직접 재생
                foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>()) particle.Play();
                Destroy(effect, hitEffectLifeTime);
            }
        }

        private ChargeTable chargeTable;

        private void Awake()
        {
            chargeTable = new ChargeTable();
            chargeTable.Load();

            if (weaponColliders == null || weaponColliders.Length == 0)
            {
                weaponColliders = GetComponentsInChildren<WeaponCollider>(true);
            }

            ApplyCharge(0f);

            if (bodyRoot == null)
            {
                Animator playerAnimator = GetComponentInParent<Animator>();
                bodyRoot = playerAnimator != null ? playerAnimator.transform : transform;
            }
            if (chargeOutlineMaterial != null) outlineInstance = new Material(chargeOutlineMaterial);
            StopTrail(); // 걸어다닐 때는 안 그림
        }

        /// <summary>
        /// 차지 시간으로 단계를 찾아서 모든 콤보 콜라이더에 값을 넘겨줍니다. ChargeState.Exit에서 호출합니다.
        /// </summary>
        public void ApplyCharge(float time)
        {
            ChargeValue value = chargeTable.Get(time);

            // 필드(attackPower)는 건드리지 않고 지역 변수로 계산한다
            float damage = attackPower * value.Damage;

            foreach (WeaponCollider weaponCollider in weaponColliders)
            {
                weaponCollider.SetAttackValue(damage, value.Fatigue);
            }

            Debug.Log($"[GreatSword] 차지 {time:F2}초 → Level {value.Level} (데미지 x{value.Damage}, 피로도 x{value.Fatigue})");
        }

        /// <summary>
        /// 모든 콤보 콜라이더의 "이미 때린 대상" 목록을 비웁니다. AttackState.Enter에서 호출합니다.
        /// </summary>
        public void ResetHash()
        {
            foreach (WeaponCollider weaponCollider in weaponColliders)
            {
                weaponCollider.ResetHash();
            }
        }
    }
}
