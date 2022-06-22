namespace FileConvoy;

public class DownloadProgress
{
    public HashSet<string> CopedFiles { get; set; }
    public HashSet<FailedCopy> Failed { get; set; }
    public double PercentageComplete { get; set; }
}