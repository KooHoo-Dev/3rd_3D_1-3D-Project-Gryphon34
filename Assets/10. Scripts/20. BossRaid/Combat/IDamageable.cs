namespace BossRaid.Combat
{
    public interface IDamageable
    {
        object Owner { get; }
        
        void TakeDamage(AttackInfo info);
    }
}