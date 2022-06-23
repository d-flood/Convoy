using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using AppLogic;
using FileConvoy;
using System;

namespace Convoy.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IFolderPicker _folderPicker;
    private FileManagement fileManagement;
    private CancellationToken _cancellationToken;
    private int InitialDisplayNumber;

    [ObservableProperty]
    List<string> failedMessages;

    [ObservableProperty]
    bool progressBarIsVisible;

    [ObservableProperty]
    double copyProgress;

    [ObservableProperty]
    IEnumerable<FailedCopy> failedCopies;

    [ObservableProperty]
    bool overwrite;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCopyCommand), nameof(DiffDirectoriesCommand))]
    string targetRoot;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCopyCommand), nameof(DiffDirectoriesCommand))]
    string sourceRoot;

    [ObservableProperty]
    string sourceFilesLabel;

    [ObservableProperty]
    string targetFilesLabel;

    [ObservableProperty]
    IEnumerable<string> sourceFiles;

    [ObservableProperty]
    IEnumerable<string> targetFiles;

    [ObservableProperty]
    bool copyActivityIndicatorIsRunning;

    [ObservableProperty]
    bool cancelButtonIsVisible;

    [ObservableProperty]
    bool startCopyButtonIsVisible;

    [ObservableProperty]
    bool retryFailedButtonIsVisible;

    [ObservableProperty]
    bool cancelRetryFailedButtonIsVisible;

    [ObservableProperty]
    string failedCopiesLabel;

    public MainViewModel(IFolderPicker folderPicker)
    {
        _folderPicker = folderPicker;
        fileManagement = new FileManagement();
        FailedMessages = new List<string>();
        StartCopyButtonIsVisible = true;
        CancelButtonIsVisible = false;
        CancelRetryFailedButtonIsVisible = false;
        RetryFailedButtonIsVisible = false;
        InitialDisplayNumber = 100;
    }

    [RelayCommand]
    async void RetrieveSourceFiles()
    {
        var _sourceRoot = await _folderPicker.PickFolder();
        if (Directory.Exists(_sourceRoot))
            {
                SourceRoot = _sourceRoot;
            }
        else
        {
            await Application.Current.MainPage.DisplayAlert("Problem Loading Folder", $"Could not find\n{_sourceRoot}", "OK");
            return;
        }
        CopyActivityIndicatorIsRunning = true;
        await RefreshSourceFiles();
        CopyActivityIndicatorIsRunning = false;
    }

