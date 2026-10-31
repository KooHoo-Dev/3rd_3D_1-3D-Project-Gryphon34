using Study.Utilities;
using UnityEngine;

namespace BossRaid.Boss
{
    public class DetectDistance : MonoBehaviour
    {
        private Boss boss;

        private Vector3 lastPosition;
        private float currentFarTime;
        [SerializeField] private float farTime = 1.5f;
        [SerializeField] private float playerDirection = 0.5f;
        [SerializeField] private float movingSpeed = 1.5f;

        public bool IsPlayerMovingAway
        {
            get
            {
                return currentFarTime >= farTime;
            }
        }

        private void Awake()
        {
            boss = GetComponent<Boss>();
        }

        private void Update()
        {
            if (boss.Player == false) return;

            Vector3 currentPosition = boss.Player.transform.position;
            Vector3 move = currentPosition - lastPosition;
            move.y = 0f;
            float speed = move.magnitude / Time.deltaTime;

            if (speed >= movingSpeed)
            {
                float direction = Vector3.Dot(move.normalized, transform.FlatDirectionTo(boss.Player.transform));
                if (direction >= playerDirection) currentFarTime = Mathf.Min(farTime, currentFarTime + Time.deltaTime);
                else currentFarTime = Mathf.Max(0f, currentFarTime - Time.deltaTime);
            }

            lastPosition = currentPosition;
        }

        public void ResetFarTime()
        {
            currentFarTime = 0f;
        }

        // 임시: 멀어지는 중 판정 테스트용. Decide 연결 확인 뒤 지웁니다
        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 24 };
            GUI.Label(new Rect(Screen.width * 0.5f - 350f, 60f, 700f, 40f),
                $"멀어진 시간: {currentFarTime:F2} / {farTime} / 멀어지는 중: {IsPlayerMovingAway}", style);
        }
    }
}
