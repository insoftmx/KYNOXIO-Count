using SQLite;

namespace KYNOXIO.Count.Infrastructure.Persistence;

/// <summary>
/// Modelo relacional interno de SQLite; se mantiene separado del modelo de dominio.
/// </summary>
[Table("CountSessions")]
internal sealed class CountSessionRecord
{
    [PrimaryKey]
    [MaxLength(32)]
    public string Id { get; set; } = string.Empty;

    [Indexed]
    [MaxLength(40)]
    public string Folio { get; set; } = string.Empty;

    [Indexed]
    public DateTime CreatedAtUtc { get; set; }

    [MaxLength(260)]
    public string ImageRelativePath { get; set; } = string.Empty;

    [MaxLength(24)]
    public string Status { get; set; } = "Pending";

    public int? DetectedCount { get; set; }
    public double? Confidence { get; set; }

    [MaxLength(160)]
    public string ItemDescription { get; set; } = string.Empty;

    public int? ManualCount { get; set; }

    [MaxLength(1000)]
    public string Notes { get; set; } = string.Empty;

    [MaxLength(24)]
    public string CountMethod { get; set; } = "Manual";
}