namespace MiniErp.Domain.Customers;

/// <summary>Condición frente al IVA (Argentina).</summary>
public enum VatCondition
{
    /// <summary>Responsable Inscripto.</summary>
    RegisteredTaxpayer,

    /// <summary>Monotributista.</summary>
    SimplifiedRegime,

    /// <summary>Exento.</summary>
    Exempt,

    /// <summary>Consumidor Final.</summary>
    FinalConsumer,
}
