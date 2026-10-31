using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BossRaid.Boss
{
    // 부위 하나 + 그 부위가 깨지면 켤 데칼 하나
    [Serializable]
    public class DecalPair
    {
        public BossPart part;
        public DecalProjector decal; // 부위 뼈의 자식으로 둬서 같이 움직임
    }

    // 부위가 깨지면 그 부위 표면에 금 간 자국(URP Decal Projector)을 켭니다. 보스 루트에 붙입니다
    // Boss는 "깨졌다"(OnPartBroken)만 알리고, 보이는 건 여기서 (Boss는 이 스크립트를 모름)
    // 데칼이 그려지려면 URP 렌더러(PC_Renderer)에 Decal Renderer Feature가 있어야 합니다
    [RequireComponent(typeof(Boss))]
    public class PartDecal : MonoBehaviour
    {
        [SerializeField] private DecalPair[] pairs;

        private Boss boss;

        private void Awake()
        {
            boss = GetComponent<Boss>();
        }

        private void Start()
        {
            foreach (DecalPair pair in pairs)
            {
                if (pair.decal != null) pair.decal.enabled = false;
            }
        }

        private void OnEnable()
        {
            boss.OnPartBroken += HandlePartBroken;
        }

        private void OnDisable()
        {
            boss.OnPartBroken -= HandlePartBroken;
        }

        private void HandlePartBroken(BossPart part)
        {
            foreach (DecalPair pair in pairs)
            {
                if (pair.part == part && pair.decal != null) pair.decal.enabled = true;
            }
        }
    }
}
