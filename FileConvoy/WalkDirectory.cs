namespace FileConvoy;

    public class WalkDirectory
{
    public string Dir { get; set; }
    public HashSet<string> AllFiles { get; }
    public HashSet<string> AllDirs { get; }
    public List<string> FailedDirs { get; }
    public List<string> FailedFiles { get; }

    public WalkDirectory(string directory)
    {
        Dir = directory;
        AllFiles = new HashSet<string>();
        AllDirs = new HashSet<string>();
        FailedDirs = new List<string>();
        FailedFiles = new List<string>();
    }

    public async Task WalkDirectoryAsync()
    {
        var src = new DirectoryInfo(Dir);
        await Task.WhenAll(RetrieveFilesAsync(src), GetSubDirsAsync(src));
        await FilesFromDirsAsync();
    }

    private async Task FilesFromDirsAsync()
    {
        List<Task> tasks = new();
        foreach (var dir in AllDirs)
        {
            var dirPath = Path.Combine(this.Dir, dir);
            var dirInfo = new DirectoryInfo(dirPath);
            //tasks.Add(Task.Run(() => RetrieveFilesAsync(dirInfo)));
            tasks.Add(RetrieveFilesAsync(dirInfo));
        }
        await Task.WhenAll(tasks);
    }
    private async Task GetSubDirsAsync(DirectoryInfo src)
    {
        try
        {
            var subDirs = src.GetDirectories();
            foreach (DirectoryInfo dir in subDirs)
            {
                AllDirs.Add(dir.FullName);
                await GetSubDirsAsync(dir);
            }
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine($"Cannot access one or more subdirectories in {src.FullName}");
            FailedDirs.Add(src.FullName);
        }
        catch (System.Security.SecurityException)
        {
            Console.WriteLine($"Security error when opening one or more subdirectories in {src.FullName}");
            FailedDirs.Add(src.FullName);
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine($"Permission denied to access deeper than {src.FullName}");
            FailedDirs.Add(src.FullName);
        }
    }

    private async Task RetrieveFilesAsync(DirectoryInfo src)
    {
        FileInfo[] paths = null;
        try
        {
            paths = await Task.Run(() => src.GetFiles());
            //paths = src.GetFiles();
        }
        catch (DirectoryNotFoundException e)
        {
            Console.WriteLine($"It looks like the entire source folder was changed.\n{e}");
            FailedFiles.Add(src.FullName);
        }
        if (paths != null)
        {
            foreach (var file in paths)
            {
                AllFiles.Add(Path.GetRelativePath(Dir, file.FullName));
            }
        }
    }
}
