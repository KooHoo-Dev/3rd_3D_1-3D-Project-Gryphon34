using BossRaid.Player;
using TMPro;
using UnityEngine;

namespace BossRaid.Flow
{
    // 길가에 세운 안내 책. 가까이 오면 화면에 이 책의 문구를 띄우고, 멀어지면 끔 (FogGate의 lockedMessage와 같은 방식)
    // 트리거는 따로 붙인 Sphere Collider (Is Trigger). 책 모양 MeshCollider는 몸을 막는 용도로 그대로 둠
    public class GuideBook : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText; // 화면 안내 글자 (PlayerHUD 아래 GuideText, 꺼 두기). 책 여러 권이 같이 씀
        [SerializeField, TextArea(3, 6)] private string text; // 이 책의 문구 (<color=#F0C060>[좌클릭]</color>처럼 색을 넣을 수 있음)

        private void Awake()
        {
            if (messageText != null) messageText.gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (messageText == null) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;

            messageText.text = text;
            messageText.gameObject.SetActive(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (messageText == null) return;
            if (other.GetComponentInParent<PlayerController>() == null) return;

            messageText.gameObject.SetActive(false);
        }
    }
}
