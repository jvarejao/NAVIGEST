namespace NAVIGEST.Shared.Helpers;

/// <summary>
/// Helper para comparação de versões semânticas.
/// 
/// Exemplo:
/// VersionComparer.IsLessThan("1.0.9", "1.0.10") → true
/// VersionComparer.IsLessThan("1.0.10", "1.0.9") → false
/// VersionComparer.IsLessThan("1.0.10", "1.0.10") → false
/// VersionComparer.IsLessThan("2.0.0", "1.9.9") → false
/// </summary>
public static class VersionComparer
{
    /// <summary>
    /// Compara duas versões no formato "major.minor.patch".
    /// 
    /// Returns:
    /// - true se versionA < versionB
    /// - false se versionA >= versionB
    /// 
    /// Usa System.Version internamente para comparação semântica.
    /// Se parsing falhar, assume versão inválida e retorna false.
    /// </summary>
    public static bool IsLessThan(string versionA, string versionB)
    {
        try
        {
            // Extrai apenas major.minor.patch (ignora sufixos como "-beta", "+build")
            var cleanA = ExtractVersionNumber(versionA);
            var cleanB = ExtractVersionNumber(versionB);

            // Tenta fazer parse para System.Version
            if (!System.Version.TryParse(cleanA, out var parsedA))
                return false;

            if (!System.Version.TryParse(cleanB, out var parsedB))
                return false;

            return parsedA < parsedB;
        }
        catch
        {
            // Em caso de erro, considera que não precisa atualizar
            return false;
        }
    }

    /// <summary>
    /// Compara duas versões.
    /// 
    /// Returns:
    /// - true se versionA <= versionB
    /// - false caso contrário
    /// </summary>
    public static bool IsLessThanOrEqual(string versionA, string versionB)
    {
        try
        {
            var cleanA = ExtractVersionNumber(versionA);
            var cleanB = ExtractVersionNumber(versionB);

            if (!System.Version.TryParse(cleanA, out var parsedA))
                return false;

            if (!System.Version.TryParse(cleanB, out var parsedB))
                return false;

            return parsedA <= parsedB;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extrai apenas a parte numérica da versão.
    /// 
    /// Exemplos:
    /// "1.0.5" → "1.0.5"
    /// "1.0.5-beta" → "1.0.5"
    /// "1.0.5+build.123" → "1.0.5"
    /// "v1.0.5" → "1.0.5"
    /// </summary>
    private static string ExtractVersionNumber(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return "0.0.0";

        version = version.Trim();

        // Remove prefixo "v" ou "V" se existir
        if (version.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            version = version.Substring(1);

        // Extrai apenas até ao primeiro caractere não-numérico/ponto
        var endIndex = version.Length;
        for (int i = 0; i < version.Length; i++)
        {
            if (!char.IsDigit(version[i]) && version[i] != '.')
            {
                endIndex = i;
                break;
            }
        }

        var cleaned = version.Substring(0, endIndex).Trim('.');

        // Garante que tem pelo menos 3 componentes (major.minor.patch)
        var parts = cleaned.Split('.');
        if (parts.Length < 3)
        {
            // Completa com zeros se necessário: "1.0" → "1.0.0"
            for (int i = parts.Length; i < 3; i++)
            {
                cleaned += ".0";
            }
        }

        return cleaned;
    }
}
