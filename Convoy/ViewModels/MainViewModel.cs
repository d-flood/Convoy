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

    [ObservableProperty]
    List<string> failedMessages;

    [ObservableProperty]
    bool progressBarIsVisible;

    [ObservableProperty]
    double copyProgress;

    [ObservableProperty]
    HashSet<string> copiedFiles;

    [ObservableProperty]
    HashSet<FailedCopy> failedCopies;

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
    HashSet<string> sourceFiles;

    [ObservableProperty]
    HashSet<string> targetFiles;

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
            return;
        }
        await RefreshSourceFiles();
    }

async Task RefreshSourceFiles()
    {
        fileManagement.SourceRoot = SourceRoot;
        await fileManagement.InspectSourceDirectoryAsync();
        var sourceFiles = fileManagement.AllSourceFilePaths;
        SourceFiles = sourceFiles;
        SourceFilesLabel = $"Source Files: {SourceFiles.Count}";
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

        FailedCopies = fileManagement.AllFailedCopies;
        FailedMessages.Clear();
        var _failedMessages = new List<string>();
        foreach (var f in FailedCopies)
        {
            _failedMessages.Add(f.ErrorMessage);
        }
        FailedMessages = _failedMessages;
        await RefreshTargetFiles();
        TargetFilesLabel = $"Copied Files: {fileManagement.AllSuccessCopies.Count}";
        CopyActivityIndicatorIsRunning = false;
        ProgressBarIsVisible = false;
        CancelButtonIsVisible = false;
        StartCopyButtonIsVisible = true;
        if (FailedCopies.Count > 0)
        {
            RetryFailedButtonIsVisible = true;
        }
    }

    [RelayCommand(CanExecute = nameof(CheckEntries))]
    private async void DiffDirectories()
    {
        await RefreshSourceFiles();
        await RefreshTargetFiles();
        var inSourceNotTarget = new HashSet<string>(SourceFiles);
        inSourceNotTarget.ExceptWith(TargetFiles);
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

        FailedCopies = fileManagement.AllFailedCopies;
        await RefreshTargetFiles();
        TargetFilesLabel = $"Copied Files: {fileManagement.AllSuccessCopies.Count}";
        CopyActivityIndicatorIsRunning = false;
        ProgressBarIsVisible = false;
        if (FailedCopies.Count > 0)
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
        TargetFiles = fileManagement.AllTargetFilePaths;
        TargetFilesLabel = $"Files in Target Folder: {TargetFiles.Count}";
    }
}
