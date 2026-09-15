using dailyTimeApi.Models.Triage;

namespace dailyTimeApi.Models.Response;

public class ScoreFactorResponse
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Points { get; set; }
    /// <summary>0 = penalización.</summary>
    public int MaxPoints { get; set; }
    public string Detail { get; set; } = string.Empty;
}

public class DiscardReasonCountResponse
{
    public string Reason { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TriageSampleResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string PortalName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Resultado de recalcular puntaje y descarte sobre todas las ofertas.</summary>
public class RescoreJobOffersResponse
{
    /// <summary>true = vista previa: no se guardó nada.</summary>
    public bool DryRun { get; set; }
    public int Evaluated { get; set; }
    /// <summary>Ofertas no descartadas tras aplicar las reglas.</summary>
    public int ActiveAfter { get; set; }
    public int TierA { get; set; }
    public int TierB { get; set; }
    public int TierC { get; set; }
    /// <summary>Nuevas que pasan a descartadas por las reglas.</summary>
    public int NewlyDiscarded { get; set; }
    /// <summary>Descartadas por reglas que ya no cumplen ninguna y vuelven a "new".</summary>
    public int Restored { get; set; }
    /// <summary>Ofertas que las reglas descartarían pero cuyo estado decidiste tú (no se tocan).</summary>
    public int ProtectedByUser { get; set; }
    public List<DiscardReasonCountResponse> DiscardReasons { get; set; } = [];
    public List<TriageSampleResponse> Samples { get; set; } = [];
}

public class UpdateOfferTriageResponse
{
    public OfferTriageSettings Settings { get; set; } = new();
    public RescoreJobOffersResponse Result { get; set; } = new();
}
