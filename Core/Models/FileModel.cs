namespace Helion.Models;

public class FileModel
{
    public static readonly FileModel Default = new FileModel();
    public string? FileName { get; set; }
    public string? MD5 { get; set; }
}

