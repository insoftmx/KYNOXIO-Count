using SQLite;

namespace KYNOXIO.Count.Infrastructure.Persistence;

/// <summary>Registro SQLite independiente de las sesiones operativas.</summary>
[Table("TrainingSamples")]
internal sealed class TrainingSampleRecord
{
    [PrimaryKey, MaxLength(32)]
    public string Id { get; set; } = string.Empty;

    [Indexed]
    public DateTime CreatedAtUtc { get; set; }

    [MaxLength(260)]
    public string ImageRelativePath { get; set; } = string.Empty;

    [Indexed, MaxLength(32)]
    public string MaterialGroup { get; set; } = string.Empty;

    [Indexed, MaxLength(160)]
    public string ClassName { get; set; } = string.Empty;

    public int? VerifiedCount { get; set; }

    public string CaptureConditionsJson { get; set; } = "{}";
    public string AnnotationsJson { get; set; } = "[]";

    [Indexed, MaxLength(16)]
    public string Status { get; set; } = "Draft";

    [MaxLength(1000)]
    public string Notes { get; set; } = string.Empty;
}