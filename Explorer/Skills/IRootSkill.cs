namespace RootBackend.Explorer.Skills
{
    public interface IRootSkill
    {
        bool CanHandle(string message);
        Task<string?> HandleAsync(string message);
    }
}
