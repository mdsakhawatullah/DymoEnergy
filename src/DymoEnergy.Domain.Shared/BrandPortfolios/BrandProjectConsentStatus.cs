namespace DymoEnergy.BrandPortfolios;

public enum BrandProjectConsentStatus
{
    /// <summary>Photo and the customer's written permission are both on file — safe to show.</summary>
    PhotoAndConsent = 0,
    WaitingOnConsent = 1,
    NoPhoto = 2
}
