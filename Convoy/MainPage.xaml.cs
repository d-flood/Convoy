using Convoy.ViewModels;
using FileSystemAccess;

namespace Convoy;

public partial class MainPage : ContentPage
{
    public List<string> targetFolderContents { get; }
    public MainPage(MainViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
