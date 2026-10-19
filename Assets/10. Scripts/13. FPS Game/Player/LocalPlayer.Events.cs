using System;
using UnityEngine;

namespace Study3D.FpsGame.Player
{
    public partial class LocalPlayer : Player
    {
        // public event Action<TouchItemEvent> OnTouchedItem;
        public event Action OnStartChangeWeapon;
        public event Action OnEndedChangeWeapon;
        public event Action OnRequestedReload;
        public event Action OnEndedReload;
        public event Action<int, int, int> OnTakenDamage; // (데미지, 현재 HP, 최대 HP)
        public event Action OnDied;
    }

    public partial class Legacy_LocalPlayer
    {
        public class Callbacks
        {
            public Action OnStartChangeWeapon;
            public Action OnEndedChangeWeapon;
            public Action OnStartedReload;
            public Action OnEndedReload;
            public Action<int, int, int> OnTakenDamage;
            public Action OnDied;
        }

        public Callbacks Events = new Callbacks();
    }

    class TestCode
    {
        public void Note()
        {
            LocalPlayer lp = new LocalPlayer();
            lp.OnEndedChangeWeapon += () => { };
            
            Legacy_LocalPlayer p = new Legacy_LocalPlayer();
            p.Events.OnEndedChangeWeapon = null;
        }
    }
}