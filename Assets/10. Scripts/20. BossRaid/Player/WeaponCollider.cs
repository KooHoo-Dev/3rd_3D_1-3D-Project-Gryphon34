using System.Collections.Generic;
using UnityEngine;

namespace BossRaid.Player
{
    public class WeaponCollider : MonoBehaviour
    {
        private Collider weaponCollider;
        private HashSet<Collider> colliders = new HashSet<Collider>();

        private void Awake()
        {
            weaponCollider =  GetComponent<Collider>();
            weaponCollider.enabled = false;
        }
        
        
    }
}