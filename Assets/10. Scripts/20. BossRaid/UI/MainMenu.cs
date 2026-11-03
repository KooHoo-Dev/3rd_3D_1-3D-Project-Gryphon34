using BossRaid.Data;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BossRaid.UI
{
    // 메인메뉴 (03. MainMenu 씬). 타이틀 화면: 가운데 제목 + 깜빡이는 "Press Any Button"
    // 아무 키·마우스 클릭 = 화면을 어둡게 한 뒤 마을로, Esc = 게임 끄기
    // 화면 구성은 BossRaid/UI/Create Main Menu 메뉴(MainMenuBuilder)가 만듭니다
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private string villageSceneName = "00. Village";
        [SerializeField] private TMP_Text pressText; // "Press Any Button". 깜빡임
        [SerializeField] private float blinkTime = 0.9f; // 밝음 → 어두움 한 번에 걸리는 시간
        [SerializeField] private CanvasGroup fade; // 시작하면 0 → 1 (검은 화면)
        [SerializeField] private float fadeTime = 0.6f;

        [Header("소리 (2D, 비어 있으면 안 냄)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clickSound; // 시작할 때

        private bool isLoading;

        private void Start()
        {
            // 마을·투기장에서 카메라가 커서를 잠그므로, 메뉴로 오면 다시 풀어 줌
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;

            if (fade != null)
            {
                fade.alpha = 0f;
                fade.blocksRaycasts = false;
            }

            if (pressText != null) pressText.DOFade(0.15f, blinkTime).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        }

        private void OnDestroy()
        {
            if (pressText != null) pressText.DOKill();
            if (fade != null) fade.DOKill();
        }

        private void Update()
        {
            if (isLoading) return;

            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                QuitGame();
                return;
            }

            bool keyPressed = keyboard != null && keyboard.anyKey.wasPressedThisFrame;
            bool clicked = mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame);
            if (keyPressed || clicked) StartGame();
        }

        private void StartGame()
        {
            isLoading = true;
            PlayClickSound(); // 페이드 뒤에 씬이 바뀌어서 끝까지 들림

            // 저장 기능이 없어서 시작할 때마다 새 진행도
            GameData.Current = new Progress();

            // 깜빡임을 멈추고 밝게 → 눌렸다는 걸 보여 줌
            if (pressText != null)
            {
                pressText.DOKill();
                pressText.alpha = 1f;
            }

            if (fade == null)
            {
                SceneManager.LoadScene(villageSceneName);
                return;
            }

            fade.blocksRaycasts = true;
            fade.DOFade(1f, fadeTime).SetUpdate(true)
                .OnComplete(() => SceneManager.LoadScene(villageSceneName));
        }

        private void QuitGame()
        {
            PlayClickSound(); // 빌드에선 바로 꺼져서 잘릴 수 있음
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void PlayClickSound()
        {
            if (audioSource != null && clickSound != null) audioSource.PlayOneShot(clickSound);
        }
    }
}
