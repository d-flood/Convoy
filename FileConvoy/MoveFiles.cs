using System.Security.Cryptography;
using System.Threading.Tasks;

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

    public async void CreateNewDirectories()
    {
        foreach (var dir in _newDirs)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    continue;
                }
                await Task.Run(() => Directory.CreateDirectory(dir));
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
        //List<Task> tasks = new();
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
                using FileStream sourceStream = File.Open(file[0], FileMode.Open);
                using FileStream destinationStream = File.Create(file[1]);
                await sourceStream.CopyToAsync(destinationStream, cancellationToken);
                //tasks.Add(Task.Run(() => CopyFile(file[0], file[1], progress, report, sourceRoot, cancellationToken)));
                //tasks.Add(CopyFile(file[0], file[1]));
                CopiedFiles.Add(file[1]);
            }
            catch (OperationCanceledException e) 
            {
                FailedToCopy.Add(new FailedCopy(file[0], e.Message));
                throw;
            }
            catch (Exception e)
            {
                FailedToCopy.Add(new FailedCopy(file[0], e.Message));
            }
            report.CopedFiles = CopiedFiles;
            report.Failed = FailedToCopy;
            report.PercentageComplete = Convert.ToDouble(CopiedFiles.Count) / Convert.ToDouble(_allFilePaths.Count);
            progress.Report(report);
        }
        
        //await Task.WhenAll(tasks);
    }

    //private async Task<string> CopyFile(string src, string dst, IProgress<DownloadProgress> progress, DownloadProgress report, string sourceRoot, CancellationToken cancellationToken)
    //{
    //    try
    //    {
    //        cancellationToken.ThrowIfCancellationRequested();
    //        using FileStream sourceStream = File.Open(src, FileMode.Open);
    //        using FileStream destinationStream = File.Create(dst);
    //        await sourceStream.CopyToAsync(destinationStream, cancellationToken);
    //        //await Task.Run(() => File.Copy(src, dst, _overwrite), cancellationToken);
    //        CopiedFiles.Add(src);
    //    }
    //    catch (OperationCanceledException)
    //    {
    //        FailedToCopy.Add(new FailedCopy(Path.GetRelativePath(sourceRoot, src), e.Message));
    //        throw;
    //    }
    //    catch (Exception e)
    //    {
    //        FailedToCopy.Add(new FailedCopy(Path.GetRelativePath(sourceRoot, src), e.Message));
    //    }
    //    report.CopedFiles = CopiedFiles;
    //    report.Failed = FailedToCopy;
    //    report.PercentageComplete = Convert.ToDouble(CopiedFiles.Count) / Convert.ToDouble(_allFilePaths.Count);
    //    progress.Report(report);
    //    return src;
    //}
}
