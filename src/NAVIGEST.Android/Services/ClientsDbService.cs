// File: NAVIGEST.Android/Services/ClientsDbService.cs
#nullable enable
using System.Data;
using MySqlConnector;
using NAVIGEST.Android.Models;

namespace NAVIGEST.Android.Services
{
    /// <summary>
    /// Serviço de acesso a MariaDB para a tabela CLIENTES.
    /// Usa o mesmo modelo de AppSettingsService/DbSettings do projeto.
    /// </summary>
    public sealed class ClientsDbService
    {
        private readonly AppSettingsService _settings = new();

        private string GetConnectionString()
        {
            var s = _settings.Load();
            var csb = new MySqlConnectionStringBuilder
            {
                Server = s.Server,
                Port = s.Port,
                Database = s.Database,
                UserID = s.UserId,
                Password = s.Password,
                SslMode = s.SslMode,
                AllowPublicKeyRetrieval = s.AllowPublicKeyRetrieval,
                ConnectionTimeout = (uint)s.ConnectionTimeout,
                DefaultCommandTimeout = (uint)s.DefaultCommandTimeout,
            };
            return csb.ConnectionString;
        }

        private MySqlConnection CreateConnection() => new MySqlConnection(GetConnectionString());

        public async Task<List<Cliente>> ListAsync(CancellationToken ct = default)
        {
            var list = new List<Cliente>();
            const string sql = @"
                SELECT 
                    CLICODIGO, CLINOME, TELEFONE, EMAIL, EXTERNO, ANULADO, VENDEDOR, VALORCREDITO, PastasSincronizadas
                FROM CLIENTES
                ORDER BY CLICODIGO;";

            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync(ct);
                using var cmd = new MySqlCommand(sql, conn);
                using var rd = await cmd.ExecuteReaderAsync(ct);
                while (await rd.ReadAsync(ct))
                {
                    list.Add(new Cliente
                    {
                        CLICODIGO = rd.GetString("CLICODIGO").Trim(),
                        CLINOME = rd.IsDBNull("CLINOME") ? null : rd.GetString("CLINOME").TrimEnd(),
                        TELEFONE = rd.IsDBNull("TELEFONE") ? null : rd.GetString("TELEFONE").TrimEnd(),
                        EMAIL = rd.IsDBNull("EMAIL") ? null : rd.GetString("EMAIL"),
                        EXTERNO = !rd.IsDBNull("EXTERNO") && rd.GetBoolean("EXTERNO"),
                        ANULADO = !rd.IsDBNull("ANULADO") && rd.GetBoolean("ANULADO"),
                        VENDEDOR = rd.IsDBNull("VENDEDOR") ? null : rd.GetString("VENDEDOR"),
                        VALORCREDITO = rd.IsDBNull("VALORCREDITO") ? null : rd.GetString("VALORCREDITO").TrimEnd(),
                        PastasSincronizadas = !rd.IsDBNull("PastasSincronizadas") && rd.GetBoolean("PastasSincronizadas")
                    });
                }
                return list;
            }
            catch (Exception)
            {
                await AppShell.DisplayToastAsync("Erro ao carregar clientes.");
                throw;
            }
        }

        public async Task<bool> UpsertAsync(Cliente c, CancellationToken ct = default)
        {
            const string sql = @"
INSERT INTO CLIENTES
  (CLICODIGO, CLINOME, TELEFONE, EMAIL, EXTERNO, ANULADO, VENDEDOR, VALORCREDITO, PastasSincronizadas)
VALUES
  (@CLICODIGO, @CLINOME, @TELEFONE, @EMAIL, @EXTERNO, @ANULADO, @VENDEDOR, @VALORCREDITO, @PastasSincronizadas)
ON DUPLICATE KEY UPDATE
  CLINOME = VALUES(CLINOME),
  TELEFONE = VALUES(TELEFONE),
  EMAIL = VALUES(EMAIL),
  EXTERNO = VALUES(EXTERNO),
  ANULADO = VALUES(ANULADO),
  VENDEDOR = VALUES(VENDEDOR),
  VALORCREDITO = VALUES(VALORCREDITO),
  PastasSincronizadas = VALUES(PastasSincronizadas);";
            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync(ct);
                using var cmd = new MySqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("@CLICODIGO", c.CLICODIGO);
                cmd.Parameters.AddWithValue("@CLINOME", (object?)c.CLINOME ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TELEFONE", (object?)c.TELEFONE ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EMAIL", (object?)c.EMAIL ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EXTERNO", c.EXTERNO);
                cmd.Parameters.AddWithValue("@ANULADO", c.ANULADO);
                cmd.Parameters.AddWithValue("@VENDEDOR", (object?)c.VENDEDOR ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@VALORCREDITO", (object?)c.VALORCREDITO ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PastasSincronizadas", c.PastasSincronizadas);

                var rows = await cmd.ExecuteNonQueryAsync(ct);
                return rows > 0;
            }
            catch (Exception)
            {
                await AppShell.DisplayToastAsync("Erro ao gravar cliente.");
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string cliCodigo, CancellationToken ct = default)
        {
            const string sql = @"DELETE FROM CLIENTES WHERE CLICODIGO = @id;";
            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync(ct);
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", cliCodigo);
                var rows = await cmd.ExecuteNonQueryAsync(ct);
                return rows > 0;
            }
            catch (Exception)
            {
                await AppShell.DisplayToastAsync("Erro ao eliminar cliente.");
                throw;
            }
        }
    }
}
