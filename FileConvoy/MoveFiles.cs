namespace FileConvoy;

public class MoveFiles
{
    private bool _overwrite;
    private List<string[]> _allFilePaths;
    private List<string> _newDirs;
    public List<string> FailedToMakeDir;
    public HashSet<FailedCopy> FailedToCopy;
    public HashSet<string> CopiedFiles;
    public MoveFiles(List<string[]> _allFiles, List<string> allDirs, bool overwrite)
    {
        _allFilePaths = _allFiles;
        _newDirs = allDirs;
        _overwrite = overwrite;
        FailedToMakeDir = new List<string>();
        FailedToCopy = new HashSet<FailedCopy>();
        CopiedFiles = new HashSet<string>();
    }

    public void CreateNewDirectories()
    {
        foreach (var dir in _newDirs)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    continue;
                }
                Directory.CreateDirectory(dir);
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine($"Failed to create the directory: {dir}");
                FailedToMakeDir.Add(dir);
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"Not authorized to modify {dir}");
                FailedToMakeDir.Add(dir);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e}");
                FailedToMakeDir.Add(dir);
            }
        }
    }
    public async Task CopyFiles(IProgress<DownloadProgress> progress, string sourceRoot, CancellationToken cancellationToken)
    {
        List<Task<string>> tasks = new();
        var report = new DownloadProgress();
        foreach (var file in _allFilePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (!_overwrite && File.Exists(file[1]))
                {
                    continue;
                }
                tasks.Add(Task.Run(() => CopyFile(file[0], file[1], progress, report, sourceRoot, cancellationToken)));
                //tasks.Add(CopyFile(file[0], file[1]));
            }
            catch (Exception e)
            {
                FailedToCopy.Add(new FailedCopy(file[0], e.Message));
            }
        }
        await Task.WhenAll(tasks);

        // https://devblogs.microsoft.com/pfxteam/processing-tasks-as-they-complete/
        //while (tasks.Count > 0)
        //{
        //    var t = await Task.WhenAny(tasks);
        //    tasks.Remove(t);
        //    try
        //    {
        //        var CopiedFile = await t;
        //    }
        //    catch (OperationCanceledException) {}
        //}
    }

    private async Task<string> CopyFile(string src, string dst, IProgress<DownloadProgress> progress, DownloadProgress report, string sourceRoot, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        try
        {
            //File.Copy(src, dst, _overwrite);
            await Task.Run(() => File.Copy(src, dst, _overwrite), cancellationToken);
            CopiedFiles.Add(src);
        }
        catch (Exception e)
        {
            FailedToCopy.Add(new FailedCopy(Path.GetRelativePath(sourceRoot, src), e.Message));
        }
        report.CopedFiles = CopiedFiles;
        report.Failed = FailedToCopy;
        report.PercentageComplete = Convert.ToDouble(CopiedFiles.Count) / Convert.ToDouble(_allFilePaths.Count);
        progress.Report(report);
        return src;
    }
}
