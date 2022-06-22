using FileConvoy;

namespace AppLogic;

public class FileManagement
{
    public string SourceRoot;
    private HashSet<string> _allRelativeSubDirs;
    private HashSet<string> _allRelativeTargetSubDirs;
    public HashSet<string> AllSourceFilePaths;
    public HashSet<FailedCopy> AllFailedCopies;
    public HashSet<string> AllSuccessCopies;
    public HashSet<string> AllTargetFilePaths;
    public bool Overwrite;
    public string TargetRoot;

    public FileManagement()
    {
        AllFailedCopies = new HashSet<FailedCopy>();
    }
    public async Task<List<string>> InspectSourceDirectoryAsync()
    {
        var sourceDirectory = new WalkDirectory(SourceRoot);
        await sourceDirectory.WalkDirectoryAsync();
        AllSourceFilePaths = sourceDirectory.AllFiles;
        _allRelativeSubDirs = sourceDirectory.AllDirs;
        var failed = sourceDirectory.FailedDirs;
        failed.AddRange(sourceDirectory.FailedFiles);
        return failed;
    }
    public async Task<List<string>> InspectTargetDirectoryAsync()
    {
        var targetDirectory = new WalkDirectory(TargetRoot);
        await targetDirectory.WalkDirectoryAsync();
        AllTargetFilePaths = targetDirectory.AllFiles;
        _allRelativeTargetSubDirs = targetDirectory.AllDirs;
        var failed = targetDirectory.FailedDirs;
        failed.AddRange(targetDirectory.FailedFiles);
        return failed;
    }

    public async Task CopyFiles(bool overwrite, IProgress<DownloadProgress> progress, CancellationToken cancellationToken)
    {
        var copyPaths = DirectoryOperations.MakeFileCopyPaths(AllSourceFilePaths, SourceRoot, TargetRoot);
        var dirPaths = DirectoryOperations.MakeDirectoryCopyPaths(_allRelativeSubDirs, SourceRoot, TargetRoot);
        var mover = new MoveFiles(copyPaths, dirPaths, overwrite);
        mover.CreateNewDirectories();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        await mover.CopyFiles(progress, SourceRoot, cancellationToken);
        AllSuccessCopies = mover.CopiedFiles;
        AllFailedCopies = mover.FailedToCopy;
    }

    public async Task RetryFailed(bool overwrite, IProgress<DownloadProgress> progress, CancellationToken cancellationToken)
    {
        var failedPaths = new HashSet<string>();
        foreach (var item in AllFailedCopies)
        {
            failedPaths.Add(item.FullPath);
        }
        var copyPaths = DirectoryOperations.MakeFileCopyPaths(failedPaths, SourceRoot, TargetRoot);
        var dirPaths = DirectoryOperations.MakeDirectoryCopyPaths(_allRelativeSubDirs, SourceRoot, TargetRoot);
        var mover = new MoveFiles(copyPaths, dirPaths, overwrite);
        mover.CreateNewDirectories();
        await mover.CopyFiles(progress, SourceRoot, cancellationToken);
        AllSuccessCopies = mover.CopiedFiles;
        AllFailedCopies = mover.FailedToCopy;
    }
}
