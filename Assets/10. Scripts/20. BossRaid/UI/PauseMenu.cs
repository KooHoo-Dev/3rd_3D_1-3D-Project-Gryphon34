using BossRaid.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BossRaid.UI
{
    // 일시정지 창 (마을·투기장). Esc로 열고 닫음. [계속하기] = 닫기, [나가기] = 메인메뉴로
    // 열려 있는 동안: 시간 멈춤(timeScale 0), 소리 멈춤, 플레이어·카메라 꺼짐, 커서 보이기. 닫으면 열기 전 상태로 되돌림
    // (화톳불·대장간 창이 열린 채로 멈춰도 그 창 상태 그대로 돌아감. 슬로모션 중이면 슬로모션으로)
    // 화면 구성은 BossRaid/UI/Create Pause Menu 메뉴(PauseMenuBuilder)가 만듭니다
    public class PauseMenu : MonoBehaviour
    {
        public static bool IsPaused { get; private set; } // 키를 직접 읽는 스크립트(화톳불·헌터북·회복약 등)가 보고 멈춤

        [SerializeField] private string mainMenuSceneName = "03. MainMenu";
        [SerializeField] private CanvasGroup window;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button quitButton;

        private PlayerController player;
        private ThirdPersonCamera cameraControl;

        // 열기 전 상태 (닫을 때 되돌림)
        private float savedTimeScale;
        private bool savedPlayerEnabled;
        private bool savedCameraEnabled;
        private bool savedCursorVisible;
        private CursorLockMode savedCursorLock;

        private void Start()
        {
            SetWindowVisible(false);
            resumeButton.onClick.AddListener(Resume);
            quitButton.onClick.AddListener(QuitToMainMenu);
            player = FindAnyObjectByType<PlayerController>();
            cameraControl = FindAnyObjectByType<ThirdPersonCamera>();
        }

        private void OnDestroy()
        {
            if (IsPaused) Unpause(); // 멈춘 채로 씬이 바뀌어도 다음 씬이 멈춰 있지 않게
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || keyboard.escapeKey.wasPressedThisFrame == false) return;

            if (IsPaused) Resume();
            else Pause();
        }

        private void Pause()
        {
            IsPaused = true;

            savedTimeScale = Time.timeScale;
            savedCursorVisible = Cursor.visible;
            savedCursorLock = Cursor.lockState;
            if (player != null)
            {
                savedPlayerEnabled = player.enabled;
                player.enabled = false;
            }
            if (cameraControl != null)
            {
                savedCameraEnabled = cameraControl.enabled;
                cameraControl.enabled = false;
            }

            Time.timeScale = 0f;
            AudioListener.pause = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            SetWindowVisible(true);
        }

        private void Resume()
        {
            if (IsPaused == false) return;
            Unpause();

            if (player != null) player.enabled = savedPlayerEnabled;
            if (cameraControl != null) cameraControl.enabled = savedCameraEnabled;
            Cursor.visible = savedCursorVisible;
            Cursor.lockState = savedCursorLock;
            SetWindowVisible(false);
        }

        // 시간·소리만 되돌림 (씬을 떠날 때는 플레이어·커서는 다음 씬이 알아서 정함)
        private void Unpause()
        {
            IsPaused = false;
            Time.timeScale = savedTimeScale;
            AudioListener.pause = false;
        }

        private void QuitToMainMenu()
        {
            Unpause();
            Time.timeScale = 1f; // 슬로모션 중에 나가도 메뉴는 제 속도
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void SetWindowVisible(bool visible)
        {
            window.alpha = visible ? 1f : 0f;
            window.blocksRaycasts = visible;
            window.interactable = visible;
        }
    }
}
