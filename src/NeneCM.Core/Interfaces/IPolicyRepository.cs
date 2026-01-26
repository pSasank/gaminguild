using NeneCM.Core.Models;

namespace NeneCM.Core.Interfaces;

/// <summary>
/// Interface for policy data access.
/// Allows swapping implementations (mock for testing, SQLite for production).
/// </summary>
public interface IPolicyRepository
{
    /// <summary>
    /// Gets all available policies.
    /// </summary>
    IEnumerable<Policy> GetAllPolicies();

    /// <summary>
    /// Gets policies filtered by category.
    /// </summary>
    IEnumerable<Policy> GetPoliciesByCategory(string category);

    /// <summary>
    /// Gets a specific policy by ID.
    /// </summary>
    Policy? GetPolicyById(int id);

    /// <summary>
    /// Gets all effects for a policy.
    /// </summary>
    IEnumerable<PolicyEffect> GetPolicyEffects(int policyId);
}
