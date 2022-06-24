namespace FileConvoy;

public class FailedCopy
{
    public string FullPath { get; set; }
    public string ErrorMessage { get; set; }

    public FailedCopy(string fullPath, string errorMessage)
    {
        FullPath = fullPath;
        ErrorMessage = errorMessage;
    }
}
