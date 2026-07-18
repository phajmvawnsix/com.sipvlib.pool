namespace SiPVLib.Pool
{
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
        void Despawn();
    }
}