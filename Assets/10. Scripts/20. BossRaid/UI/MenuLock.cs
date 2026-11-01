using BossRaid.Player;
using UnityEngine;

namespace BossRaid.UI
{
    // 마을 메뉴(대장간·냄비)가 열려 있는 동안: 이동·공격 입력 멈춤, 카메라 회전 멈춤, 커서 보이기 (닫으면 되돌림)
    // BonfirePanel.LockPlayer와 같은 일
    public static class MenuLock
    {
        public static void Set(PlayerController player, bool locked)
        {
            if (locked)
            {
                player.StopMovement();
                player.Animator.SetFloat(PlayerController.SpeedHash, 0f); // 꺼지면 Speed를 못 내려서 걷는 모션으로 굳는 것 막기
            }
            player.enabled = !locked;

            ThirdPersonCamera cameraControl = Object.FindAnyObjectByType<ThirdPersonCamera>();
            if (cameraControl != null) cameraControl.enabled = !locked;

            Cursor.visible = locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
}
