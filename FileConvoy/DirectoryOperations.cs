namespace FileConvoy;

public static class DirectoryOperations
{
    public static List<string[]> MakeFileCopyPaths(HashSet<string> allFilePaths, string sourceDir, string targetDir)
    {
        List<string[]> copyPaths = new List<string[]>();
        foreach (var file in allFilePaths)
        {
            var sourcePath = Path.Combine(sourceDir, file);
            var targetPath = Path.Combine(targetDir, file);
            string[] sourceTarget = new string[] { sourcePath, targetPath };
            copyPaths.Add(sourceTarget);
        }
        return copyPaths;
    }
    public static List<string> MakeDirectoryCopyPaths(HashSet<string> allDirs, string sourceDir, string targetDir)
    {
        List<string> dirPaths = new List<string>();
        foreach (var dir in allDirs)
        {
            var newRelativeDir = Path.GetRelativePath(sourceDir, dir);
            var newDirComplete = Path.Combine(targetDir, newRelativeDir);
            dirPaths.Add(newDirComplete);
        }
        return dirPaths;
    }
    public static (bool isEqual, HashSet<string> inSrcOnly, HashSet<string> inDstOnly) CheckParity(HashSet<string> sourceDirectory, HashSet<string> targetDirectory)
    {
        var isEqual = sourceDirectory.Equals(targetDirectory);
        sourceDirectory.ExceptWith(targetDirectory);
        targetDirectory.ExceptWith(sourceDirectory);
        return (isEqual, sourceDirectory, targetDirectory);
    }
}