namespace KYNOXIO.Count.Core.Models;

/// <summary>
/// Muestra independiente para construir y validar el conjunto de entrenamiento.
/// No representa una sesión operativa de conteo.
/// </summary>
public sealed class TrainingSample
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string ImageRelativePath { get; set; } = string.Empty;

    /// <summary>IndividualParts o StackedMaterials.</summary>
    public string MaterialGroup { get; set; } = string.Empty;

    /// <summary>Nombre normalizado de la clase, por ejemplo Rondana galvanizada o Polín de madera.</summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>Cantidad comprobada físicamente por la persona; nunca es una predicción.</summary>
    public int? VerifiedCount { get; set; }

    /// <summary>Condiciones de captura en formato JSON para permitir ampliar el esquema.</summary>
    public string CaptureConditionsJson { get; set; } = "{}";

    /// <summary>Anotaciones geométricas versionadas en JSON; [] mientras no existan.</summary>
    public string AnnotationsJson { get; set; } = "[]";

    /// <summary>Draft, Ready o Excluded.</summary>
    public string Status { get; set; } = "Draft";

    public string Notes { get; set; } = string.Empty;
}