using BossRaid.Player;
using UnityEngine;

namespace BossRaid.Weapon
{
    // "무기 공격력 x 차지 배율"을 계산해서 콤보 콜라이더들에 넘겨주는 대검
    // 동작마다 다른 값(모션값, 모션 피로도)은 각 WeaponCollider가 들고 있습니다.
    public class GreatSword : MonoBehaviour
    {
        [SerializeField] private float attackPower = 100f; // 강화하거나 다른 대검으로 바꾸면 이 값이 달라짐
        [SerializeField] private WeaponCollider[] weaponColliders;

        private ChargeTable chargeTable;

        private void Awake()
        {
            chargeTable = new ChargeTable();
            chargeTable.Load();

            // 인스펙터에 안 넣었으면 자식에서 찾는다. 콤보 콜라이더는 공격할 때만 켜지므로 꺼진 것까지 찾는다(true)
            if (weaponColliders == null || weaponColliders.Length == 0)
            {
                weaponColliders = GetComponentsInChildren<WeaponCollider>(true);
            }

            // 차지를 거치지 않은 공격도 0단계(배율 1.0)로 때리도록 기본값을 넣어둔다
            ApplyCharge(0f);
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
