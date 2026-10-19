using Study3D.FpsGame.Weapon;
using UnityEngine;

namespace Study3D.FpsGame.Player
{
    // LocalPlayer
    // : 유저가 직접 제어하는 게임 속 내의 플레이어 객체
    // HP, 무기 제어, 아이템 줍기, 재장전, 무기 교체, 다양한 알림등
    // 게임 이벤트()와 연결되어있는 주요 클래스 입니다.
    public partial class LocalPlayer : Player, IDamageable
    {
        [Header("Stat")] 
        [SerializeField] private int maxHp = 100;

        [Header("Settings.Weapon")] [SerializeField]
        private Weapon.Weapon.WeaponType startWeapon
            = Weapon.Weapon.WeaponType.AssaultRifle;
        
        // Weapon을 연결하기 위한 참조들을 설정해 줍니다.
        [SerializeField] private Transform weaponParent;
        [SerializeField] private WeaponController weaponWeaponController;

        [Header("애니메이션 콜백")]
        [SerializeField] private string reloadCompleteEvent = "ReloadComplete";
        [SerializeField] private string swapCompleteEvent = "SwapComplete";
        
        [Header("Ref")] 
        [SerializeField] private CharacterController characterController;
        [SerializeField] private AnimStateEventListener animStateEventListener;

        // MainCollider를 따로 지정하는것을 선호합니다.
        public Collider MainCollider => characterController;
        // 이 부분은 인터페이스 보시면 됩니다.
        public GameObject GameObject => gameObject;
        
        public Weapon.Weapon CurrentWeapon { get; private set; }
        public Health Health { get; private set; } // 단순 데미지 차감용
        public bool IsDead { get; private set; }
        public bool IsReloading { get; private set; } = false;

        #region 유니티 함수들
        
        private void Awake()
        {
            Player.LocalPlayer = this;
            Health = new Health(maxHp);
        }

        private void OnDestroy()
        {
            if (Player.LocalPlayer == this) Player.LocalPlayer = null;
        }

        private void Start()
        {
            var changeTarget = weaponWeaponController.GetWeaponOrNull(startWeapon);
            ChangeWeapon(changeTarget);
        }

        private void OnEnable()
        {
            animStateEventListener.OnOccursAnimStateEvent += OnAnimEvent;
        }

        private void OnDisable()
        {
            animStateEventListener.OnOccursAnimStateEvent -= OnAnimEvent;
        }
        
        #endregion
        
        #region 애니메이션 이벤트 처리

        private void OnAnimEvent(string eventName, string eventParam)
        {
            // 재장전이 완료된 시점에 호출되는 애니메이션 상태 이벤트 입니다.
            if (eventName == reloadCompleteEvent)
            {
                IsReloading = false;
                // 현재 무기에 리로드하라고 명령 합니다.
                CurrentWeapon.Reload();
                // 리로드 완료 시점을 델리게이트를 깨워줍니다.
                // (알림해놓은 구독자들에게 완료 됐음을 전달합니다.)
                OnEndedReload?.Invoke();
            }
            // 무기 교체 애니메이션 상태가 완료된 시점에 호출되는 이벤트 입니다.
            else if (eventName == swapCompleteEvent)
            {
                OnEndedChangeWeapon?.Invoke();
            }
        }
        
        #endregion
        
        #region 총기 명령
        
        public bool Fire()
        {
            return CurrentWeapon.Fire();
        }

        public void RequestReload()
        {
            if (IsReloading) return;
            IsReloading = true;
            OnRequestedReload?.Invoke();
        }

        private void ChangeWeapon(Weapon.Weapon weapon)
        {
            if (CurrentWeapon != null)
            {
                var prevWeapon = CurrentWeapon;
                prevWeapon.gameObject.SetActive(false);
            }

            CurrentWeapon = weapon;
            CurrentWeapon.gameObject.SetActive(true);
            OnStartChangeWeapon?.Invoke();
            CurrentWeapon.Reload(); // 재장전 애니메이션 재생 없이.
        }
        
        #endregion

        #region 게임 이벤트 처리
        
        public void TakeDamage(CombatEvent combatEvent)
        {
            throw new System.NotImplementedException();
        }

        public void TakeHeal(HealEvent healEvent)
        {
            throw new System.NotImplementedException();
        }
        
        #endregion
    }
}