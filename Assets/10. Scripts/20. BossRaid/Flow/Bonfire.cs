using UnityEngine;

namespace BossRaid.Flow
{
    // 마을 씬의 화톳불입니다. 지금은 루프가 한 바퀴 도는지 확인하는 자리표시자입니다.
    // 마을에 들어올 때 지난 사냥 결과를 로그로 보여주고, 대장간 자리를 표시합니다.
    public class Bonfire : MonoBehaviour
    {
        private void Start()
        {
            switch (HuntManager.LastResult)
            {
                case HuntResult.None:
                    Debug.Log("[화톳불] 준비하고 안개문으로 가세요");
                    break;
                case HuntResult.Killed:
                    Debug.Log("[화톳불] 사냥 성공 후 귀환");
                    Debug.Log("[대장간] 제작·강화 (미구현)");
                    break;
                case HuntResult.Died:
                    Debug.Log("[화톳불] 사망 후 귀환");
                    Debug.Log("[대장간] 제작·강화 (미구현)");
                    break;
            }
        }
    }
}
