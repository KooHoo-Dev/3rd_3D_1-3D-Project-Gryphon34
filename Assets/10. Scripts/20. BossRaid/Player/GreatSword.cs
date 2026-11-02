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
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private float hitEffectLifeTime = 2f;

        // 슬래시 하나 = 이름 + 프리팹 + 플레이어 기준 위치·회전. 공격 칸의 이벤트 Slash가 파라미터(이름)로 고름
        [System.Serializable]
        public class SlashSetting
        {
            public string name;
            public GameObject prefab;
            public Vector3 position; // 플레이어 기준 (0, 1, 1) = 앞 1m, 위 1m
            public Vector3 rotation; // 플레이어 기준 각도 (Euler)
        }

        [Header("칼 슬래시 (칸 이벤트 Slash, 파라미터 = 이름)")]
        [SerializeField] private SlashSetting[] slashes;
        [SerializeField] private float slashLifeTime = 1f;

        [Header("퍼펙트 가드 (비어 있으면 안 함)")]
        [SerializeField] private GameObject perfectGuardEffectPrefab;
        [SerializeField] private AudioClip[] perfectGuardSounds;

        [Header("일반 가드 (비어 있으면 안 함, 불 공격도 같은 것)")]
        [SerializeField] private GameObject guardEffectPrefab;
        [SerializeField] private AudioClip[] guardSounds;

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
        private AnimStateEventListener animEventListener; // 공격 칸의 Slash 이벤트를 받음
        private Transform slashRoot;                      // 슬래시 위치·회전의 기준 (플레이어)

        public float HitStopTime => hitStopTime;
        public float ShakeStrength => shakeStrength;
        public float ShakeTime => shakeTime;

        private float weaponAttackRate = 1f; // 끼고 있는 무기의 공격력 배율 (기본 attackPower에 곱함). PlayerLoadout이 넣어 줌
        private float brokenPartBonus = 1f;  // 깨진 부위에 더 곱하는 배율 (용뿔 대검). PlayerLoadout이 넣어 줌
        private float chargeTimeRate = 1f;   // 차지 단계까지 걸리는 시간 배율 (0.8 = 20% 빨리, 요리). PlayerLoadout이 넣어 줌

        public float BaseAttackPower => attackPower; // 무기 배율을 곱하기 전 (대장간 화면에서 실제 공격력을 보여 줄 때)
        public float BrokenPartBonus => brokenPartBonus;

        /// <summary>
        /// 끼고 있는 무기의 값으로 바꿉니다 (attackPower는 그대로, 배율만). PlayerLoadout이 부릅니다.
        /// </summary>
        public void SetWeapon(float attackRate, float brokenPartBonus)
        {
            weaponAttackRate = attackRate;
            this.brokenPartBonus = brokenPartBonus;
        }

        /// <summary>
        /// 차지 단계까지 걸리는 시간 배율 (1 = 그대로). PlayerLoadout이 부릅니다.
        /// </summary>
        public void SetChargeTimeRate(float rate)
        {
            chargeTimeRate = rate;
        }

        /// <summary>
        /// 모은 시간이 몇 단계인지 (0~3). ChargeState가 매 프레임 단계가 올랐는지 볼 때 씁니다.
        /// </summary>
        public int GetChargeLevel(float time)
        {
            return chargeTable.Get(time / chargeTimeRate).Level; // 0.8배면 0.8초 모은 게 표의 1초
        }

        /// <summary>
        /// 이 단계가 시작되는 차지 시간. 상쇄 보너스로 차지를 그 단계부터 셀 때 씁니다.
        /// </summary>
        public float GetChargeTime(int level)
        {
            return chargeTable.GetTime(level) * chargeTimeRate;
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

        private void OnEnable()
        {
            if (animEventListener != null) animEventListener.OnOccursAnimStateEvent += HandleAnimStateEvent;
        }

        private void OnDisable()
        {
            if (animEventListener != null) animEventListener.OnOccursAnimStateEvent -= HandleAnimStateEvent;
        }

        private void HandleAnimStateEvent(string eventName, string parameter)
        {
            if (eventName == "Slash") PlaySlash(parameter);
        }

        /// <summary>
        /// 이름이 같은 슬래시를 플레이어 기준 위치·회전에 만듭니다 (월드에 남음). 공격 칸의 이벤트 Slash가 부릅니다.
        /// 값은 매번 목록에서 읽어서, 플레이 중에 고치면 다음 공격부터 바로 보입니다
        /// </summary>
        private void PlaySlash(string slashName)
        {
            foreach (SlashSetting slash in slashes)
            {
                if (slash.name != slashName) continue;
                if (slash.prefab == null) return;

                Vector3 position = slashRoot.TransformPoint(slash.position);
                Quaternion rotation = slashRoot.rotation * Quaternion.Euler(slash.rotation);
                GameObject effect = Instantiate(slash.prefab, position, rotation);
                foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>()) particle.Play();
                Destroy(effect, slashLifeTime);
                return;
            }
            Debug.LogWarning($"[GreatSword] 슬래시 '{slashName}'가 목록에 없습니다");
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

        /// <summary>
        /// 퍼펙트 가드 자리에 불꽃 + 금속음을 냅니다. PlayerController.TryGuard가 퍼펙트로 막은 순간 부릅니다.
        /// </summary>
        public void PlayPerfectGuardEffect(Vector3 point)
        {
            PlayRandomSound(perfectGuardSounds);
            SpawnEffect(perfectGuardEffectPrefab, point);
        }

        /// <summary>
        /// 일반 가드 자리에 작은 불꽃 + 막는 소리를 냅니다. PlayerController.TryGuard가 퍼펙트가 아니게 막은 순간 부릅니다.
        /// </summary>
        public void PlayGuardEffect(Vector3 point)
        {
            PlayRandomSound(guardSounds);
            SpawnEffect(guardEffectPrefab, point);
        }

        // 가드 이펙트를 월드에 만들고 재생, hitEffectLifeTime 뒤 지움. 프리팹이 비어 있으면 안 함
        private void SpawnEffect(GameObject prefab, Vector3 point)
        {
            if (prefab == null) return;

            GameObject effect = Instantiate(prefab, point, Quaternion.identity);
            foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>()) particle.Play();
            Destroy(effect, hitEffectLifeTime);
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

            animEventListener = GetComponentInParent<AnimStateEventListener>();
            PlayerController player = GetComponentInParent<PlayerController>();
            slashRoot = player != null ? player.transform : transform;
        }

        /// <summary>
        /// 차지 시간으로 단계를 찾아서 모든 콤보 콜라이더에 값을 넘겨줍니다. ChargeState.Exit에서 호출합니다.
        /// </summary>
        public void ApplyCharge(float time)
        {
            ChargeValue value = chargeTable.Get(time / chargeTimeRate);

            // 필드(attackPower)는 건드리지 않고 지역 변수로 계산한다
            float damage = attackPower * weaponAttackRate * value.Damage;

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

        #region 안 쓰는 것 (상쇄, 11/02 뺌)

        [Header("상쇄 성공 (안 씀, 비어 있으면 안 함)")]
        [SerializeField] private GameObject counterEffectPrefab;
        [SerializeField] private AudioClip[] counterSounds;

        /// <summary>
        /// 상쇄가 맞부딪친 자리에 불꽃 + 금속음을 냅니다. CounterCollider가 상쇄에 성공한 순간 부릅니다.
        /// 소리는 이 칼의 AudioSource로 (상쇄 상자는 클립이 꺼서, 거기 붙이면 소리가 끊김)
        /// </summary>
        public void PlayCounterEffect(Vector3 point)
        {
            PlayRandomSound(counterSounds);

            if (counterEffectPrefab != null)
            {
                GameObject effect = Instantiate(counterEffectPrefab, point, Quaternion.identity);
                foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>()) particle.Play();
                Destroy(effect, hitEffectLifeTime);
            }
        }

        #endregion
    }
}
