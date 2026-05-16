namespace WDG
{
    public interface IGameSystem
    {
        void Initialize();
        void Tick(float deltaTime);
        void Dispose();
    }
}
