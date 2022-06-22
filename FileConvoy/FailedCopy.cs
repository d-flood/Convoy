namespace FileConvoy;

public class FailedCopy
{
    public string FullPath;
    public string ErrorMessage;

    public FailedCopy(string fullPath, string errorMessage)
    {
        FullPath = fullPath;
        ErrorMessage = errorMessage;
    }
}
