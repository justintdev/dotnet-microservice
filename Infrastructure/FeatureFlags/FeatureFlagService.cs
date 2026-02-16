using Application.Interfaces;
using Microsoft.FeatureManagement;

namespace Infrastructure.FeatureFlags;

public sealed class FeatureFlagService : IFeatureFlagService
{
    private readonly IFeatureManager _featureManager;

    public FeatureFlagService(IFeatureManager featureManager)
    {
        _featureManager = featureManager;
    }

    public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        return _featureManager.IsEnabledAsync(featureName);
    }
}
