namespace KYNOXIO.Count.Core.Models;

/// <summary>
/// Representa una sesión local de conteo. Los campos de IA permanecen vacíos
/// hasta que exista un resultado real del motor de análisis.
/// </summary>
public sealed class CountSession
{
    public string Id { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string ImageRelativePath { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public int? DetectedCount { get; set; }
    public double? Confidence { get; set; }
}