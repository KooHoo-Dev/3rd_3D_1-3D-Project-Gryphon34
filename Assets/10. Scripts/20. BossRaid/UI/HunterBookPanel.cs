using System;
using System.Collections.Generic;
using BossRaid.Data;
using BossRaid.Player;
using Study.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BossRaid.UI
{
    // 헌터북 페이지가 열리는 조건 (Progress에 이미 있는 값만)
    public enum PageUnlock
    {
        Start,         // 처음부터
        FirstDeath,    // 첫 사망 (forgeUnlocked, 대장간과 같이)
        Stage1Cleared, // 1단계 처치 (냄비와 같이)
        Stage2Cleared  // 2단계 처치
    }

    // 헌터북 (마을 씬). Tab으로 열고 닫는 조작·시스템 안내 책. 페이지 글은 인스펙터 pages에 씁니다
    // 마을에 도착했을 때 열렸는데 안 읽은 페이지가 있으면 알림 한 줄 + 목록에 NEW. 읽은 페이지 id는 Progress.readPages
    public class HunterBookPanel : MonoBehaviour
    {
        [Serializable]
        public class Page
        {
            public string id;          // 읽음 기록용 (영어). 바꾸면 다시 NEW가 됨
            public string title;
            public PageUnlock unlock;
            [TextArea(5, 20)] public string body;
        }

        [SerializeField] private PlayerController player;
        [SerializeField] private Page[] pages;

        [Header("화면")]
        [SerializeField] private CanvasGroup window;    // 책 창 전체
        [SerializeField] private TMP_Text hintText;     // "[Tab] 헌터북" (+ NEW). 창이 닫혀 있을 때만
        [SerializeField] private TMP_Text noticeText;   // 마을 도착 알림 한 줄
        [SerializeField] private float noticeTime = 4f;
        [SerializeField] private Transform rowParent;
        [SerializeField] private Button rowTemplate;    // 꺼 둔 줄 하나 (자식에 TMP_Text). 열린 페이지마다 복제합니다
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private ScrollRect bodyScroll; // 본문이 길 때 (비어 있으면 안 씀). 페이지를 바꾸면 맨 위로
        [SerializeField] private Button closeButton;

        [Header("소리 (2D, 비어 있으면 안 냄)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] openSounds; // 책을 펼칠 때

        private const string NewMark = "  <color=#FFC040>NEW</color>";

        public bool IsOpen { get; private set; }

        private readonly List<Button> rows = new();
        private float noticeTimer;

        private void Start()
        {
            SetWindowVisible(false);
            rowTemplate.gameObject.SetActive(false);
            closeButton.onClick.AddListener(Close);
            RefreshHint();
            ShowNotice();
        }

        private void Update()
        {
            if (noticeTimer > 0)
            {
                noticeTimer -= Time.deltaTime;
                if (noticeTimer <= 0) noticeText.gameObject.SetActive(false);
            }

            if (SimpleInput.GetKeyDown(Key.Tab) == false) return;
            if (IsOpen) Close();
            else if (player.enabled) Open(); // 다른 메뉴(화톳불·대장간·냄비)가 열려 있으면 플레이어가 꺼져 있음
        }

        public void Open()
        {
            IsOpen = true;
            SetWindowVisible(true);
            hintText.gameObject.SetActive(false);
            noticeText.gameObject.SetActive(false);
            noticeTimer = 0;
            MenuLock.Set(player, true);
            PlayRandomSound(openSounds);

            // 안 읽은 페이지가 있으면 그것부터, 없으면 첫 페이지
            Page first = null;
            foreach (Page page in pages)
            {
                if (IsUnlocked(page) == false) continue;
                if (first == null) first = page;
                if (IsRead(page) == false)
                {
                    first = page;
                    break;
                }
            }

            if (first != null) ShowPage(first);
            else
            {
                titleText.text = "";
                bodyText.text = "";
                RefreshRows();
            }
        }

        public void Close()
        {
            if (IsOpen == false) return;
            IsOpen = false;
            SetWindowVisible(false);
            MenuLock.Set(player, false);
            RefreshHint();
        }

        // 오른쪽에 페이지를 보여 주고 읽음으로 적음 (목록의 NEW도 다시 그림)
        private void ShowPage(Page page)
        {
            titleText.text = page.title;
            bodyText.text = page.body;
            if (bodyScroll != null) bodyScroll.verticalNormalizedPosition = 1f;

            List<string> readPages = GameData.Current.readPages;
            if (readPages.Contains(page.id) == false) readPages.Add(page.id);

            RefreshRows();
        }

        // 열린 페이지만 줄로 다시 만듦 (Refresh 때마다 새로 만들어서 버튼 이벤트가 쌓이지 않음, MenuRow와 같은 방식)
        private void RefreshRows()
        {
            foreach (Button row in rows) Destroy(row.gameObject);
            rows.Clear();

            foreach (Page page in pages)
            {
                if (IsUnlocked(page) == false) continue;

                Button row = Instantiate(rowTemplate, rowParent);
                row.gameObject.SetActive(true);
                row.GetComponentInChildren<TMP_Text>().text = IsRead(page) ? page.title : page.title + NewMark;
                Page target = page; // 람다가 반복 변수 대신 이 줄의 페이지를 기억하게
                row.onClick.AddListener(() => ShowPage(target));
                rows.Add(row);
            }
        }

        private void RefreshHint()
        {
            hintText.gameObject.SetActive(IsOpen == false);
            hintText.text = HasNewPage() ? "[Tab] 헌터북" + NewMark : "[Tab] 헌터북";
        }

        // 마을에 도착했을 때 열렸는데 안 읽은 페이지 제목을 한 줄로 몇 초 동안
        private void ShowNotice()
        {
            List<string> titles = new();
            foreach (Page page in pages)
            {
                if (IsUnlocked(page) && IsRead(page) == false) titles.Add(page.title);
            }

            if (titles.Count == 0)
            {
                noticeText.gameObject.SetActive(false);
                return;
            }

            noticeText.text = $"새 페이지: {string.Join(", ", titles)}  —  [Tab] 헌터북";
            noticeText.gameObject.SetActive(true);
            noticeTimer = noticeTime;
        }

        private bool HasNewPage()
        {
            foreach (Page page in pages)
            {
                if (IsUnlocked(page) && IsRead(page) == false) return true;
            }
            return false;
        }

        private static bool IsUnlocked(Page page)
        {
            Progress progress = GameData.Current;
            switch (page.unlock)
            {
                case PageUnlock.FirstDeath: return progress.forgeUnlocked;
                case PageUnlock.Stage1Cleared: return progress.stage1Cleared;
                case PageUnlock.Stage2Cleared: return progress.stage2Cleared;
                default: return true;
            }
        }

        private static bool IsRead(Page page)
        {
            return GameData.Current.readPages.Contains(page.id);
        }

        private void PlayRandomSound(AudioClip[] clips)
        {
            if (audioSource == null || clips == null || clips.Length == 0) return;

            AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Length)];
            if (clip != null) audioSource.PlayOneShot(clip);
        }

        private void SetWindowVisible(bool visible)
        {
            window.alpha = visible ? 1f : 0f;
            window.blocksRaycasts = visible;
            window.interactable = visible;
        }
    }
}
