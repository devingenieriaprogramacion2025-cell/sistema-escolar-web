namespace SistemaEscolarWeb.Helpers;

public static class FileUploadHelper
{
    public static string SanitizeFileName(string fileName)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(c, '_');
        return fileName;
    }
}
