namespace EthanTcm.Application.Abstractions;

public sealed class TaxDocumentStorageOptions
{
    public const string SectionName = "DocumentStorage";

    public string RootPath { get; set; } = "App_Data/Documents";
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
    public string[] AllowedExtensions { get; set; } = ["pdf", "xlsx", "xls", "docx", "jpg", "png"];
    public string[] AllowedContentTypes { get; set; } =
    [
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "image/jpeg",
        "image/png"
    ];
}
