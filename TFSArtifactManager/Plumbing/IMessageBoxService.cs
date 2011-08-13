namespace TFSArtifactManager.Plumbing
{
    public interface IMessageBoxService
    {
        void ShowOKDispatch(string text, string header);
        bool ShowOkCancel(string text, string header);
    }
}