async Task RefreshSourceFiles()
    {
        fileManagement.SourceRoot = SourceRoot;
        await fileManagement.InspectSourceDirectoryAsync();
        SourceFiles = fileManagement.AllSourceFilePaths.Take(InitialDisplayNumber);
        SourceFilesLabel = $"Source Files: {fileManagement.AllSourceFilePaths.Count}";
    }

    [RelayCommand]
    async void RetrieveTargetFiles()
    {
        var _targetRoot = await _folderPicker.PickFolder();
        if (Directory.Exists(_targetRoot))
        {
            TargetRoot = _targetRoot;
        }
        else
        {
            return;
        }
        await RefreshTargetFiles();
    }

    [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CheckEntries), IncludeCancelCommand = true)]
    private Task StartCopy(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        return CopyFiles(cancellationToken);
    }

    async Task CopyFiles(CancellationToken cancellationToken)
    {
        FailedCopiesLabel = "";
        CopyActivityIndicatorIsRunning = true;
        ProgressBarIsVisible = true;
        StartCopyButtonIsVisible = false;
        CancelButtonIsVisible = true;
        CopyProgress = 0;

        Progress<DownloadProgress> progress = new Progress<DownloadProgress>();
        progress.ProgressChanged += ReportProgress;

        try
        {
            await fileManagement.CopyFiles(Overwrite, progress, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            TargetFilesLabel = $"Cancelled after copying {fileManagement.AllSuccessCopies.Count} files";
            CopyActivityIndicatorIsRunning = false;
            ProgressBarIsVisible = false;
            return;
        }

        if (fileManagement.AllFailedCopies.Count > 0)
        {
            FailedCopies = fileManagement.AllFailedCopies.Take(InitialDisplayNumber);
            var _failedMessages = new List<string>();
            foreach (var f in FailedCopies)
            {
                _failedMessages.Add(f.ErrorMessage);
            }
            FailedMessages = _failedMessages;
        }
        await RefreshTargetFiles();
        TargetFilesLabel = $"Copied Files: {fileManagement.AllSuccessCopies.Count}";
        CopyActivityIndicatorIsRunning = false;
        ProgressBarIsVisible = false;
        CancelButtonIsVisible = false;
        StartCopyButtonIsVisible = true;
        if (fileManagement.AllFailedCopies.Count > 0)
        {
            RetryFailedButtonIsVisible = true;
        }
    }

    [RelayCommand(CanExecute = nameof(CheckEntries))]
    private async void DiffDirectories()
    {
        await RefreshSourceFiles();
        await RefreshTargetFiles();
        var inSourceNotTarget = new HashSet<string>(fileManagement.AllSourceFilePaths);
        inSourceNotTarget.ExceptWith(fileManagement.AllTargetFilePaths);
        FailedMessages = inSourceNotTarget.ToList();
        FailedCopiesLabel = $"Source vs. Target: {FailedMessages.Count}";
    }


    [RelayCommand(AllowConcurrentExecutions = false, IncludeCancelCommand = true)]
    private Task RetryFailed(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        return RetryFailedCopies(cancellationToken);
    }

    async Task RetryFailedCopies(CancellationToken cancellationToken)
    {
        FailedCopiesLabel = "";
        CopyActivityIndicatorIsRunning = true;
        ProgressBarIsVisible = true;
        StartCopyButtonIsVisible = false;
        CancelRetryFailedButtonIsVisible = true;
        CopyProgress = 0;

        FailedMessages = new List<string>();
        Progress<DownloadProgress> progress = new Progress<DownloadProgress>();
        progress.ProgressChanged += ReportProgress;

        try
        {
            await fileManagement.RetryFailed(Overwrite, progress, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            TargetFilesLabel = $"Cancelled after copying {fileManagement.AllSuccessCopies.Count} files";
            CopyActivityIndicatorIsRunning = false;
            ProgressBarIsVisible = false;
            CancelRetryFailedButtonIsVisible = false;
            return;
        }

        FailedCopies = fileManagement.AllFailedCopies.Take(InitialDisplayNumber);
        await RefreshTargetFiles();
        TargetFilesLabel = $"Copied Files: {fileManagement.AllSuccessCopies.Count}";
        CopyActivityIndicatorIsRunning = false;
        ProgressBarIsVisible = false;
        if (fileManagement.AllFailedCopies.Count > 0)
        {
            RetryFailedButtonIsVisible = true;
        }
        else
        {
            RetryFailedButtonIsVisible = false;
        }
        StartCopyButtonIsVisible = true;
        CancelRetryFailedButtonIsVisible = false;
    }


    private void ReportProgress(object sender, DownloadProgress e)
    {
        CopyProgress = e.PercentageComplete;
        TargetFilesLabel = $"Copied: {e.CopedFiles.Count}";
        TargetFiles = e.CopedFiles;
        FailedCopies = e.Failed;
    }

    private Boolean CheckEntries()
    {
        if (!String.IsNullOrEmpty(TargetRoot) && !String.IsNullOrEmpty(SourceRoot))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private async Task RefreshTargetFiles()
    {
        fileManagement.TargetRoot = TargetRoot;
        await fileManagement.InspectTargetDirectoryAsync();
        TargetFiles = fileManagement.AllTargetFilePaths.Take(InitialDisplayNumber);
        TargetFilesLabel = $"Files in Target Folder: {fileManagement.AllTargetFilePaths.Count}";
    }
}
