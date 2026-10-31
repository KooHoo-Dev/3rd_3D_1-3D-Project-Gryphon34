using BossRaid.Data;
using BossRaid.Flow;
using BossRaid.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BossRaid.UI
{
    // 화톳불 메뉴 (마을 씬). Bonfire가 [E]로 열고 닫고, 버튼은 Bonfire의 함수를 부릅니다
    // 규칙(인간성 −1, 회복약 최대 +1)은 Bonfire에 있고, 이 패널은 보여 주기 + 열려 있는 동안 플레이어 입력 막기만 합니다
    // 화면 구성은 BossRaid/UI/Create Bonfire Panel 메뉴(BonfirePanelBuilder)가 만듭니다
    public class BonfirePanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text promptText;   // "[E] 화톳불" (가까이 있을 때만)
        [SerializeField] private CanvasGroup window;    // 메뉴 창 전체
        [SerializeField] private TMP_Text humanityText;
        [SerializeField] private TMP_Text potionText;
        [SerializeField] private Button burnButton;
        [SerializeField] private TMP_Text reasonText;   // 태우기를 못 할 때 이유
        [SerializeField] private Button closeButton;

        public bool IsOpen { get; private set; }

        private Bonfire bonfire;
        private ThirdPersonCamera cameraControl;

        private void Start()
        {
            SetWindowVisible(false);
            promptText.gameObject.SetActive(false);
            burnButton.onClick.AddListener(HandleBurnClicked);
            closeButton.onClick.AddListener(Close);
            cameraControl = FindAnyObjectByType<ThirdPersonCamera>();
        }

        public void ShowPrompt(bool show)
        {
            promptText.gameObject.SetActive(show && IsOpen == false);
        }

        public void Open(Bonfire owner)
        {
            bonfire = owner;
            IsOpen = true;
            promptText.gameObject.SetActive(false);
            SetWindowVisible(true);
            LockPlayer(true);
            Refresh();
        }

        public void Close()
        {
            if (IsOpen == false) return;
            IsOpen = false;
            SetWindowVisible(false);
            LockPlayer(false);
        }

        private void HandleBurnClicked()
        {
            bonfire.BurnHumanity();
            Refresh();
        }

        // 지금 값으로 글자·버튼을 다시 그림
        private void Refresh()
        {
            Progress progress = GameData.Current;
            humanityText.text = $"인간성  {progress.humanity}";
            potionText.text = $"회복약 최대  {progress.currentMaxPotion} / {bonfire.MaxPotionLimit}";

            burnButton.interactable = bonfire.CanBurnHumanity;
            if (progress.humanity <= 0) reasonText.text = "인간성이 없습니다";
            else if (progress.currentMaxPotion >= bonfire.MaxPotionLimit) reasonText.text = "회복약이 이미 최대입니다";
            else reasonText.text = "";
        }

        // 메뉴가 열려 있는 동안: 이동·공격 입력 멈춤, 카메라 회전 멈춤, 커서 보이기 (닫으면 되돌림)
        private void LockPlayer(bool locked)
        {
            PlayerController player = bonfire.player;
            if (locked)
            {
                player.StopMovement();
                player.Animator.SetFloat(PlayerController.SpeedHash, 0f); // 꺼지면 Speed를 못 내려서 걷는 모션으로 굳는 것 막기
            }
            player.enabled = !locked;
            if (cameraControl != null) cameraControl.enabled = !locked;

            Cursor.visible = locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private void SetWindowVisible(bool visible)
        {
            window.alpha = visible ? 1f : 0f;
            window.blocksRaycasts = visible;
            window.interactable = visible;
        }
    }
}
