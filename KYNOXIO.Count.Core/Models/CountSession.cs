namespace KYNOXIO.Count.Core.Models;

/// <summary>
/// Representa una sesión local de conteo y conserva por separado
/// los resultados manuales y los resultados que produzca una IA en el futuro.
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

    // Datos capturados por el usuario para un conteo real asistido.
    public string ItemDescription { get; set; } = string.Empty;
    public int? ManualCount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string CountMethod { get; set; } = "Manual";
}