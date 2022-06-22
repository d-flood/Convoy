namespace Convoy
{
    public interface IFolderPicker
    {
        Task<string> PickFolder();
    }
}
