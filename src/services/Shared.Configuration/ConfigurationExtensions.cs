using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace Shared.Configuration;

/// <summary>
/// Extension methods for configuring Azure Key Vault integration with IConfigurationBuilder.
/// Provides centralized configuration management for local (user-secrets) and Azure (Key Vault) environments.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds Azure Key Vault as a configuration source using managed identity in Azure environments.
    /// In local development, configuration comes from user-secrets (dotnet user-secrets) instead.
    /// 
    /// Configuration priority (highest to lowest):
    /// 1. Environment variables
    /// 2. Azure Key Vault (production)
    /// 3. User Secrets (local development)
    /// 4. appsettings.{Environment}.json
    /// 5. appsettings.json
    /// </summary>
    /// <param name="builder">The configuration builder</param>
    /// <param name="keyVaultUri">Key Vault URI (e.g., https://kv-events-prod.vault.azure.net/)</param>
    /// <returns>The configuration builder for chaining</returns>
    public static IConfigurationBuilder AddKeyVaultIfConfigured(
        this IConfigurationBuilder builder,
        string? keyVaultUri = null)
    {
        // Build intermediate configuration to check for Key Vault URI
        var config = builder.Build();
        var vaultUri = keyVaultUri ?? config["KeyVault:VaultUri"] ?? config["Azure:KeyVault:VaultUri"];

        // Only add Key Vault if URI is configured (Azure deployment)
        if (!string.IsNullOrEmpty(vaultUri))
        {
            try
            {
                var secretClient = new SecretClient(
                    new Uri(vaultUri),
                    new DefaultAzureCredential());

                builder.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());

                Console.WriteLine($"✓ Configured Azure Key Vault: {vaultUri}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Warning: Failed to connect to Key Vault: {ex.Message}");
                Console.WriteLine("  Falling back to local configuration sources");
            }
        }
        else
        {
            Console.WriteLine("ℹ Key Vault not configured - using local configuration (user-secrets/environment variables)");
        }

        return builder;
    }

    /// <summary>
    /// Validates that required configuration sections exist and contain non-empty values.
    /// Call this during application startup to fail fast if critical configuration is missing.
    /// </summary>
    /// <param name="configuration">The configuration to validate</param>
    /// <param name="requiredKeys">Array of required configuration keys (supports nested keys with ':')</param>
    /// <exception cref="InvalidOperationException">Thrown when required configuration is missing</exception>
    public static void ValidateRequiredConfiguration(
        this IConfiguration configuration,
        params string[] requiredKeys)
    {
        var missingKeys = new List<string>();

        foreach (var key in requiredKeys)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                missingKeys.Add(key);
            }
        }

        if (missingKeys.Any())
        {
            var errorMessage = $"Missing required configuration:\n  - {string.Join("\n  - ", missingKeys)}\n\n" +
                               "For local development, run: .\\setup-user-secrets.ps1\n" +
                               "For Azure deployment, ensure Key Vault contains these secrets.";

            throw new InvalidOperationException(errorMessage);
        }
    }

    /// <summary>
    /// Gets a strongly-typed configuration section with validation.
    /// Throws exception if section is missing or invalid.
    /// </summary>
    /// <typeparam name="T">The type to bind the configuration section to</typeparam>
    /// <param name="configuration">The configuration</param>
    /// <param name="sectionName">The section name</param>
    /// <returns>Bound and validated configuration object</returns>
    /// <exception cref="InvalidOperationException">Thrown when section is missing or invalid</exception>
    public static T GetRequiredSection<T>(this IConfiguration configuration, string sectionName) where T : class, new()
    {
        var section = configuration.GetSection(sectionName);
        if (!section.Exists())
        {
            throw new InvalidOperationException(
                $"Configuration section '{sectionName}' is missing. " +
                $"Ensure appsettings.json contains a '{sectionName}' section or Key Vault/user-secrets contain the values.");
        }

        var result = new T();
        section.Bind(result);

        return result;
    }
}
