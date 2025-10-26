// File: NAVIGEST.iOS/Services/ClientsDbService.cs
#nullable enable
using System;
using System.Data;
using MySqlConnector;
using NAVIGEST.iOS.Models;

namespace NAVIGEST.iOS.Services
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

        private static async Task EnsureIndicativoColumnAsync(MySqlConnection conn, CancellationToken ct)
        {
            const string checkNew = @"
                SELECT COUNT(*)
                FROM information_schema.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'CLIENTES'
                  AND COLUMN_NAME = 'INDICATIVO';";
            using var cmdCheckNew = new MySqlCommand(checkNew, conn);
            var existsNew = Convert.ToInt32(await cmdCheckNew.ExecuteScalarAsync(ct)) > 0;
            if (existsNew) return;

            const string checkOld = @"
                SELECT COUNT(*)
                FROM information_schema.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'CLIENTES'
                  AND COLUMN_NAME = 'IONDICATIVO';";
            using var cmdCheckOld = new MySqlCommand(checkOld, conn);
            var existsOld = Convert.ToInt32(await cmdCheckOld.ExecuteScalarAsync(ct)) > 0;

            if (existsOld)
            {
                try
                {
                    const string rename = "ALTER TABLE CLIENTES CHANGE COLUMN IONDICATIVO INDICATIVO VARCHAR(12) NULL AFTER TELEFONE;";
                    using var cmdRename = new MySqlCommand(rename, conn);
                    await cmdRename.ExecuteNonQueryAsync(ct);
                    return;
                }
                catch (Exception exRename)
                {
                    System.Diagnostics.Debug.WriteLine("[ClientsDbService] Falha ao renomear coluna IONDICATIVO: " + exRename.Message);
                }
            }

            try
            {
                const string alter = "ALTER TABLE CLIENTES ADD COLUMN INDICATIVO VARCHAR(12) NULL AFTER TELEFONE;";
                using var cmdAlter = new MySqlCommand(alter, conn);
                await cmdAlter.ExecuteNonQueryAsync(ct);

                if (existsOld)
                {
                    const string copy = "UPDATE CLIENTES SET INDICATIVO = IONDICATIVO WHERE INDICATIVO IS NULL AND IONDICATIVO IS NOT NULL;";
                    using var cmdCopy = new MySqlCommand(copy, conn);
                    await cmdCopy.ExecuteNonQueryAsync(ct);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[ClientsDbService] Falha a criar coluna INDICATIVO: " + ex.Message);
            }
        }

        public async Task<List<Cliente>> ListAsync(CancellationToken ct = default)
        {
            var list = new List<Cliente>();
            const string sql = @"
                SELECT 
                    CLICODIGO, CLINOME, TELEFONE, INDICATIVO, EMAIL, EXTERNO, ANULADO, VENDEDOR, VALORCREDITO, PastasSincronizadas
                FROM CLIENTES
                ORDER BY CLICODIGO;";

            try
            {
                using var conn = CreateConnection();
                await conn.OpenAsync(ct);
                await EnsureIndicativoColumnAsync(conn, ct);
                using var cmd = new MySqlCommand(sql, conn);
                using var rd = await cmd.ExecuteReaderAsync(ct);
                while (await rd.ReadAsync(ct))
                {
                    list.Add(new Cliente
                    {
                        CLICODIGO = rd.GetString("CLICODIGO").Trim(),
                        CLINOME = rd.IsDBNull("CLINOME") ? null : rd.GetString("CLINOME").TrimEnd(),
                        TELEFONE = rd.IsDBNull("TELEFONE") ? null : rd.GetString("TELEFONE").TrimEnd(),
                        INDICATIVO = rd.IsDBNull("INDICATIVO") ? null : rd.GetString("INDICATIVO").TrimEnd(),
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
    (CLICODIGO, CLINOME, TELEFONE, INDICATIVO, EMAIL, EXTERNO, ANULADO, VENDEDOR, VALORCREDITO, PastasSincronizadas)
VALUES
    (@CLICODIGO, @CLINOME, @TELEFONE, @INDICATIVO, @EMAIL, @EXTERNO, @ANULADO, @VENDEDOR, @VALORCREDITO, @PastasSincronizadas)
ON DUPLICATE KEY UPDATE
  CLINOME = VALUES(CLINOME),
    TELEFONE = VALUES(TELEFONE),
    INDICATIVO = VALUES(INDICATIVO),
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
                await EnsureIndicativoColumnAsync(conn, ct);
                using var cmd = new MySqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("@CLICODIGO", c.CLICODIGO);
                cmd.Parameters.AddWithValue("@CLINOME", (object?)c.CLINOME ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TELEFONE", (object?)c.TELEFONE ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@INDICATIVO", string.IsNullOrWhiteSpace(c.INDICATIVO) ? DBNull.Value : c.INDICATIVO);
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
