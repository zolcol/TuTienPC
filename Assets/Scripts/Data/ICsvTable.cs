namespace TopDownGame.Data
{
    public interface ICsvTable
    {
        bool IsLoaded { get; }
        void Load();
        void Clear();
    }
}
