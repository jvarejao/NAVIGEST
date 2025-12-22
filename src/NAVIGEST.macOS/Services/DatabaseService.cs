using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using NAVIGEST.macOS.Models;
using System.IO;
using System.Linq;
using System.Globalization;

namespace NAVIGEST.macOS.Services
{
    public static partial class DatabaseService
    {
        private static readonly AppSettingsService _settingsService = new();

        private static string GetConnectionString()
        {
            var s = _settingsService.Load();
            var builder = new MySqlConnectionStringBuilder
            {
                Server = s.Server,
                Port = s.Port,
                Database = s.Database,
                UserID = s.UserId,
                Password = s.Password ?? string.Empty,
                SslMode = s.SslMode,
                AllowPublicKeyRetrieval = s.AllowPublicKeyRetrieval,
                ConnectionTimeout = (uint)s.ConnectionTimeout,
                DefaultCommandTimeout = (uint)s.DefaultCommandTimeout
            };
            return builder.ConnectionString;
        }

        public static async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro de ligação: {ex.Message}");
                return false;
            }
        }

        // ================= LOGIN / USERS =================
        public static async Task<(bool Ok, string? Nome, string? Tipo)> CheckLoginAsync(string username, string password)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            const string sql = @"
                SELECT Username, Password, Name, TipoUtilizador
                FROM REGISTRATION
                WHERE Username = @u
                LIMIT 1;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.Add("@u", MySqlDbType.VarChar, 30).Value = username;

            string? dbPass = null;
            string? nome = null;
            string? tipo = null;

            using (var rd = await cmd.ExecuteReaderAsync())
            {
                if (!await rd.ReadAsync())
                    return (false, null, null);

                dbPass = rd.IsDBNull(rd.GetOrdinal("Password")) ? null : rd.GetString("Password");
                nome = rd.IsDBNull(rd.GetOrdinal("Name")) ? null : rd.GetString("Name");
                tipo = rd.IsDBNull(rd.GetOrdinal("TipoUtilizador")) ? null : rd.GetString("TipoUtilizador");
            }

            bool ok;
            if (!string.IsNullOrEmpty(dbPass) && dbPass.StartsWith("$2"))
            {
                try { ok = BCrypt.Net.BCrypt.Verify(password, dbPass); }
                catch { ok = false; }
            }
            else
            {
                ok = string.Equals(dbPass ?? string.Empty, password, StringComparison.Ordinal);
                if (ok && (dbPass?.Length ?? 0) <= 30)
                {
                    try
                    {
                        string hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 10);
                        const string up = "UPDATE REGISTRATION SET Password = @h WHERE Username = @u LIMIT 1;";
                        using var upCmd = new MySqlCommand(up, conn);
                        upCmd.Parameters.Add("@h", MySqlDbType.VarChar, 100).Value = hash;
                        upCmd.Parameters.Add("@u", MySqlDbType.VarChar, 30).Value = username;
                        await upCmd.ExecuteNonQueryAsync();
                    }
                    catch { /* ignore */ }
                }
            }

            return (ok, ok ? nome : null, ok ? tipo : null);
        }

        public static async Task<bool> UserExistsAsync(string username)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();
            const string sql = "SELECT 1 FROM REGISTRATION WHERE Username = @u LIMIT 1;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.Add("@u", MySqlDbType.VarChar, 30).Value = username;
            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync();
        }

        public static async Task<bool> GravarUtilizadorAsync(Registration reg)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            const string sql = @"
                INSERT INTO REGISTRATION
                (Username, Password, Name, ContactNo, Categoria1, Categoria2, TipoUtilizador, email, profilepicture)
                VALUES
                (@username, @password, @name, @contact, @cat1, @cat2, @tipo, @mail, @foto);";

            string passToStore = reg.Password ?? "";
            if (!string.IsNullOrWhiteSpace(passToStore) && passToStore.Length <= 30)
                passToStore = BCrypt.Net.BCrypt.HashPassword(passToStore, workFactor: 10);

            using var cmd = new MySqlCommand(sql, conn) { CommandTimeout = 90 };
            cmd.Parameters.Add("@username", MySqlDbType.VarChar, 30).Value = reg.Username ?? "";
            cmd.Parameters.Add("@password", MySqlDbType.VarChar, 100).Value = passToStore;
            cmd.Parameters.Add("@name", MySqlDbType.VarChar, 30).Value = reg.Name ?? "";
            cmd.Parameters.Add("@contact", MySqlDbType.VarChar, 15).Value = reg.ContactNo ?? "";
            cmd.Parameters.Add("@cat1", MySqlDbType.VarChar, 30).Value = reg.Categoria1 ?? "";
            cmd.Parameters.Add("@cat2", MySqlDbType.VarChar, 30).Value = reg.Categoria2 ?? "";
            cmd.Parameters.Add("@tipo", MySqlDbType.VarChar, 20).Value = reg.TipoUtilizador ?? "";
            cmd.Parameters.Add("@mail", MySqlDbType.VarChar, 50).Value = reg.Email ?? "";
            var pFoto = cmd.Parameters.Add("@foto", MySqlDbType.LongBlob);
            pFoto.Value = (object?)reg.ProfilePicture ?? DBNull.Value;

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public static async Task<bool> UpdateUserAsync(Registration reg)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            string? pass = reg.Password;
            string? passParam = null;
            if (!string.IsNullOrWhiteSpace(pass))
                passParam = BCrypt.Net.BCrypt.HashPassword(pass, workFactor: 10);

            const string sql = @"
                UPDATE REGISTRATION
                SET
                    Password = COALESCE(@password, Password),
                    Name = @name,
                    ContactNo = @contact,
                    Categoria1 = @cat1,
                    Categoria2 = @cat2,
                    TipoUtilizador = @tipo,
                    email = @mail,
                    profilepicture = COALESCE(@foto, profilepicture)
                WHERE Username = @username
                LIMIT 1;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.Add("@username", MySqlDbType.VarChar, 30).Value = reg.Username ?? "";
            cmd.Parameters.Add("@password", MySqlDbType.VarChar, 100).Value = (object?)passParam ?? DBNull.Value;
            cmd.Parameters.Add("@name", MySqlDbType.VarChar, 30).Value = reg.Name ?? "";
            cmd.Parameters.Add("@contact", MySqlDbType.VarChar, 15).Value = reg.ContactNo ?? "";
            cmd.Parameters.Add("@cat1", MySqlDbType.VarChar, 30).Value = reg.Categoria1 ?? "";
            cmd.Parameters.Add("@cat2", MySqlDbType.VarChar, 30).Value = reg.Categoria2 ?? "";
            cmd.Parameters.Add("@tipo", MySqlDbType.VarChar, 20).Value = reg.TipoUtilizador ?? "";
            cmd.Parameters.Add("@mail", MySqlDbType.VarChar, 50).Value = reg.Email ?? "";
            var pFoto = cmd.Parameters.Add("@foto", MySqlDbType.LongBlob);
            pFoto.Value = (object?)reg.ProfilePicture ?? DBNull.Value;
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public static async Task<bool> DeleteUserAsync(string username)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();
            const string sql = "DELETE FROM REGISTRATION WHERE Username = @u LIMIT 1;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.Add("@u", MySqlDbType.VarChar, 30).Value = username;
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public static async Task<List<Registration>> GetUsersAsync()
        {
            var list = new List<Registration>();
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();
            const string sql = @"
                SELECT Username, Password, Name, ContactNo, Categoria1, Categoria2, TipoUtilizador, email, profilepicture
                FROM REGISTRATION
                ORDER BY Username;";
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Registration
                {
                    Username = reader.GetString("Username"),
                    Password = reader.IsDBNull(reader.GetOrdinal("Password")) ? "" : reader.GetString("Password"),
                    Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? "" : reader.GetString("Name"),
                    ContactNo = reader.IsDBNull(reader.GetOrdinal("ContactNo")) ? "" : reader.GetString("ContactNo"),
                    Categoria1 = reader.IsDBNull(reader.GetOrdinal("Categoria1")) ? "" : reader.GetString("Categoria1"),
                    Categoria2 = reader.IsDBNull(reader.GetOrdinal("Categoria2")) ? "" : reader.GetString("Categoria2"),
                    TipoUtilizador = reader.IsDBNull(reader.GetOrdinal("TipoUtilizador")) ? "" : reader.GetString("TipoUtilizador"),
                    Email = reader.IsDBNull(reader.GetOrdinal("email")) ? "" : reader.GetString("email"),
                    ProfilePicture = reader.IsDBNull(reader.GetOrdinal("profilepicture")) ? null : (byte[])reader["profilepicture"]
                });
            }
            return list;
        }

        public sealed class UserInfo
        {
            public string Username { get; init; } = "";
            public string? Email { get; init; }
            public byte[]? ProfilePicture { get; init; }
        }

        public static async Task<UserInfo?> TryGetUserPhotoAndEmailAsync(string username)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();
            const string sql = @"
                SELECT Username, email, profilepicture
                FROM REGISTRATION
                WHERE Username = @u
                LIMIT 1;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.Add("@u", MySqlDbType.VarChar, 30).Value = username;
            using var rd = await cmd.ExecuteReaderAsync();
            if (!await rd.ReadAsync()) return null;
            return new UserInfo
            {
                Username = rd.GetString("Username"),
                Email = rd.IsDBNull(rd.GetOrdinal("email")) ? null : rd.GetString("email"),
                ProfilePicture = rd.IsDBNull(rd.GetOrdinal("profilepicture")) ? null : (byte[])rd["profilepicture"]
            };
        }

        // ================= PASSWORD RESET =================
        private static async Task EnsureResetTableAsync(MySqlConnection conn)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS password_reset_tokens (
                  Username   VARCHAR(30) NOT NULL,
                  Token      VARCHAR(12) NOT NULL,
                  ExpiresAt  DATETIME    NOT NULL,
                  PRIMARY KEY (Username, Token)
                );";
            using var cmd = new MySqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<string> CreatePasswordResetTokenAsync(string username, TimeSpan validity)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            if (!await UserExistsAsync(username))
                throw new InvalidOperationException("Utilizador inexistente.");

            await EnsureResetTableAsync(conn);

            var bytes = new byte[4];
            RandomNumberGenerator.Fill(bytes);
            var code = (BitConverter.ToUInt32(bytes, 0) % 1_000_000).ToString("D6");

            var expires = DateTime.UtcNow.Add(validity);

            const string ins = @"
                INSERT INTO password_reset_tokens (Username, Token, ExpiresAt)
                VALUES (@u, @t, @e);";
            using var cmd = new MySqlCommand(ins, conn);
            cmd.Parameters.Add("@u", MySqlDbType.VarChar, 30).Value = username;
            cmd.Parameters.Add("@t", MySqlDbType.VarChar, 12).Value = code;
            cmd.Parameters.Add("@e", MySqlDbType.DateTime).Value = expires;
            await cmd.ExecuteNonQueryAsync();

            return code;
        }

        public static async Task<bool> ValidateTokenAndResetPasswordAsync(string username, string token, string newPassword)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            await EnsureResetTableAsync(conn);

            const string sel = @"
                SELECT ExpiresAt
                FROM password_reset_tokens
                WHERE Username = @u AND Token = @t
                LIMIT 1;";
            using var cmdSel = new MySqlCommand(sel, conn);
            cmdSel.Parameters.Add("@u", MySqlDbType.VarChar, 30).Value = username;
            cmdSel.Parameters.Add("@t", MySqlDbType.VarChar, 12).Value = token;

            DateTime? expires = null;
            using (var rd = await cmdSel.ExecuteReaderAsync())
            {
                if (await rd.ReadAsync())
                    expires = rd.GetDateTime("ExpiresAt");
                else
                    return false;
            }

            if (expires == null || DateTime.UtcNow > expires.Value)
            {
                const string delExp = "DELETE FROM password_reset_tokens WHERE Username = @u AND Token = @t;";
                using var cmdDelExp = new MySqlCommand(delExp, conn);
                cmdDelExp.Parameters.Add("@u", MySqlDbType.VarChar, 30).Value = username;
                cmdDelExp.Parameters.Add("@t", MySqlDbType.VarChar, 12).Value = token;
                await cmdDelExp.ExecuteNonQueryAsync();
                return false;
            }

            var hash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 10);
            const string up = "UPDATE REGISTRATION SET Password = @p WHERE Username = @u LIMIT 1;";
            using (var cmdUp = new MySqlCommand(up, conn))
            {
                cmdUp.Parameters.Add("@p", MySqlDbType.VarChar, 100).Value = hash;
                cmdUp.Parameters.Add("@u", MySqlDbType.VarChar, 30).Value = username;
                var rows = await cmdUp.ExecuteNonQueryAsync();
                if (rows <= 0) return false;
            }

            const string del = "DELETE FROM password_reset_tokens WHERE Username = @u AND Token = @t;";
            using (var cmdDel = new MySqlCommand(del, conn))
            {
                cmdDel.Parameters.Add("@u", MySqlDbType.VarChar, 30).Value = username;
                cmdDel.Parameters.Add("@t", MySqlDbType.VarChar, 12).Value = token;
                await cmdDel.ExecuteNonQueryAsync();
            }

            return true;
        }

        // ================= COMPANIES (RESTORED) =================
        public sealed class CompanyInfo
        {
            public string CodEmp { get; init; } = "";
            public string? Empresa { get; init; }
            public string? Ano { get; init; }
            public byte[]? Logotipo { get; init; }
            public string? Morada { get; init; }
            public string? Localidade { get; init; }
            public string? CodPostal { get; init; }
            public string? Nif { get; init; }
        }

        public static async Task<List<CompanyInfo>> GetActiveCompaniesAsync()
        {
            var list = new List<CompanyInfo>();
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            const string sql = @"
                SELECT *
                FROM SETUP
                WHERE Ativa = '1'
                ORDER BY Empresa;";

            using var cmd = new MySqlCommand(sql, conn);
            using var rd = await cmd.ExecuteReaderAsync();
            
            // Helpers
            static string S(MySqlConnector.MySqlDataReader r, string col) 
            {
                try { return r.IsDBNull(r.GetOrdinal(col)) ? "" : r.GetString(col); }
                catch { return ""; } // Column might not exist
            }

            while (await rd.ReadAsync())
            {
                list.Add(new CompanyInfo
                {
                    CodEmp = S(rd, "CodEmp"),
                    Empresa = S(rd, "Empresa"),
                    Ano = S(rd, "Ano"),
                    Logotipo = rd.IsDBNull(rd.GetOrdinal("Logotipo")) ? null : (byte[])rd["Logotipo"],
                    Morada = S(rd, "Morada"),
                    Localidade = S(rd, "Localidade"),
                    CodPostal = S(rd, "CodPostal"),
                    Nif = S(rd, "Nif") // Assuming column name is Nif or NIF
                });
            }
            return list;
        }

     
        // ================= CLIENTES =================

        private static async Task EnsureClienteSeqTableAsync(MySqlConnection conn, CancellationToken ct)
        {
            // Tabela de sequência (apenas coluna AUTO_INCREMENT)
            const string createSeq = @"
                CREATE TABLE IF NOT EXISTS CLIENTE_SEQ (
                    Id BIGINT NOT NULL AUTO_INCREMENT,
                    PRIMARY KEY (Id)
                ) ENGINE=InnoDB;";
            using (var cmd = new MySqlCommand(createSeq, conn))
                await cmd.ExecuteNonQueryAsync(ct);
        }

        private static async Task EnsureClienteCodigoUniqueIndexAsync(MySqlConnection conn, CancellationToken ct)
        {
            // Verifica se já existe índice único
            const string check = @"
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.STATISTICS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'CLIENTES'
                  AND INDEX_NAME = 'UX_CLIENTES_CLICODIGO';";
            using var cmdCheck = new MySqlCommand(check, conn);
            var exists = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync(ct)) > 0;
            if (exists) return;

            // Tenta criar índice único
            try
            {
                const string createIdx = "ALTER TABLE CLIENTES ADD UNIQUE INDEX UX_CLIENTES_CLICODIGO (CLICODIGO);";
                using var cmdCreate = new MySqlCommand(createIdx, conn);
                await cmdCreate.ExecuteNonQueryAsync(ct);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[Indice Unico CLICODIGO] Falhou: " + ex.Message);
                // Provavelmente duplicados pré-existentes: administrador deve corrigir
            }
        }

        /// <summary>
        /// Gera próximo código único (thread-safe via AUTO_INCREMENT).
        /// </summary>
        public static async Task<string> GetNextClienteCodigoAsync(CancellationToken ct = default)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync(ct);

            await EnsureClienteSeqTableAsync(conn, ct);
            await EnsureClienteCodigoUniqueIndexAsync(conn, ct);

            long id;
            // Inserção atómica gera novo Id
            using (var ins = new MySqlCommand("INSERT INTO CLIENTE_SEQ () VALUES ();", conn))
                await ins.ExecuteNonQueryAsync(ct);
            using (var last = new MySqlCommand("SELECT LAST_INSERT_ID();", conn))
                id = Convert.ToInt64(await last.ExecuteScalarAsync(ct));

            // Formatação: até 999999 com padding, depois expande sem travar
            string codigo = id <= 999999
                ? "CL" + id.ToString("D6")
                : "CL" + id.ToString(); // Permite crescimento sem quebrar

            return codigo;
        }

        // Peek (mostra próximo sem consumir – apenas para pré-visualização)
        public static async Task<string> PeekNextClienteCodigoAsync(CancellationToken ct = default)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync(ct);

            await EnsureClienteSeqTableAsync(conn, ct);

            const string sql = @"
                SELECT AUTO_INCREMENT
                FROM information_schema.TABLES
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'CLIENTE_SEQ'
                LIMIT 1;";
            using var cmd = new MySqlCommand(sql, conn);
            var val = await cmd.ExecuteScalarAsync(ct);
            long next = (val == null || val == DBNull.Value) ? 1 : Convert.ToInt64(val);
            return next <= 999999 ? "CL" + next.ToString("D6") : "CL" + next.ToString();
        }

        public static async Task<List<Cliente>> GetClientesAsync(string? filtro = null, CancellationToken ct = default)
        {
            var list = new List<Cliente>();
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync(ct);

            string sql = @"
                SELECT CLINOME, CLICODIGO, TELEFONE, INDICATIVO, EMAIL, EXTERNO, ANULADO,
                       VENDEDOR, VALORCREDITO, PastasSincronizadas,
                       (SELECT COALESCE(SUM(TotalAmount), 0) FROM OrderInfo o WHERE o.CustomerNo COLLATE utf8mb4_unicode_ci = c.CLICODIGO COLLATE utf8mb4_unicode_ci AND YEAR(o.OrderDate) = YEAR(CURDATE())) as VendasAnoAtual,
                       (SELECT COALESCE(SUM(TotalAmount), 0) FROM OrderInfo o WHERE o.CustomerNo COLLATE utf8mb4_unicode_ci = c.CLICODIGO COLLATE utf8mb4_unicode_ci AND YEAR(o.OrderDate) = YEAR(CURDATE()) - 1) as VendasAnoAnterior
                FROM CLIENTES c";
            if (!string.IsNullOrWhiteSpace(filtro))
                sql += " WHERE (LOWER(CLINOME) LIKE @f OR LOWER(CLICODIGO) LIKE @f OR LOWER(EMAIL) LIKE @f)";
            sql += " ORDER BY CLINOME;";

            using (var cmd = new MySqlCommand(sql, conn))
            {
                if (!string.IsNullOrWhiteSpace(filtro))
                    cmd.Parameters.Add("@f", MySqlDbType.VarChar, 120).Value = "%" + filtro.ToLowerInvariant() + "%";

                using var rd = await cmd.ExecuteReaderAsync(ct);
                while (await rd.ReadAsync(ct))
                {
                    bool GetBool(string col)
                    {
                        if (rd.IsDBNull(rd.GetOrdinal(col))) return false;
                        object val = rd[col];
                        return val switch
                        {
                            bool b => b,
                            sbyte sb => sb != 0,
                            byte bt => bt != 0,
                            short sh => sh != 0,
                            ushort us => us != 0,
                            int i => i != 0,
                            uint ui => ui != 0,
                            long l => l != 0,
                            ulong ul => ul != 0,
                            string str => str == "1" || str.Equals("true", StringComparison.OrdinalIgnoreCase),
                            _ => Convert.ToBoolean(val) // Fallback genérico
                        };
                    }

                    list.Add(new Cliente
                    {
                        CLINOME = rd.IsDBNull(rd.GetOrdinal("CLINOME")) ? null : rd.GetString("CLINOME"),
                        CLICODIGO = rd.IsDBNull(rd.GetOrdinal("CLICODIGO")) ? null : rd.GetString("CLICODIGO"),
                        TELEFONE = rd.IsDBNull(rd.GetOrdinal("TELEFONE")) ? null : rd.GetString("TELEFONE"),
                        INDICATIVO = rd.IsDBNull(rd.GetOrdinal("INDICATIVO")) ? null : rd.GetString("INDICATIVO"),
                        EMAIL = rd.IsDBNull(rd.GetOrdinal("EMAIL")) ? null : rd.GetString("EMAIL"),
                        EXTERNO = GetBool("EXTERNO"),
                        ANULADO = GetBool("ANULADO"),
                        VENDEDOR = rd.IsDBNull(rd.GetOrdinal("VENDEDOR")) ? null : rd.GetString("VENDEDOR"),
                        VALORCREDITO = rd.IsDBNull(rd.GetOrdinal("VALORCREDITO")) ? null : rd.GetString("VALORCREDITO"),
                        PastasSincronizadas = GetBool("PastasSincronizadas"),
                        VendasAnoAtual = rd.IsDBNull(rd.GetOrdinal("VendasAnoAtual")) ? 0 : rd.GetDecimal("VendasAnoAtual"),
                        VendasAnoAnterior = rd.IsDBNull(rd.GetOrdinal("VendasAnoAnterior")) ? 0 : rd.GetDecimal("VendasAnoAnterior")
                    });
                }
            }

            // Fetch service counts if OrderInfo exists
            if (await TableExistsAsync(conn, "OrderInfo", ct))
            {
                try
                {
                    const string countSql = "SELECT CustomerNo, COUNT(*) as Cnt FROM OrderInfo GROUP BY CustomerNo";
                    using var cmdCount = new MySqlCommand(countSql, conn);
                    using var rdCount = await cmdCount.ExecuteReaderAsync(ct);
                    var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    while (await rdCount.ReadAsync(ct))
                    {
                        var code = rdCount.IsDBNull(0) ? null : rdCount.GetString(0);
                        var cnt = rdCount.GetInt32(1);
                        if (!string.IsNullOrWhiteSpace(code))
                            counts[code] = cnt;
                    }

                    foreach (var c in list)
                    {
                        if (!string.IsNullOrWhiteSpace(c.CLICODIGO) && counts.TryGetValue(c.CLICODIGO, out var count))
                        {
                            c.ServicesCount = count;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error fetching service counts: {ex.Message}");
                }
            }

            return list;
        }

        public static async Task<bool> UpsertClienteAsync(Cliente c, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(c.CLICODIGO))
                throw new ArgumentException("CLICODIGO obrigatório.");

            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync(ct);

            const string existsSql = "SELECT 1 FROM CLIENTES WHERE CLICODIGO = @cod LIMIT 1;";
            bool exists;
            using (var check = new MySqlCommand(existsSql, conn))
            {
                check.Parameters.Add("@cod", MySqlDbType.VarChar, 30).Value = c.CLICODIGO;
                exists = (await check.ExecuteScalarAsync(ct)) != null;
            }

            string sql = exists
                ? @"UPDATE CLIENTES SET
                        CLINOME=@nome, TELEFONE=@tel, INDICATIVO=@ind, EMAIL=@mail,
                        EXTERNO=@ext, ANULADO=@anu, VENDEDOR=@vend,
                        VALORCREDITO=@cred, PastasSincronizadas=@past
                    WHERE CLICODIGO=@cod LIMIT 1;"
                : @"INSERT INTO CLIENTES
                    (CLICODIGO, CLINOME, TELEFONE, INDICATIVO, EMAIL, EXTERNO, ANULADO, VENDEDOR, VALORCREDITO, PastasSincronizadas)
                    VALUES (@cod,@nome,@tel,@ind,@mail,@ext,@anu,@vend,@cred,@past);";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.Add("@cod", MySqlDbType.VarChar, 30).Value = c.CLICODIGO ?? "";
            cmd.Parameters.Add("@nome", MySqlDbType.VarChar, 150).Value = c.CLINOME ?? "";
            cmd.Parameters.Add("@tel", MySqlDbType.VarChar, 40).Value = c.TELEFONE ?? "";
            cmd.Parameters.Add("@ind", MySqlDbType.VarChar, 10).Value = c.INDICATIVO ?? "";
            cmd.Parameters.Add("@mail", MySqlDbType.VarChar, 150).Value = c.EMAIL ?? "";
            cmd.Parameters.Add("@ext", MySqlDbType.Bit).Value = c.EXTERNO ? 1 : 0;
            cmd.Parameters.Add("@anu", MySqlDbType.Bit).Value = c.ANULADO ? 1 : 0;
            cmd.Parameters.Add("@vend", MySqlDbType.VarChar, 80).Value = c.VENDEDOR ?? "";
            cmd.Parameters.Add("@cred", MySqlDbType.VarChar, 40).Value = c.VALORCREDITO ?? "";
            cmd.Parameters.Add("@past", MySqlDbType.Bit).Value = c.PastasSincronizadas ? 1 : 0;
            var rows = await cmd.ExecuteNonQueryAsync(ct);
            return rows > 0;
        }

        private static async Task EnsureSetupSchemaAsync()
        {
            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();

                // Check ServerUser
                var checkSql = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'SETUP' AND COLUMN_NAME = 'ServerUser';";
                using (var cmd = new MySqlCommand(checkSql, conn))
                {
                    cmd.Parameters.AddWithValue("@db", conn.Database);
                    var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                    if (count == 0)
                    {
                        var alter = "ALTER TABLE SETUP ADD COLUMN ServerUser VARCHAR(100) NULL DEFAULT NULL;";
                        using var alterCmd = new MySqlCommand(alter, conn);
                        await alterCmd.ExecuteNonQueryAsync();
                    }
                }

                // Check ServerPassword
                checkSql = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'SETUP' AND COLUMN_NAME = 'ServerPassword';";
                using (var cmd = new MySqlCommand(checkSql, conn))
                {
                    cmd.Parameters.AddWithValue("@db", conn.Database);
                    var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                    if (count == 0)
                    {
                        var alter = "ALTER TABLE SETUP ADD COLUMN ServerPassword VARCHAR(100) NULL DEFAULT NULL;";
                        using var alterCmd = new MySqlCommand(alter, conn);
                        await alterCmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SchemaUpdate] Erro: {ex.Message}");
            }
        }

        public static async Task<Setup?> GetSetupAsync(CancellationToken ct = default)
        {
            // Ensure schema exists before querying
            await EnsureSetupSchemaAsync();

            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync(ct);

            const string sql = @"
                SELECT CodEmp, Empresa, CaminhoServidor, CaminhoServidor2,
                       SERV1PASTA1, SERV1PASTA2, SERV1PASTA3, SERV1PASTA4,
                       SERV1PASTA5, SERV1PASTA6, SERV1PASTA7, SERV1PASTA8,
                       SERV2PASTA1, SERV2PASTA2,
                       ServerUser, ServerPassword
                FROM SETUP
                LIMIT 1;";

            using var cmd = new MySqlCommand(sql, conn);
            using var rd = await cmd.ExecuteReaderAsync(ct);
            if (!await rd.ReadAsync(ct)) return null;

            // Helper to safely get string even if column doesn't exist (though SQL would fail before)
            // or if it is null.
            string? GetStr(string col) 
            {
                try { return rd.IsDBNull(rd.GetOrdinal(col)) ? null : rd.GetString(col); }
                catch { return null; }
            }

            return new Setup
            {
                CodEmp = rd.GetString("CodEmp"),
                Empresa = GetStr("Empresa"),
                CaminhoServidor = GetStr("CaminhoServidor"),
                CaminhoServidor2 = GetStr("CaminhoServidor2"),
                ServerUser = GetStr("ServerUser"),
                ServerPassword = GetStr("ServerPassword"),
                SERV1PASTA1 = GetStr("SERV1PASTA1"),
                SERV1PASTA2 = GetStr("SERV1PASTA2"),
                SERV1PASTA3 = GetStr("SERV1PASTA3"),
                SERV1PASTA4 = GetStr("SERV1PASTA4"),
                SERV1PASTA5 = GetStr("SERV1PASTA5"),
                SERV1PASTA6 = GetStr("SERV1PASTA6"),
                SERV1PASTA7 = GetStr("SERV1PASTA7"),
                SERV1PASTA8 = GetStr("SERV1PASTA8"),
                SERV2PASTA1 = GetStr("SERV2PASTA1"),
                SERV2PASTA2 = GetStr("SERV2PASTA2")
            };
        }

        public static async Task<bool> UpdateSetupAsync(Setup setup)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            const string sql = @"
                UPDATE SETUP SET
                    CaminhoServidor = @CaminhoServidor,
                    ServerUser = @ServerUser,
                    ServerPassword = @ServerPassword,
                    SERV1PASTA1 = @SERV1PASTA1,
                    SERV1PASTA2 = @SERV1PASTA2,
                    SERV1PASTA3 = @SERV1PASTA3,
                    SERV1PASTA4 = @SERV1PASTA4,
                    SERV1PASTA5 = @SERV1PASTA5,
                    SERV1PASTA6 = @SERV1PASTA6,
                    SERV1PASTA7 = @SERV1PASTA7,
                    SERV1PASTA8 = @SERV1PASTA8
                WHERE CodEmp = @CodEmp
                LIMIT 1;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@CodEmp", setup.CodEmp);
            cmd.Parameters.AddWithValue("@CaminhoServidor", setup.CaminhoServidor);
            cmd.Parameters.AddWithValue("@ServerUser", setup.ServerUser);
            cmd.Parameters.AddWithValue("@ServerPassword", setup.ServerPassword);
            cmd.Parameters.AddWithValue("@SERV1PASTA1", setup.SERV1PASTA1);
            cmd.Parameters.AddWithValue("@SERV1PASTA2", setup.SERV1PASTA2);
            cmd.Parameters.AddWithValue("@SERV1PASTA3", setup.SERV1PASTA3);
            cmd.Parameters.AddWithValue("@SERV1PASTA4", setup.SERV1PASTA4);
            cmd.Parameters.AddWithValue("@SERV1PASTA5", setup.SERV1PASTA5);
            cmd.Parameters.AddWithValue("@SERV1PASTA6", setup.SERV1PASTA6);
            cmd.Parameters.AddWithValue("@SERV1PASTA7", setup.SERV1PASTA7);
            cmd.Parameters.AddWithValue("@SERV1PASTA8", setup.SERV1PASTA8);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public static async Task<(bool Created, string Message)> EnsureClientePastasAsync(Cliente c)
        {
            var (success, msg) = await FolderService.CreateClientFoldersAsync(c);
            if (success)
            {
                try
                {
                    using var conn = new MySqlConnection(GetConnectionString());
                    await conn.OpenAsync();
                    const string sql = "UPDATE CLIENTES SET PastasSincronizadas=1 WHERE CLICODIGO=@cod LIMIT 1;";
                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.Add("@cod", MySqlDbType.VarChar, 30).Value = c.CLICODIGO;
                    await cmd.ExecuteNonQueryAsync();
                    
                    c.PastasSincronizadas = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DB] Erro ao atualizar flag pastas: {ex.Message}");
                }
            }
            return (success, msg);
        }



        public static async Task<bool> DeleteClienteAsync(string? codigo, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return false;

            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync(ct);

            // Verificar se tabela orderinfo existe antes de fazer query
            if (await TableExistsAsync(conn, "orderinfo", ct))
            {
                const string depSql = "SELECT COUNT(*) FROM orderinfo WHERE CustomerNO=@cod;";
                using (var dep = new MySqlCommand(depSql, conn))
                {
                    dep.Parameters.Add("@cod", MySqlDbType.VarChar, 30).Value = codigo;
                    var count = Convert.ToInt32(await dep.ExecuteScalarAsync(ct));
                    if (count > 0)
                        throw new InvalidOperationException("Impossível eliminar. Existem serviços associados.");
                }
            }

            const string del = "DELETE FROM CLIENTES WHERE CLICODIGO=@cod LIMIT 1;";
            using var cmd = new MySqlCommand(del, conn);
            cmd.Parameters.Add("@cod", MySqlDbType.VarChar, 30).Value = codigo;
            var rows = await cmd.ExecuteNonQueryAsync(ct);
            return rows > 0;
        }

        private static async Task<bool> TableExistsAsync(MySqlConnection conn, string tableName, CancellationToken ct)
        {
            const string sql = @"SELECT 1
                                   FROM information_schema.TABLES
                                  WHERE TABLE_SCHEMA = DATABASE()
                                    AND TABLE_NAME = @table
                                  LIMIT 1;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.Add("@table", MySqlDbType.VarChar, 128).Value = tableName;
            using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ReadAsync(ct);
        }

        public static async Task<List<string>> GetVendedoresAsync(CancellationToken ct = default)
        {
            var list = new List<string>();
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync(ct);

            const string sql = @"
                SELECT RTRIM(Name) AS Name
                FROM REGISTRATION
                WHERE RTRIM(TipoUtilizador)='VENDEDOR'
                ORDER BY Name;";

            using var cmd = new MySqlCommand(sql, conn);
            using var rd = await cmd.ExecuteReaderAsync(ct);
            while (await rd.ReadAsync(ct))
                list.Add(rd.GetString("Name"));
            return list;
        }

        // ================= PRODUTOS =================
        private const string ProdutosTable = "PRODUTOS"; // Ajuste se o nome real for diferente.

        private static async Task EnsureProductSeqTableAsync(MySqlConnection conn, CancellationToken ct)
        {
            const string createSeq = @"
                CREATE TABLE IF NOT EXISTS PRODUTO_SEQ (
                    Id BIGINT NOT NULL AUTO_INCREMENT,
                    PRIMARY KEY (Id)
                ) ENGINE=InnoDB;";
            using (var cmd = new MySqlCommand(createSeq, conn))
                await cmd.ExecuteNonQueryAsync(ct);

            // Se a tabela estiver vazia (acabou de ser criada ou nunca usada), tentar inicializar com o MAX atual
            // Para evitar reiniciar do 1 se já existirem produtos.
            // Verificamos o AUTO_INCREMENT atual? Ou apenas se tem registos?
            // O mais seguro é: se count=0, fazemos ALTER TABLE AUTO_INCREMENT = MAX + 1
            
            // Nota: Em MySQL, verificar o valor atual do auto_increment é via information_schema, 
            // mas para simplificar, vamos assumir que se não houver registos na tabela SEQ, precisamos sincronizar.
            // Mas a tabela SEQ vai enchendo. Se estiver vazia, é porque é nova.
            
            // Verificar se tem linhas
            const string check = "SELECT 1 FROM PRODUTO_SEQ LIMIT 1;";
            using var cmdCheck = new MySqlCommand(check, conn);
            var hasRows = (await cmdCheck.ExecuteScalarAsync(ct)) != null;

            if (!hasRows)
            {
                // Buscar o maior ID atual na tabela de produtos
                // PRDxxxxxx -> substring(4)
                string sqlMax = $"SELECT MAX(CAST(SUBSTRING(PRODCODIGO,4) AS UNSIGNED)) FROM {ProdutosTable} WHERE PRODCODIGO REGEXP '^PRD[0-9]+$';";
                using var cmdMax = new MySqlCommand(sqlMax, conn);
                var result = await cmdMax.ExecuteScalarAsync(ct);
                long max = (result == null || result == DBNull.Value) ? 0 : Convert.ToInt64(result);

                if (max > 0)
                {
                    // Ajustar o próximo ID
                    string sqlAlter = $"ALTER TABLE PRODUTO_SEQ AUTO_INCREMENT = {max + 1};";
                    using var cmdAlter = new MySqlCommand(sqlAlter, conn);
                    await cmdAlter.ExecuteNonQueryAsync(ct);
                }
            }
        }

        public static async Task<string> GetNextProductCodigoAsync(CancellationToken ct = default)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync(ct);

            await EnsureProductSeqTableAsync(conn, ct);

            long id;
            using (var ins = new MySqlCommand("INSERT INTO PRODUTO_SEQ () VALUES ();", conn))
                await ins.ExecuteNonQueryAsync(ct);
            using (var last = new MySqlCommand("SELECT LAST_INSERT_ID();", conn))
                id = Convert.ToInt64(await last.ExecuteScalarAsync(ct));

            return id <= 999999
                ? "PRD" + id.ToString("D6")
                : "PRD" + id.ToString();
        }

        private static async Task EnsureProductColumnsAsync(MySqlConnection conn, CancellationToken ct)
        {
            // Verifica se PRECOVENDA existe
            const string checkSql = "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tb AND COLUMN_NAME = 'PRECOVENDA';";
            using var cmd = new MySqlCommand(checkSql, conn);
            cmd.Parameters.Add("@tb", MySqlDbType.VarChar).Value = ProdutosTable;
            var count = Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
            
            if (count == 0)
            {
                const string alterSql = $"ALTER TABLE {ProdutosTable} ADD COLUMN PRECOVENDA DECIMAL(18,4) NOT NULL DEFAULT 0.0000;";
                using var alterCmd = new MySqlCommand(alterSql, conn);
                await alterCmd.ExecuteNonQueryAsync(ct);
            }
        }

        public static async Task<List<Product>> GetProductsAsync(string? filtro = null, CancellationToken ct = default)
        {
            var list = new List<Product>();
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync(ct);

            await EnsureProductColumnsAsync(conn, ct);

            string sql = $@"
                SELECT 
                    p.PRODCODIGO, 
                    p.PRODNOME, 
                    p.FAMILIA, 
                    p.COLABORADOR,
                    p.PRECOCUSTO,
                    p.PRECOVENDA,
                    (SELECT COALESCE(SUM(SUBTOTAIS), 0) FROM ORDEREDPRODUCT WHERE ProductCode = p.PRODCODIGO COLLATE utf8mb4_unicode_ci) AS TOTALVENDAS
                FROM {ProdutosTable} p";

            if (!string.IsNullOrWhiteSpace(filtro))
                sql += " WHERE (LOWER(p.PRODNOME) LIKE @f OR LOWER(p.PRODCODIGO) LIKE @f OR LOWER(p.FAMILIA) LIKE @f)";
            sql += " ORDER BY p.PRODNOME;";

            using var cmd = new MySqlCommand(sql, conn);
            if (!string.IsNullOrWhiteSpace(filtro))
                cmd.Parameters.Add("@f", MySqlDbType.VarChar, 150).Value = "%" + filtro.ToLowerInvariant() + "%";

            using var rd = await cmd.ExecuteReaderAsync(ct);
            while (await rd.ReadAsync(ct))
            {
                list.Add(new Product
                {
                    PRODCODIGO = rd.IsDBNull(rd.GetOrdinal("PRODCODIGO")) ? null : rd.GetString("PRODCODIGO"),
                    PRODNOME = rd.IsDBNull(rd.GetOrdinal("PRODNOME")) ? null : rd.GetString("PRODNOME"),
                    FAMILIA = rd.IsDBNull(rd.GetOrdinal("FAMILIA")) ? null : rd.GetString("FAMILIA"),
                    COLABORADOR = rd.IsDBNull(rd.GetOrdinal("COLABORADOR")) ? null : rd.GetString("COLABORADOR"),
                    PRECOCUSTO = rd.IsDBNull(rd.GetOrdinal("PRECOCUSTO")) ? 0m : rd.GetDecimal("PRECOCUSTO"),
                    PRECOVENDA = rd.IsDBNull(rd.GetOrdinal("PRECOVENDA")) ? 0m : rd.GetDecimal("PRECOVENDA"),
                    TOTALVENDAS = rd.IsDBNull(rd.GetOrdinal("TOTALVENDAS")) ? 0m : rd.GetDecimal("TOTALVENDAS")
                });
            }
            return list;
        }

        public static async Task<List<string>> GetProductFamiliesAsync(CancellationToken ct = default)
        {
            var families = new List<string>();
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync(ct);

            string sql = $"SELECT DISTINCT FAMILIA FROM {ProdutosTable} WHERE FAMILIA IS NOT NULL AND FAMILIA <> '' ORDER BY FAMILIA;";
            using var cmd = new MySqlCommand(sql, conn);
            using var rd = await cmd.ExecuteReaderAsync(ct);
            while (await rd.ReadAsync(ct))
                families.Add(rd.GetString("FAMILIA"));
            return families;
        }

        public static async Task<bool> UpsertProductAsync(Product p, CancellationToken ct = default)
        {
            if (p is null) throw new ArgumentNullException(nameof(p));
            if (string.IsNullOrWhiteSpace(p.PRODCODIGO))
                throw new ArgumentException("PRODCODIGO obrigatório.");

            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync(ct);

            await EnsureProductColumnsAsync(conn, ct);

            string existsSql = $"SELECT 1 FROM {ProdutosTable} WHERE PRODCODIGO=@cod LIMIT 1;";
            bool exists;
            using (var check = new MySqlCommand(existsSql, conn))
            {
                check.Parameters.Add("@cod", MySqlDbType.VarChar, 30).Value = p.PRODCODIGO;
                exists = (await check.ExecuteScalarAsync(ct)) != null;
            }

            string sql = exists
                ? $@"UPDATE {ProdutosTable} SET
                        PRODNOME=@nome,
                        FAMILIA=@fam,
                        COLABORADOR=@col,
                        PRECOCUSTO=@pcusto,
                        PRECOVENDA=@pvenda
                    WHERE PRODCODIGO=@cod LIMIT 1;"
                : $@"INSERT INTO {ProdutosTable}
                        (PRODCODIGO, PRODNOME, FAMILIA, COLABORADOR, PRECOCUSTO, PRECOVENDA)
                    VALUES
                        (@cod,@nome,@fam,@col,@pcusto,@pvenda);";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.Add("@cod", MySqlDbType.VarChar, 30).Value = p.PRODCODIGO ?? "";
            cmd.Parameters.Add("@nome", MySqlDbType.VarChar, 150).Value = p.PRODNOME ?? "";
            cmd.Parameters.Add("@fam", MySqlDbType.VarChar, 80).Value = p.FAMILIA ?? "";
            cmd.Parameters.Add("@col", MySqlDbType.VarChar, 80).Value = p.COLABORADOR ?? "";
            cmd.Parameters.Add("@pcusto", MySqlDbType.Decimal).Value = p.PRECOCUSTO;
            cmd.Parameters.Add("@pvenda", MySqlDbType.Decimal).Value = p.PRECOVENDA;
            var rows = await cmd.ExecuteNonQueryAsync(ct);
            return rows > 0;
        }

        public static async Task<bool> DeleteProductAsync(string? codigo, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return false;

            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync(ct);

            string del = $"DELETE FROM {ProdutosTable} WHERE PRODCODIGO=@cod LIMIT 1;";
            using var cmd = new MySqlCommand(del, conn);
            cmd.Parameters.Add("@cod", MySqlDbType.VarChar, 30).Value = codigo;
            var rows = await cmd.ExecuteNonQueryAsync(ct);
            return rows > 0;
        }

        // ==== ARMAZENAMENTO / PASTAS (Clientes) ====
        private sealed class StorageSetup
        {
            public string? CaminhoServidor { get; init; }
            public string? CaminhoServidor2 { get; init; }
            public string?[] SubPastas { get; init; } = Array.Empty<string?>();
        }

        private static async Task<StorageSetup?> GetStorageSetupAsync(MySqlConnection conn, CancellationToken ct)
        {
            const string sql = @"SELECT CaminhoServidor, CaminhoServidor2,
                SERV1PASTA1,SERV1PASTA2,SERV1PASTA3,SERV1PASTA4,
                SERV1PASTA5,SERV1PASTA6,SERV1PASTA7,SERV1PASTA8
                FROM SETUP LIMIT 1;";
            using var cmd = new MySqlCommand(sql, conn);
            using var rd = await cmd.ExecuteReaderAsync(ct);
            if (!await rd.ReadAsync(ct)) return null;

            string? Get(int i) => rd.IsDBNull(i) ? null : rd.GetString(i);

            return new StorageSetup
            {
                CaminhoServidor = Get(rd.GetOrdinal("CaminhoServidor")),
                CaminhoServidor2 = Get(rd.GetOrdinal("CaminhoServidor2")),
                SubPastas = new[]
                {
                    Get(rd.GetOrdinal("SERV1PASTA1")),
                    Get(rd.GetOrdinal("SERV1PASTA2")),
                    Get(rd.GetOrdinal("SERV1PASTA3")),
                    Get(rd.GetOrdinal("SERV1PASTA4")),
                    Get(rd.GetOrdinal("SERV1PASTA5")),
                    Get(rd.GetOrdinal("SERV1PASTA6")),
                    Get(rd.GetOrdinal("SERV1PASTA7")),
                    Get(rd.GetOrdinal("SERV1PASTA8"))
                }
            };
        }

        private static async Task UpdatePastasFlagAsync(MySqlConnection conn, string codigo, bool flag, CancellationToken ct)
        {
            const string sql = "UPDATE CLIENTES SET PastasSincronizadas=@p WHERE CLICODIGO=@c LIMIT 1;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.Add("@p", MySqlDbType.Bit).Value = flag ? 1 : 0;
            cmd.Parameters.Add("@c", MySqlDbType.VarChar, 30).Value = codigo;
            await cmd.ExecuteNonQueryAsync(ct);
        }

        public static async Task<(bool Ok, string? BasePath)> EnsureClientePastasAsync(Cliente c, CancellationToken ct = default)
        {
            if (c is null) throw new ArgumentNullException(nameof(c));
            if (string.IsNullOrWhiteSpace(c.CLICODIGO))
                throw new ArgumentException("Cliente sem código.");
            if (string.IsNullOrWhiteSpace(c.CLINOME))
                throw new ArgumentException("Cliente sem nome.");

            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync(ct);

            var setup = await GetStorageSetupAsync(conn, ct);
            if (setup == null)
                return (false, null);

            string? basePath = !string.IsNullOrWhiteSpace(setup.CaminhoServidor)
                ? setup.CaminhoServidor
                : setup.CaminhoServidor2;

            if (string.IsNullOrWhiteSpace(basePath))
                return (false, null);

#if ANDROID || IOS
            return (false, null);
#endif
            try
            {
                var invalid = Path.GetInvalidFileNameChars();
                var rawName = c.CLINOME?.Trim() ?? "";
                var safeName = new string(rawName.Where(ch => !invalid.Contains(ch)).ToArray()).Trim();
                if (string.IsNullOrEmpty(safeName))
                    return (false, null);

                if (!Directory.Exists(basePath))
                    return (false, basePath);

                var clienteRoot = Path.Combine(basePath, safeName);
                if (!Directory.Exists(clienteRoot))
                    Directory.CreateDirectory(clienteRoot);

                foreach (var sub in setup.SubPastas)
                {
                    if (string.IsNullOrWhiteSpace(sub)) continue;
                    var cleanSub = new string(sub.Where(ch => !invalid.Contains(ch)).ToArray()).Trim();
                    if (string.IsNullOrEmpty(cleanSub)) continue;
                    var subPath = Path.Combine(clienteRoot, cleanSub);
                    if (!Directory.Exists(subPath))
                        Directory.CreateDirectory(subPath);
                }

                await UpdatePastasFlagAsync(conn, c.CLICODIGO!, true, ct);
                return (true, clienteRoot);
            }
            catch
            {
                try { await UpdatePastasFlagAsync(conn, c.CLICODIGO!, false, ct); } catch { }
                return (false, basePath);
            }
        }

        // ===== RESET PASSWORD (EXPOSTO – usado em LoginPage) =====
        public static async Task<bool> ResetPasswordAsync(string username, string newPassword)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            if (!await UserExistsAsync(username))
                return false;

            var hash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 10);
            const string up = "UPDATE REGISTRATION SET Password = @p WHERE Username = @u LIMIT 1;";
            using var cmd = new MySqlCommand(up, conn);
            cmd.Parameters.Add("@p", MySqlDbType.VarChar, 100).Value = hash;
            cmd.Parameters.Add("@u", MySqlDbType.VarChar, 30).Value = username;
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        // ===== PRODUTOFAMILIAS (simples) =====
        public static async Task<bool> UpsertProductFamilyAsync(string codigo, string descricao, CancellationToken ct = default)
        {
            // Exemplo de implementação simples (ajuste conforme seu banco de dados)
            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync(ct);

                // Tenta inserir ou atualizar a família de produto
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO PRODUTOFAMILIAS (CODIGO, DESCRICAO)
                    VALUES (@codigo, @descricao)
                    ON DUPLICATE KEY UPDATE DESCRICAO = @descricao;";
                cmd.Parameters.AddWithValue("@codigo", codigo);
                cmd.Parameters.AddWithValue("@descricao", descricao);

                var result = await cmd.ExecuteNonQueryAsync(ct);
                return result > 0;
            }
            catch
            {
                return false;
            }
        }

        // Carregar T O D A S as colunas, mas com LIMIT para manter rápido
        public static async Task<List<OrderInfoModel>> GetOrdersLightAsync(string? filtro = null, CancellationToken ct = default)
        {
            var list = new List<OrderInfoModel>();
            using var conn = new MySqlConnector.MySqlConnection(GetConnectionString());
            await conn.OpenAsync(ct);

            var sql = @"
SELECT OrderNo, OrderDate, OrderStatus, CustomerNo, CustomerName,
       SubTotal, TaxPercentage, TaxAmount, TotalAmount, OrderDateEnt,
       Desconto, DescPercentage, observacoes, utilizador, ANO, ControlAbert,
       CONTROLVEND, DESCRICAOCC, VALORPENDENTE, numfatura, numrecibo,
       VALORPAGO, TotalAmountCusto, MargemLucro, MargemPercentual, Numserv,
       numext1, numext2, Encomenda, Servencomenda, EMAILENC, EMAILPROD,
       EMAILPRODEXT, EMAILFINAL, EMAILLIQUID, PEQDESCSERVICO, DESCPROD, TEMPO
FROM OrderInfo";

            if (!string.IsNullOrWhiteSpace(filtro))
                sql += " WHERE (LOWER(OrderNo) LIKE @f OR LOWER(CustomerName) LIKE @f OR LOWER(OrderStatus) LIKE @f)";

            sql += " ORDER BY OrderDate DESC LIMIT 200;";

            using var cmd = new MySqlConnector.MySqlCommand(sql, conn);
            if (!string.IsNullOrWhiteSpace(filtro))
                cmd.Parameters.Add("@f", MySqlConnector.MySqlDbType.VarChar, 200).Value = "%" + filtro.Trim().ToLowerInvariant() + "%";

            using var rd = await cmd.ExecuteReaderAsync(ct);

            // Helpers locais para reduzir verbosidade
            static string S(MySqlConnector.MySqlDataReader r, string col) => r.IsDBNull(r.GetOrdinal(col)) ? "" : r.GetString(col);
            static string? Ns(MySqlConnector.MySqlDataReader r, string col) => r.IsDBNull(r.GetOrdinal(col)) ? null : r.GetString(col);
            static DateTime? Dt(MySqlConnector.MySqlDataReader r, string col) => r.IsDBNull(r.GetOrdinal(col)) ? (DateTime?)null : r.GetDateTime(col);
            static decimal? Dc(MySqlConnector.MySqlDataReader r, string col) => r.IsDBNull(r.GetOrdinal(col)) ? (decimal?)null : r.GetDecimal(col);
            static bool? Bn(MySqlConnector.MySqlDataReader r, string col)
            {
                if (r.IsDBNull(r.GetOrdinal(col))) return null;
                var v = r[col];
                return v switch
                {
                    bool b => b,
                    sbyte sb => sb != 0,
                    byte bt => bt != 0,
                    short sh => sh != 0,
                    int i => i != 0,
                    long l => l != 0,
                    string s => s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase),
                    _ => (bool?)null
                };
            }

            while (await rd.ReadAsync(ct))
            {
                list.Add(new OrderInfoModel
                {
                    OrderNo = S(rd, "OrderNo"),
                    OrderDate = Dt(rd, "OrderDate"),
                    OrderStatus = Ns(rd, "OrderStatus"),
                    CustomerNo = S(rd, "CustomerNo"),
                    CustomerName = S(rd, "CustomerName"),
                    SubTotal = Dc(rd, "SubTotal"),
                    TaxPercentage = Dc(rd, "TaxPercentage"),
                    TaxAmount = Dc(rd, "TaxAmount"),
                    TotalAmount = Dc(rd, "TotalAmount"),
                    OrderDateEnt = Dt(rd, "OrderDateEnt"),
                    Desconto = Dc(rd, "Desconto"),
                    DescPercentage = Dc(rd, "DescPercentage"),
                    Observacoes = Ns(rd, "observacoes"),
                    Utilizador = Ns(rd, "utilizador"),
                    ANO = Ns(rd, "ANO"),
                    ControlAbert = Bn(rd, "ControlAbert") ?? false,
                    CONTROLVEND = Ns(rd, "CONTROLVEND"),
                    DESCRICAOCC = Ns(rd, "DESCRICAOCC"),
                    VALORPENDENTE = Dc(rd, "VALORPENDENTE"),
                    Numfatura = Ns(rd, "numfatura"),
                    Numrecibo = Ns(rd, "numrecibo"),
                    VALORPAGO = Dc(rd, "VALORPAGO"),
                    TotalAmountCusto = Dc(rd, "TotalAmountCusto"),
                    MargemLucro = Dc(rd, "MargemLucro"),
                    MargemPercentual = Dc(rd, "MargemPercentual"),
                    Numserv = Ns(rd, "Numserv"),
                    Numext1 = Ns(rd, "numext1"),
                    Numext2 = Ns(rd, "numext2"),
                    Encomenda = Bn(rd, "Encomenda"),
                    Servencomenda = Ns(rd, "Servencomenda"),
                    EMAILENC = Bn(rd, "EMAILENC"),
                    EMAILPROD = Bn(rd, "EMAILPROD"),
                    EMAILPRODEXT = Bn(rd, "EMAILPRODEXT"),
                    EMAILFINAL = Bn(rd, "EMAILFINAL"),
                    EMAILLIQUID = Bn(rd, "EMAILLIQUID"),
                    PEQDESCSERVICO = Ns(rd, "PEQDESCSERVICO"),
                    DESCPROD = Ns(rd, "DESCPROD"),
                    TEMPO = Ns(rd, "TEMPO"),
                });
            }

            return list;
        }

        public static async Task<List<OrderInfoModel>> GetOrdersByClientAsync(string clientCode, CancellationToken ct = default)
        {
            var list = new List<OrderInfoModel>();
            using var conn = new MySqlConnector.MySqlConnection(GetConnectionString());
            await conn.OpenAsync(ct);

            const string sql = @"
SELECT OrderNo, OrderDate, OrderStatus, CustomerNo, CustomerName,
       SubTotal, TaxPercentage, TaxAmount, TotalAmount, OrderDateEnt,
       Desconto, DescPercentage, observacoes, utilizador, ANO, ControlAbert,
       CONTROLVEND, DESCRICAOCC, VALORPENDENTE, numfatura, numrecibo,
       VALORPAGO, TotalAmountCusto, MargemLucro, MargemPercentual, Numserv,
       numext1, numext2, Encomenda, Servencomenda, EMAILENC, EMAILPROD,
       EMAILPRODEXT, EMAILFINAL, EMAILLIQUID, PEQDESCSERVICO, DESCPROD, TEMPO
FROM OrderInfo
WHERE CustomerNo = @c
ORDER BY OrderDate DESC";

            using var cmd = new MySqlConnector.MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@c", clientCode ?? "");

            using var rd = await cmd.ExecuteReaderAsync(ct);

            // Helpers locais para reduzir verbosidade
            static string S(MySqlConnector.MySqlDataReader r, string col) => r.IsDBNull(r.GetOrdinal(col)) ? "" : r.GetString(col);
            static string? Ns(MySqlConnector.MySqlDataReader r, string col) => r.IsDBNull(r.GetOrdinal(col)) ? null : r.GetString(col);
            static DateTime? Dt(MySqlConnector.MySqlDataReader r, string col) => r.IsDBNull(r.GetOrdinal(col)) ? (DateTime?)null : r.GetDateTime(col);
            static decimal? Dc(MySqlConnector.MySqlDataReader r, string col) => r.IsDBNull(r.GetOrdinal(col)) ? (decimal?)null : r.GetDecimal(col);
            static bool? Bn(MySqlConnector.MySqlDataReader r, string col)
            {
                if (r.IsDBNull(r.GetOrdinal(col))) return null;
                var v = r[col];
                return v switch
                {
                    bool b => b,
                    sbyte sb => sb != 0,
                    byte bt => bt != 0,
                    short sh => sh != 0,
                    int i => i != 0,
                    long l => l != 0,
                    string s => s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase),
                    _ => (bool?)null
                };
            }

            while (await rd.ReadAsync(ct))
            {
                list.Add(new OrderInfoModel
                {
                    OrderNo = S(rd, "OrderNo"),
                    OrderDate = Dt(rd, "OrderDate"),
                    OrderStatus = Ns(rd, "OrderStatus"),
                    CustomerNo = S(rd, "CustomerNo"),
                    CustomerName = S(rd, "CustomerName"),
                    SubTotal = Dc(rd, "SubTotal"),
                    TaxPercentage = Dc(rd, "TaxPercentage"),
                    TaxAmount = Dc(rd, "TaxAmount"),
                    TotalAmount = Dc(rd, "TotalAmount"),
                    OrderDateEnt = Dt(rd, "OrderDateEnt"),
                    Desconto = Dc(rd, "Desconto"),
                    DescPercentage = Dc(rd, "DescPercentage"),
                    Observacoes = Ns(rd, "observacoes"),
                    Utilizador = Ns(rd, "utilizador"),
                    ANO = Ns(rd, "ANO"),
                    ControlAbert = Bn(rd, "ControlAbert") ?? false,
                    CONTROLVEND = Ns(rd, "CONTROLVEND"),
                    DESCRICAOCC = Ns(rd, "DESCRICAOCC"),
                    VALORPENDENTE = Dc(rd, "VALORPENDENTE"),
                    Numfatura = Ns(rd, "numfatura"),
                    Numrecibo = Ns(rd, "numrecibo"),
                    VALORPAGO = Dc(rd, "VALORPAGO"),
                    TotalAmountCusto = Dc(rd, "TotalAmountCusto"),
                    MargemLucro = Dc(rd, "MargemLucro"),
                    MargemPercentual = Dc(rd, "MargemPercentual"),
                    Numserv = Ns(rd, "Numserv"),
                    Numext1 = Ns(rd, "numext1"),
                    Numext2 = Ns(rd, "numext2"),
                    Encomenda = Bn(rd, "Encomenda"),
                    Servencomenda = Ns(rd, "Servencomenda"),
                    EMAILENC = Bn(rd, "EMAILENC"),
                    EMAILPROD = Bn(rd, "EMAILPROD"),
                    EMAILPRODEXT = Bn(rd, "EMAILPRODEXT"),
                    EMAILFINAL = Bn(rd, "EMAILFINAL"),
                    EMAILLIQUID = Bn(rd, "EMAILLIQUID"),
                    PEQDESCSERVICO = Ns(rd, "PEQDESCSERVICO"),
                    DESCPROD = Ns(rd, "DESCPROD"),
                    TEMPO = Ns(rd, "TEMPO"),
                });
            }

            return list;
        }

        public static async Task<List<OrderedProduct>> GetOrderedProductsAsync(string orderNo)
        {
            var list = new List<OrderedProduct>();
            using var conn = new MySqlConnector.MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            // Tenta ser robusto: remove espaços, usa LOWER e TRIM
            const string sql = @"
                SELECT * 
                FROM ORDEREDPRODUCT 
                WHERE LOWER(REPLACE(OrderNo, ' ', '')) = LOWER(REPLACE(@no, ' ', ''))
                   OR TRIM(OrderNo) = TRIM(@no)";

            using var cmd = new MySqlConnector.MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@no", orderNo ?? "");

            using var rd = await cmd.ExecuteReaderAsync();
            
            // Helpers locais
            static string S(MySqlConnector.MySqlDataReader r, string col) => r.IsDBNull(r.GetOrdinal(col)) ? "" : r.GetString(col);
            static string SSafe(MySqlConnector.MySqlDataReader r, string col)
            {
                try
                {
                    return r.IsDBNull(r.GetOrdinal(col)) ? "" : r.GetString(col);
                }
                catch (IndexOutOfRangeException)
                {
                    return "";
                }
            }
            static decimal D(MySqlConnector.MySqlDataReader r, string col) => r.IsDBNull(r.GetOrdinal(col)) ? 0m : r.GetDecimal(col);
            static long L(MySqlConnector.MySqlDataReader r, string col) => r.IsDBNull(r.GetOrdinal(col)) ? 0L : r.GetInt64(col);
            static DateTime? DT(MySqlConnector.MySqlDataReader r, string col) => r.IsDBNull(r.GetOrdinal(col)) ? null : r.GetDateTime(col);

            while (await rd.ReadAsync())
            {
                list.Add(new OrderedProduct
                {
                    Id = L(rd, "Id"),
                    OrderNo = S(rd, "OrderNo"),
                    Numserv = SSafe(rd, "Numserv"),
                    ProductCode = S(rd, "ProductCode"),
                    ProductName = S(rd, "ProductName"),
                    Cor = S(rd, "Cor"),
                    Tam = S(rd, "Tam"),
                    Quantidade = D(rd, "Quantidade"),
                    Altura = D(rd, "Altura"),
                    Largura = D(rd, "Largura"),
                    M2 = D(rd, "M2"),
                    PrecoUnit = D(rd, "PrecoUnit"),
                    SUBTOTAIS = D(rd, "SUBTOTAIS"),
                    PrecoCusto = D(rd, "PrecoCusto"),
                    SubtotalCusto = D(rd, "SubtotalCusto"),
                    DATACOMPRA = DT(rd, "DATACOMPRA"),
                    SubTotal = S(rd, "SubTotal"),
                    SUBTOTALNUM = D(rd, "SUBTOTALNUM")
                });
            }
            return list;
        }

        public static async Task<List<OrderedProduct>> GetOrderedProductsExtendedAsync(OrderInfoModel order)
        {
            // 1. Tenta pelo OrderNo (Trimmed)
            var list = await GetOrderedProductsAsync(order.OrderNo?.Trim());
            if (list.Count > 0) return list;

            // 2. Tenta pelo Numserv (se existir)
            if (!string.IsNullOrWhiteSpace(order.Numserv) && order.Numserv?.Trim() != order.OrderNo?.Trim())
            {
                list = await GetOrderedProductsAsync(order.Numserv?.Trim());
                if (list.Count > 0) return list;
            }

            // 3. Tenta pelo Servencomenda (se existir)
            if (!string.IsNullOrWhiteSpace(order.Servencomenda) && order.Servencomenda?.Trim() != order.OrderNo?.Trim())
            {
                list = await GetOrderedProductsAsync(order.Servencomenda?.Trim());
                if (list.Count > 0) return list;
            }

            // 4. Fallback: Se OrderNo tiver '/', tenta a parte direita (ex: "2024/123" -> "123")
            if (!string.IsNullOrWhiteSpace(order.OrderNo) && order.OrderNo.Contains('/'))
            {
                var parts = order.OrderNo.Split('/');
                if (parts.Length > 1)
                {
                    var suffix = parts.Last().Trim();
                    if (!string.IsNullOrWhiteSpace(suffix) && suffix.Length > 1)
                    {
                        list = await GetOrderedProductsAsync(suffix);
                        if (list.Count > 0) return list;
                    }
                }
            }

            return list;
        }

                private static bool? _orderedProductHasNumserv;

                private static async Task<bool> OrderedProductHasNumservAsync(MySqlConnection conn, MySqlTransaction tx, CancellationToken ct)
                {
                        if (_orderedProductHasNumserv.HasValue) return _orderedProductHasNumserv.Value;

                        const string sql = @"
SELECT 1
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'ORDEREDPRODUCT'
    AND COLUMN_NAME = 'Numserv'
LIMIT 1;";

                        using var cmd = new MySqlCommand(sql, conn, tx);
                        var result = await cmd.ExecuteScalarAsync(ct);
                        _orderedProductHasNumserv = result != null;
                        return _orderedProductHasNumserv.Value;
                }

                public static async Task<string> SaveOrderWithProductsAsync(OrderInfoModel order, CancellationToken ct = default)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            if (string.IsNullOrWhiteSpace(order.CustomerNo)) throw new ArgumentException("CustomerNo obrigatório", nameof(order));

            order.OrderDate ??= DateTime.Now;
            order.OrderStatus ??= "Orçamento";
            order.OrderNo = string.IsNullOrWhiteSpace(order.OrderNo) ? GenerateOrderNumber() : order.OrderNo.Trim();
            order.Numserv = string.IsNullOrWhiteSpace(order.Numserv) ? order.OrderNo : order.Numserv?.Trim();
            order.Servencomenda = string.IsNullOrWhiteSpace(order.Servencomenda) ? order.OrderNo : order.Servencomenda?.Trim();
            order.ANO ??= order.OrderDate?.Year.ToString();
            order.Utilizador ??= UserSession.Current.User?.Name;

            var products = order.Products ?? new List<OrderedProduct>();
            foreach (var p in products)
            {
                p.OrderNo = order.OrderNo;
                p.Numserv ??= order.Numserv;
            }

            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync(ct);
            using var tx = await conn.BeginTransactionAsync(ct);

            const string orderSql = @"
INSERT INTO OrderInfo
    (OrderNo, OrderDate, OrderStatus, CustomerNo, CustomerName, SubTotal, TaxPercentage, TaxAmount, TotalAmount,
     OrderDateEnt, DescPercentage, Desconto, observacoes, utilizador, ANO, CONTROLVEND, Numserv, Servencomenda, DESCPROD)
VALUES
    (@OrderNo, @OrderDate, @OrderStatus, @CustomerNo, @CustomerName, @SubTotal, @TaxPercentage, @TaxAmount, @TotalAmount,
     @OrderDateEnt, @DescPercentage, @Desconto, @Observacoes, @Utilizador, @ANO, @CONTROLVEND, @Numserv, @Servencomenda, @DESCPROD)
ON DUPLICATE KEY UPDATE
    OrderDate = VALUES(OrderDate),
    OrderStatus = VALUES(OrderStatus),
    CustomerNo = VALUES(CustomerNo),
    CustomerName = VALUES(CustomerName),
    SubTotal = VALUES(SubTotal),
    TaxPercentage = VALUES(TaxPercentage),
    TaxAmount = VALUES(TaxAmount),
    TotalAmount = VALUES(TotalAmount),
    OrderDateEnt = VALUES(OrderDateEnt),
    DescPercentage = VALUES(DescPercentage),
    Desconto = VALUES(Desconto),
    observacoes = VALUES(observacoes),
    utilizador = VALUES(utilizador),
    ANO = VALUES(ANO),
    CONTROLVEND = VALUES(CONTROLVEND),
    Numserv = VALUES(Numserv),
    Servencomenda = VALUES(Servencomenda),
    DESCPROD = VALUES(DESCPROD);";

            using (var cmd = new MySqlCommand(orderSql, conn, tx))
            {
                cmd.Parameters.Add("@OrderNo", MySqlDbType.VarChar, 50).Value = order.OrderNo;
                cmd.Parameters.Add("@OrderDate", MySqlDbType.DateTime).Value = order.OrderDate;
                cmd.Parameters.Add("@OrderStatus", MySqlDbType.VarChar, 50).Value = order.OrderStatus ?? "";
                cmd.Parameters.Add("@CustomerNo", MySqlDbType.VarChar, 30).Value = order.CustomerNo ?? "";
                cmd.Parameters.Add("@CustomerName", MySqlDbType.VarChar, 150).Value = order.CustomerName ?? "";
                cmd.Parameters.Add("@SubTotal", MySqlDbType.Decimal).Value = order.SubTotal ?? 0m;
                cmd.Parameters.Add("@TaxPercentage", MySqlDbType.Decimal).Value = order.TaxPercentage ?? 0m;
                cmd.Parameters.Add("@TaxAmount", MySqlDbType.Decimal).Value = order.TaxAmount ?? 0m;
                cmd.Parameters.Add("@TotalAmount", MySqlDbType.Decimal).Value = order.TotalAmount ?? 0m;
                cmd.Parameters.Add("@OrderDateEnt", MySqlDbType.DateTime).Value = order.OrderDateEnt ?? (object)DBNull.Value;
                cmd.Parameters.Add("@DescPercentage", MySqlDbType.Decimal).Value = order.DescPercentage ?? 0m;
                cmd.Parameters.Add("@Desconto", MySqlDbType.Decimal).Value = order.Desconto ?? 0m;
                cmd.Parameters.Add("@Observacoes", MySqlDbType.VarChar, 4000).Value = order.Observacoes ?? "";
                cmd.Parameters.Add("@Utilizador", MySqlDbType.VarChar, 80).Value = order.Utilizador ?? "";
                cmd.Parameters.Add("@ANO", MySqlDbType.VarChar, 10).Value = order.ANO ?? "";
                cmd.Parameters.Add("@CONTROLVEND", MySqlDbType.VarChar, 80).Value = order.CONTROLVEND ?? "";
                cmd.Parameters.Add("@Numserv", MySqlDbType.VarChar, 40).Value = order.Numserv ?? "";
                cmd.Parameters.Add("@Servencomenda", MySqlDbType.VarChar, 40).Value = order.Servencomenda ?? "";
                cmd.Parameters.Add("@DESCPROD", MySqlDbType.VarChar, 4000).Value = order.DESCPROD ?? "";

                await cmd.ExecuteNonQueryAsync(ct);
            }

            var hasNumservColumn = await OrderedProductHasNumservAsync(conn, tx, ct);

            // Reescreve linhas associadas
            var deleteSql = hasNumservColumn
                ? "DELETE FROM ORDEREDPRODUCT WHERE OrderNo = @ord OR (Numserv IS NOT NULL AND Numserv = @num)"
                : "DELETE FROM ORDEREDPRODUCT WHERE OrderNo = @ord";

            using (var del = new MySqlCommand(deleteSql, conn, tx))
            {
                del.Parameters.AddWithValue("@ord", order.OrderNo);
                if (hasNumservColumn)
                {
                    del.Parameters.AddWithValue("@num", order.Numserv ?? order.OrderNo);
                }
                await del.ExecuteNonQueryAsync(ct);
            }

            var lineSql = hasNumservColumn
                ? @"
INSERT INTO ORDEREDPRODUCT
    (OrderNo, Numserv, ProductCode, ProductName, Cor, Tam, Quantidade, Altura, Largura, M2, PrecoUnit, SUBTOTAIS, PrecoCusto, SubtotalCusto, DATACOMPRA, SubTotal, SUBTOTALNUM)
VALUES
    (@OrderNo, @Numserv, @ProductCode, @ProductName, @Cor, @Tam, @Quantidade, @Altura, @Largura, @M2, @PrecoUnit, @SUBTOTAIS, @PrecoCusto, @SubtotalCusto, @DATACOMPRA, @SubTotal, @SUBTOTALNUM);"
                : @"
INSERT INTO ORDEREDPRODUCT
    (OrderNo, ProductCode, ProductName, Cor, Tam, Quantidade, Altura, Largura, M2, PrecoUnit, SUBTOTAIS, PrecoCusto, SubtotalCusto, DATACOMPRA, SubTotal, SUBTOTALNUM)
VALUES
    (@OrderNo, @ProductCode, @ProductName, @Cor, @Tam, @Quantidade, @Altura, @Largura, @M2, @PrecoUnit, @SUBTOTAIS, @PrecoCusto, @SubtotalCusto, @DATACOMPRA, @SubTotal, @SUBTOTALNUM);";

            foreach (var p in products)
            {
                using var lineCmd = new MySqlCommand(lineSql, conn, tx);
                lineCmd.Parameters.Add("@OrderNo", MySqlDbType.VarChar, 50).Value = order.OrderNo;
                if (hasNumservColumn)
                {
                    lineCmd.Parameters.Add("@Numserv", MySqlDbType.VarChar, 40).Value = order.Numserv ?? order.OrderNo;
                }
                lineCmd.Parameters.Add("@ProductCode", MySqlDbType.VarChar, 60).Value = p.ProductCode ?? "";
                lineCmd.Parameters.Add("@ProductName", MySqlDbType.VarChar, 255).Value = p.ProductName ?? "";
                lineCmd.Parameters.Add("@Cor", MySqlDbType.VarChar, 80).Value = p.Cor ?? "";
                lineCmd.Parameters.Add("@Tam", MySqlDbType.VarChar, 40).Value = p.Tam ?? "";
                lineCmd.Parameters.Add("@Quantidade", MySqlDbType.Decimal).Value = p.Quantidade;
                lineCmd.Parameters.Add("@Altura", MySqlDbType.Decimal).Value = p.Altura;
                lineCmd.Parameters.Add("@Largura", MySqlDbType.Decimal).Value = p.Largura;
                lineCmd.Parameters.Add("@M2", MySqlDbType.Decimal).Value = p.M2;
                lineCmd.Parameters.Add("@PrecoUnit", MySqlDbType.Decimal).Value = p.PrecoUnit;
                lineCmd.Parameters.Add("@SUBTOTAIS", MySqlDbType.Decimal).Value = p.SUBTOTAIS;
                lineCmd.Parameters.Add("@PrecoCusto", MySqlDbType.Decimal).Value = p.PrecoCusto;
                lineCmd.Parameters.Add("@SubtotalCusto", MySqlDbType.Decimal).Value = p.SubtotalCusto;
                lineCmd.Parameters.Add("@DATACOMPRA", MySqlDbType.DateTime).Value = p.DATACOMPRA ?? (object)DBNull.Value;
                lineCmd.Parameters.Add("@SubTotal", MySqlDbType.VarChar, 100).Value = p.SubTotal ?? "";
                lineCmd.Parameters.Add("@SUBTOTALNUM", MySqlDbType.Decimal).Value = p.SUBTOTALNUM ?? p.SUBTOTAIS;

                await lineCmd.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
            return order.OrderNo;
        }

        private static string GenerateOrderNumber()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmssfff");
        }







        public static async Task<List<MonthlyHoursData>> GetMonthlyHoursStatsAsync(int? colabId, int year)
        {
            var list = new List<MonthlyHoursData>();
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            string filter = "WHERE YEAR(DataTrabalho) = @year";
            if (colabId.HasValue) filter += " AND IDColaborador = @colabId";

            string sql = $@"
                SELECT 
                    MONTH(DataTrabalho) as Mes,
                    SUM(HorasTrab) as Normais,
                    SUM(HorasExtras) as Extras
                FROM HORASTRABALHADAS
                {filter}
                GROUP BY MONTH(DataTrabalho)
                ORDER BY MONTH(DataTrabalho)";

            var dataMap = new Dictionary<int, (double N, double E)>();

            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@year", year);
                if (colabId.HasValue) cmd.Parameters.AddWithValue("@colabId", colabId.Value);
                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    int m = rd.GetInt32(0);
                    double n = rd.GetDouble(1);
                    double e = rd.GetDouble(2);
                    dataMap[m] = (n, e);
                }
            }

            // Fill all 12 months
            var culture = new CultureInfo("pt-PT");
            for (int i = 1; i <= 12; i++)
            {
                var item = new MonthlyHoursData
                {
                    MesNumero = i,
                    Ano = year,
                    Mes = culture.DateTimeFormat.GetAbbreviatedMonthName(i).Replace(".", "").ToUpper(),
                    HorasNormais = dataMap.ContainsKey(i) ? dataMap[i].N : 0,
                    HorasExtras = dataMap.ContainsKey(i) ? dataMap[i].E : 0,
                    HorasIdeais = CalculateIdealHours(i, year)
                };
                list.Add(item);
            }

            return list;
        }

        private static double CalculateIdealHours(int month, int year)
        {
            int days = DateTime.DaysInMonth(year, month);
            int workDays = 0;
            for (int d = 1; d <= days; d++)
            {
                var date = new DateTime(year, month, d);
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                    workDays++;
            }
            return workDays * 8.0;
        }

        public static async Task<List<DailyHoursData>> GetDailyHoursStatsAsync(int? colabId, int month, int year)
        {
            var list = new List<DailyHoursData>();
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            string filter = "WHERE YEAR(DataTrabalho) = @year AND MONTH(DataTrabalho) = @month";
            if (colabId.HasValue) filter += " AND IDColaborador = @colabId";

            string sql = $@"
                SELECT 
                    DAY(DataTrabalho) as Dia,
                    SUM(HorasTrab) as Normais,
                    SUM(HorasExtras) as Extras,
                    MAX(DescCentroCusto) as AbsenceType
                FROM HORASTRABALHADAS
                {filter}
                GROUP BY DAY(DataTrabalho)
                ORDER BY DAY(DataTrabalho)";

            var dataMap = new Dictionary<int, (double N, double E, string A)>();

            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@year", year);
                cmd.Parameters.AddWithValue("@month", month);
                if (colabId.HasValue) cmd.Parameters.AddWithValue("@colabId", colabId.Value);
                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    int d = rd.GetInt32(0);
                    double n = rd.GetDouble(1);
                    double e = rd.GetDouble(2);
                    string a = rd.IsDBNull(3) ? string.Empty : rd.GetString(3);
                    dataMap[d] = (n, e, a);
                }
            }

            int daysInMonth = DateTime.DaysInMonth(year, month);
            for (int d = 1; d <= daysInMonth; d++)
            {
                var date = new DateTime(year, month, d);
                bool isWorkDay = date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;
                
                string absence = dataMap.ContainsKey(d) ? dataMap[d].A : string.Empty;

                list.Add(new DailyHoursData
                {
                    Dia = d,
                    Data = date,
                    HorasNormais = dataMap.ContainsKey(d) ? dataMap[d].N : 0,
                    HorasExtras = dataMap.ContainsKey(d) ? dataMap[d].E : 0,
                    HorasIdeais = isWorkDay ? 8.0 : 0,
                    IsAbsent = !string.IsNullOrEmpty(absence),
                    AbsenceType = absence
                });
            }

            return list;
        }

        public static async Task<DashboardMetrics> GetDashboardMetricsAsync(int? colabId, int year)
        {
            var m = new DashboardMetrics();
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            // Base filter
            string filter = "WHERE YEAR(DataTrabalho) = @year";
            if (colabId.HasValue) filter += " AND IDColaborador = @colabId";

            // 1. Totals
            // Work days are those with actual hours recorded (not absences)
            string sqlTotals = $@"
                SELECT 
                    SUM(HorasTrab) as Normais, 
                    SUM(HorasExtras) as Extras, 
                    COUNT(DISTINCT DataTrabalho) as DiasTrab
                FROM HORASTRABALHADAS 
                {filter} AND IDCentroCusto IS NULL";

            using (var cmd = new MySqlCommand(sqlTotals, conn))
            {
                cmd.Parameters.AddWithValue("@year", year);
                if (colabId.HasValue) cmd.Parameters.AddWithValue("@colabId", colabId.Value);

                using var rd = await cmd.ExecuteReaderAsync();
                if (await rd.ReadAsync())
                {
                    m.TotalHorasNormais = rd.IsDBNull(0) ? 0 : rd.GetDouble(0);
                    m.TotalHorasExtras = rd.IsDBNull(1) ? 0 : rd.GetDouble(1);
                    m.TotalDiasTrabalhados = rd.IsDBNull(2) ? 0 : rd.GetInt32(2);
                }
            }
            m.TotalHoras = m.TotalHorasNormais + m.TotalHorasExtras;
            if (m.TotalDiasTrabalhados > 0)
                m.MediaHorasDia = m.TotalHoras / m.TotalDiasTrabalhados;
            
            m.SaldoHoras = m.TotalHoras - (m.TotalDiasTrabalhados * 8);

            // 2. Absences (Records with IdCentroCusto NOT NULL)
            string sqlAbs = $@"SELECT COUNT(*) FROM HORASTRABALHADAS {filter} AND IDCentroCusto IS NOT NULL";
            using (var cmd = new MySqlCommand(sqlAbs, conn))
            {
                cmd.Parameters.AddWithValue("@year", year);
                if (colabId.HasValue) cmd.Parameters.AddWithValue("@colabId", colabId.Value);
                m.TotalAusencias = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // 3. Top Client (Exclude absences)
            string sqlTop = $@"
                SELECT Cliente 
                FROM HORASTRABALHADAS
                {filter} AND IDCentroCusto IS NULL
                GROUP BY Cliente
                ORDER BY SUM(HorasTrab + HorasExtras) DESC
                LIMIT 1";
            
            using (var cmd = new MySqlCommand(sqlTop, conn))
            {
                cmd.Parameters.AddWithValue("@year", year);
                if (colabId.HasValue) cmd.Parameters.AddWithValue("@colabId", colabId.Value);
                var res = await cmd.ExecuteScalarAsync();
                m.TopCliente = res?.ToString() ?? "-";
            }

            return m;
        }

        public static async Task<List<Colaborador>> GetColaboradoresAsync()
        {
            var list = new List<Colaborador>();
            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();

                var sql = @"SELECT ID, Nome, Funcao, ValorHora 
                            FROM COLABORADORESTRAB
                            ORDER BY Nome";

                using var cmd = new MySqlCommand(sql, conn);
                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    list.Add(new Colaborador
                    {
                        ID = rd.GetInt32(0),
                        Nome = rd.IsDBNull(1) ? string.Empty : rd.GetString(1),
                        Funcao = rd.IsDBNull(2) ? null : rd.GetString(2),
                        ValorHora = rd.IsDBNull(3) ? null : rd.GetDecimal(3)
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao obter colaboradores: {ex.Message}");
                Console.WriteLine($"ERROR: GetColaboradoresAsync failed: {ex.Message}");
            }
            return list;
        }

        public static async Task<List<AbsenceSummary>> GetAbsenceStatsAsync(int? colabId, int year)
        {
            var list = new List<AbsenceSummary>();
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            string filter = "WHERE YEAR(h.DataTrabalho) = @year AND h.IDCentroCusto IS NOT NULL";
            if (colabId.HasValue) filter += " AND h.IDColaborador = @colabId";

            string sql = $@"
                SELECT h.DescCentroCusto, COUNT(*) as Qtd, MAX(t.Icon) as Icon
                FROM HORASTRABALHADAS h
                LEFT JOIN TIPOS_AUSENCIA t ON h.IDCentroCusto = t.ID
                {filter}
                GROUP BY h.DescCentroCusto
                ORDER BY Qtd DESC";

            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@year", year);
                if (colabId.HasValue) cmd.Parameters.AddWithValue("@colabId", colabId.Value);
                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    string tipo = rd.IsDBNull(0) ? "Outros" : rd.GetString(0);
                    string iconCode = rd.IsDBNull(2) ? "uf073" : rd.GetString(2);

                    list.Add(new AbsenceSummary
                    {
                        Tipo = tipo,
                        Dias = rd.GetInt32(1),
                        Cor = GetColorForAbsence(tipo),
                        Icon = ParseIconCode(iconCode)
                    });
                }
            }
            return list;
        }

        private static string ParseIconCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "\uf073";
            
            // If it's already a single character (the icon itself), return it
            if (code.Length == 1) return code;

            // Otherwise, try to parse as hex code (e.g. "uf073" or "\uf073")
            var cleanCode = code.TrimStart('\\', 'u', 'U');
            try
            {
                int charCode = int.Parse(cleanCode, System.Globalization.NumberStyles.HexNumber);
                return ((char)charCode).ToString();
            }
            catch
            {
                // If parsing fails, return the original code (it might be a valid string that just failed hex parsing)
                // or return default if it looks garbage. 
                // For now, let's assume if it's not hex, it might be the icon itself but maybe with some whitespace?
                return code.Trim(); 
            }
        }

        private static string GetColorForAbsence(string type)
        {
            return type.ToLower() switch
            {
                "férias" => "#FF2D55", // Red/Pink
                "baixa" => "#AF52DE", // Purple
                "feriado" => "#FF9500", // Orange
                _ => "#8E8E93" // Gray
            };
        }



        public static async Task<List<string>> GetAbsenceDetailsAsync(int? colabId, string type, int year)
        {
            var list = new List<string>();
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            string filter = "WHERE YEAR(DataTrabalho) = @year AND DescCentroCusto = @type AND IDCentroCusto IS NOT NULL";
            if (colabId.HasValue) filter += " AND IDColaborador = @colabId";

            string sql = $@"
                SELECT DataTrabalho
                FROM HORASTRABALHADAS
                {filter}
                ORDER BY DataTrabalho";

            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@year", year);
                cmd.Parameters.AddWithValue("@type", type);
                if (colabId.HasValue) cmd.Parameters.AddWithValue("@colabId", colabId.Value);
                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    list.Add(rd.GetDateTime(0).ToString("dd/MM/yyyy"));
                }
            }
            return list;
        }

        public static async Task<List<ClientHoursSummary>> GetClientHoursStatsAsync(int? colabId, int year)
        {
            var list = new List<ClientHoursSummary>();
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            string filter = "WHERE YEAR(DataTrabalho) = @year";
            if (colabId.HasValue) filter += " AND IDColaborador = @colabId";

            // Get Total first for percentage
            string sqlTotal = $"SELECT SUM(HorasTrab + HorasExtras) FROM HORASTRABALHADAS {filter}";
            double total = 0;
            using (var cmd = new MySqlCommand(sqlTotal, conn))
            {
                cmd.Parameters.AddWithValue("@year", year);
                if (colabId.HasValue) cmd.Parameters.AddWithValue("@colabId", colabId.Value);
                var res = await cmd.ExecuteScalarAsync();
                if (res != null && res != DBNull.Value) total = Convert.ToDouble(res);
            }

            if (total == 0) return list;

            string sql = $@"
                SELECT Cliente, SUM(HorasTrab + HorasExtras) as Total
                FROM HORASTRABALHADAS
                {filter}
                GROUP BY Cliente
                ORDER BY Total DESC
                LIMIT 5";

            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@year", year);
                if (colabId.HasValue) cmd.Parameters.AddWithValue("@colabId", colabId.Value);
                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    string cliente = rd.IsDBNull(0) ? "Sem Cliente" : rd.GetString(0);
                    double h = rd.GetDouble(1);
                    list.Add(new ClientHoursSummary
                    {
                        Cliente = cliente,
                        Horas = h,
                        Percentagem = h / total
                    });
                }
            }
            return list;
        }

        public static async Task<List<HoraColaborador>> GetHorasColaboradorAsync(
            int? idColaborador = null,
            DateTime? dataInicio = null,
            DateTime? dataFim = null)
        {
            var list = new List<HoraColaborador>();
            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();

                // Join with TIPOS_AUSENCIA to get the Icon
                var sql = @"SELECT h.ID, h.DataTrabalho, h.IDColaborador, h.NomeColaborador, 
                                   h.IDCliente, h.Cliente, h.IDCentroCusto, h.DescCentroCusto,
                                   h.HorasTrab, h.HorasExtras, h.Observacoes,
                                   t.Icon
                            FROM HORASTRABALHADAS h
                            LEFT JOIN TIPOS_AUSENCIA t ON h.IDCentroCusto = t.ID
                            WHERE (@IDColaborador IS NULL OR h.IDColaborador = @IDColaborador)
                              AND (@DataInicio IS NULL OR h.DataTrabalho >= @DataInicio)
                              AND (@DataFim IS NULL OR h.DataTrabalho <= @DataFim)
                            ORDER BY h.DataTrabalho DESC";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@IDColaborador", idColaborador.HasValue ? idColaborador.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@DataInicio", dataInicio.HasValue ? dataInicio.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@DataFim", dataFim.HasValue ? dataFim.Value : DBNull.Value);

                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    list.Add(new HoraColaborador
                    {
                        Id = rd.GetInt32(0),
                        DataTrabalho = rd.GetDateTime(1),
                        IdColaborador = rd.GetInt32(2),
                        NomeColaborador = rd.IsDBNull(3) ? string.Empty : rd.GetString(3),
                        IdCliente = rd.IsDBNull(4) ? null : rd.GetString(4),
                        Cliente = rd.IsDBNull(5) ? null : rd.GetString(5),
                        IdCentroCusto = rd.IsDBNull(6) ? null : rd.GetInt32(6),
                        DescCentroCusto = rd.IsDBNull(7) ? null : rd.GetString(7),
                        HorasTrab = rd.GetFloat(8),
                        HorasExtras = rd.GetFloat(9),
                        Observacoes = rd.IsDBNull(10) ? null : rd.GetString(10),
                        Icon = rd.IsDBNull(11) ? null : ParseIconCode(rd.GetString(11))
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro GetHorasColaboradorAsync: {ex.Message}");
            }
            return list;
        }

        public static async Task DeleteHoraColaboradorAsync(int id)
        {
            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();

                var sql = "DELETE FROM HORASTRABALHADAS WHERE ID=@Id";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao apagar hora: {ex.Message}");
                throw;
            }
        }

        // ================= TIPOS DE AUSÊNCIA =================
        public static async Task CreateAbsenceTypesTableAsync()
        {
            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();
                using var cmd = new MySqlCommand(@"
                    CREATE TABLE IF NOT EXISTS TIPOS_AUSENCIA (
                        ID INT AUTO_INCREMENT PRIMARY KEY,
                        Descricao VARCHAR(100) NOT NULL,
                        Icon VARCHAR(20) DEFAULT '\uf073'
                    )", conn);
                await cmd.ExecuteNonQueryAsync();

                // Migration: Add Icon column if it doesn't exist
                try 
                {
                    // Note: In SQL, backslash might be consumed. We use a parameter or hex if possible, 
                    // but for simplicity here we just add the column. The default might end up as literal text if not careful.
                    // We will fix the values below anyway.
                    using var cmdAlter = new MySqlCommand("ALTER TABLE TIPOS_AUSENCIA ADD COLUMN Icon VARCHAR(20) DEFAULT '';", conn);
                    await cmdAlter.ExecuteNonQueryAsync();
                }
                catch { /* Column likely exists */ }

                // FIX: Corrigir ícones que ficaram como texto literal "uf073" ou vazios
                try
                {
                    // 1. Fix known types based on description
                    var updates = new[] 
                    { 
                        ("Férias", "\uf5ca"), 
                        ("Doença", "\uf236"), 
                        ("Feriado", "\uf79f"),
                        ("Outro", "\uf073")
                    };

                    foreach (var (desc, icon) in updates)
                    {
                        using var cmdFix = new MySqlCommand("UPDATE TIPOS_AUSENCIA SET Icon = @icon WHERE Descricao = @desc AND (Icon IS NULL OR CHAR_LENGTH(Icon) > 1)", conn);
                        cmdFix.Parameters.AddWithValue("@icon", icon);
                        cmdFix.Parameters.AddWithValue("@desc", desc);
                        await cmdFix.ExecuteNonQueryAsync();
                    }

                    // 2. Fix any remaining "uf073" literals to the actual calendar icon
                    using var cmdGeneralFix = new MySqlCommand("UPDATE TIPOS_AUSENCIA SET Icon = @icon WHERE Icon LIKE '%uf073%' AND CHAR_LENGTH(Icon) > 1", conn);
                    cmdGeneralFix.Parameters.AddWithValue("@icon", "\uf073");
                    await cmdGeneralFix.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao corrigir ícones: {ex.Message}");
                }

                // Inserir alguns tipos padrão se a tabela estiver vazia
                using var cmdCount = new MySqlCommand("SELECT COUNT(*) FROM TIPOS_AUSENCIA", conn);
                var count = Convert.ToInt32(await cmdCount.ExecuteScalarAsync());
                if (count == 0)
                {
                    var defaults = new[] 
                    { 
                        ("Férias", "\uf5ca"), // plane-departure
                        ("Doença", "\uf236"), // bed
                        ("Feriado", "\uf79f"), // umbrella-beach
                        ("Outro", "\uf073")   // calendar-alt
                    };
                    foreach (var (desc, icon) in defaults)
                    {
                        using var cmdInsert = new MySqlCommand("INSERT INTO TIPOS_AUSENCIA (Descricao, Icon) VALUES (@desc, @icon)", conn);
                        cmdInsert.Parameters.AddWithValue("@desc", desc);
                        cmdInsert.Parameters.AddWithValue("@icon", icon);
                        await cmdInsert.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao criar tabela de ausências: {ex.Message}");
            }
        }

        public static async Task<List<AbsenceType>> GetAbsenceTypesAsync()
        {
            var list = new List<AbsenceType>();
            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();
                using var cmd = new MySqlCommand("SELECT ID, Descricao, Icon FROM TIPOS_AUSENCIA ORDER BY Descricao", conn);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new AbsenceType
                    {
                        Id = reader.GetInt32(0),
                        Description = reader.GetString(1),
                        Icon = reader.IsDBNull(2) ? "\uf073" : reader.GetString(2)
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao obter tipos de ausência: {ex.Message}");
            }
            return list;
        }

        public static async Task<bool> AddAbsenceTypeAsync(string description, string icon)
        {
            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();
                using var cmd = new MySqlCommand("INSERT INTO TIPOS_AUSENCIA (Descricao, Icon) VALUES (@desc, @icon)", conn);
                cmd.Parameters.AddWithValue("@desc", description);
                cmd.Parameters.AddWithValue("@icon", icon);
                var rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao adicionar tipo de ausência: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> DeleteAbsenceTypeAsync(int id)
        {
            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();
                using var cmd = new MySqlCommand("DELETE FROM TIPOS_AUSENCIA WHERE ID = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                var rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao apagar tipo de ausência: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> UpdateAbsenceTypeAsync(AbsenceType type)
        {
            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();
                using var cmd = new MySqlCommand("UPDATE TIPOS_AUSENCIA SET Descricao = @desc, Icon = @icon WHERE ID = @id", conn);
                cmd.Parameters.AddWithValue("@desc", type.Description);
                cmd.Parameters.AddWithValue("@icon", type.Icon);
                cmd.Parameters.AddWithValue("@id", type.Id);
                var rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao atualizar tipo de ausência: {ex.Message}");
                return false;
            }
        }

        public static async Task<int> UpsertHoraColaboradorAsync(HoraColaborador hora)
        {
            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();

                if (hora.Id == 0)
                {
                    // FIX: Manually generate ID because the table does not have AUTO_INCREMENT
                    var maxIdSql = "SELECT COALESCE(MAX(ID), 0) FROM HORASTRABALHADAS";
                    using (var maxIdCmd = new MySqlCommand(maxIdSql, conn))
                    {
                        var maxId = Convert.ToInt32(await maxIdCmd.ExecuteScalarAsync());
                        var newId = maxId + 1;

                        // INSERT with explicit ID
                        var sql = @"INSERT INTO HORASTRABALHADAS 
                                    (ID, DataTrabalho, IDColaborador, NomeColaborador, IDCliente, Cliente, 
                                     IDCentroCusto, DescCentroCusto, HorasTrab, HorasExtras, Observacoes)
                                    VALUES 
                                    (@ID, @DataTrabalho, @IDColaborador, @NomeColaborador, @IDCliente, @Cliente,
                                     @IDCentroCusto, @DescCentroCusto, @HorasTrab, @HorasExtras, @Observacoes)";

                        using var cmd = new MySqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@ID", newId);
                        cmd.Parameters.AddWithValue("@DataTrabalho", hora.DataTrabalho);
                        cmd.Parameters.AddWithValue("@IDColaborador", hora.IdColaborador);
                        cmd.Parameters.AddWithValue("@NomeColaborador", hora.NomeColaborador);
                        cmd.Parameters.AddWithValue("@IDCliente", hora.IdCliente ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Cliente", hora.Cliente ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@IDCentroCusto", hora.IdCentroCusto ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DescCentroCusto", hora.DescCentroCusto ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@HorasTrab", hora.HorasTrab);
                        cmd.Parameters.AddWithValue("@HorasExtras", hora.HorasExtras);
                        cmd.Parameters.AddWithValue("@Observacoes", hora.Observacoes ?? (object)DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();
                        return newId;
                    }
                }
                else
                {
                    // UPDATE
                    var sql = @"UPDATE HORASTRABALHADAS 
                                SET DataTrabalho=@DataTrabalho, IDColaborador=@IDColaborador, 
                                    NomeColaborador=@NomeColaborador, IDCliente=@IDCliente, Cliente=@Cliente,
                                    IDCentroCusto=@IDCentroCusto, DescCentroCusto=@DescCentroCusto,
                                    HorasTrab=@HorasTrab, HorasExtras=@HorasExtras, Observacoes=@Observacoes
                                WHERE ID=@Id";

                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@Id", hora.Id);
                    cmd.Parameters.AddWithValue("@DataTrabalho", hora.DataTrabalho);
                    cmd.Parameters.AddWithValue("@IDColaborador", hora.IdColaborador);
                    cmd.Parameters.AddWithValue("@NomeColaborador", hora.NomeColaborador);
                    cmd.Parameters.AddWithValue("@IDCliente", hora.IdCliente ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Cliente", hora.Cliente ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@IDCentroCusto", hora.IdCentroCusto ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@DescCentroCusto", hora.DescCentroCusto ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@HorasTrab", hora.HorasTrab);
                    cmd.Parameters.AddWithValue("@HorasExtras", hora.HorasExtras);
                    cmd.Parameters.AddWithValue("@Observacoes", hora.Observacoes ?? (object)DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                    return hora.Id;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao guardar hora: {ex.Message}");
                throw;
            }
        }

        // ================= CORES / TAMANHOS =================
        public static async Task<List<Cor>> GetCoresAsync()
        {
            var list = new List<Cor>();
            try
            {
                await EnsureCorTableColumnsAsync();
                await SeedDefaultColorsAsync();
                await NormalizeColorNamesAsync();

                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();
                const string sql = "SELECT idcor, nomecor, codigohex, referencia FROM Cor ORDER BY nomecor";
                using var cmd = new MySqlCommand(sql, conn);
                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    list.Add(new Cor
                    {
                        IdCor = rd.IsDBNull(0) ? "" : rd.GetString(0),
                        NomeCor = rd.IsDBNull(1) ? "" : rd.GetString(1),
                        CodigoHex = rd.IsDBNull(2) ? null : rd.GetString(2),
                        Referencia = rd.IsDBNull(3) ? null : rd.GetString(3)
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao obter cores: {ex.Message}");
            }
            return list;
        }

        public static async Task<string> GetNextCorIdAsync()
        {
            try
            {
                await EnsureCorTableColumnsAsync();

                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();

                const string sql = @"SELECT MAX(CAST(SUBSTRING(idcor, 4) AS UNSIGNED))
FROM Cor
WHERE idcor REGEXP '^COR[0-9]+$'";

                using var cmd = new MySqlCommand(sql, conn);
                var result = await cmd.ExecuteScalarAsync();
                var currentMax = (result == null || result == DBNull.Value) ? 0 : Convert.ToInt32(result);
                var next = currentMax + 1;
                return $"COR{next:D3}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao gerar próximo IdCor: {ex.Message}");
                // fallback determinístico em caso de erro
                var fallback = Math.Abs(DateTime.UtcNow.Ticks % 999999);
                return $"COR{fallback:D6}";
            }
        }

        public static async Task<bool> AddCorAsync(Cor cor)
        {
            try
            {
                await EnsureCorTableColumnsAsync();

                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();
                const string sql = "INSERT INTO Cor (idcor, nomecor, codigohex, referencia) VALUES (@id, @nome, @codigo, @ref)";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", cor.IdCor);
                cmd.Parameters.AddWithValue("@nome", NormalizeColorName(cor.NomeCor));
                cmd.Parameters.AddWithValue("@codigo", (object?)cor.CodigoHex ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ref", (object?)cor.Referencia ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao adicionar cor: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> UpdateCorAsync(Cor cor)
        {
            try
            {
                await EnsureCorTableColumnsAsync();

                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();
                const string sql = "UPDATE Cor SET nomecor=@nome, codigohex=@codigo, referencia=@ref WHERE idcor=@id";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", cor.IdCor);
                cmd.Parameters.AddWithValue("@nome", NormalizeColorName(cor.NomeCor));
                cmd.Parameters.AddWithValue("@codigo", (object?)cor.CodigoHex ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ref", (object?)cor.Referencia ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao atualizar cor: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> DeleteCorAsync(string idCor)
        {
            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();
                const string sql = "DELETE FROM Cor WHERE idcor=@id";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", idCor);
                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao eliminar cor: {ex.Message}");
                return false;
            }
        }

        public static async Task EnsureCorTableAndNamesAsync()
        {
            await EnsureCorTableColumnsAsync();
        }

        private static async Task EnsureCorTableColumnsAsync()
        {
            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();

                const string checkSql = @"SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Cor' AND COLUMN_NAME IN ('codigohex', 'referencia')";
                using var checkCmd = new MySqlCommand(checkSql, conn);
                var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var reader = await checkCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        found.Add(reader.GetString(0));
                    }
                }

                if (!found.Contains("codigohex"))
                {
                    const string alterSql = "ALTER TABLE Cor ADD COLUMN codigohex VARCHAR(20) NULL AFTER nomecor";
                    using var alterCmd = new MySqlCommand(alterSql, conn);
                    await alterCmd.ExecuteNonQueryAsync();
                }

                if (!found.Contains("referencia"))
                {
                    const string alterSqlRef = "ALTER TABLE Cor ADD COLUMN referencia VARCHAR(80) NULL AFTER codigohex";
                    using var alterCmdRef = new MySqlCommand(alterSqlRef, conn);
                    await alterCmdRef.ExecuteNonQueryAsync();
                }

                // Garantir nomes em maiúsculas para registos existentes
                await NormalizeColorNamesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao garantir coluna codigohex em Cor: {ex.Message}");
            }
        }

        private static async Task SeedDefaultColorsAsync()
        {
            // Inserir um set de cores neon/claras se não existirem
            var defaults = new (string Id, string Nome, string Hex, string Referencia)[]
            {
                ("COR001", "BRANCO", "#FFFFFF", "REF-BRANCO"),
                ("COR002", "AMARELO", "#FFD60A", "REF-AMARELO"),
                ("COR003", "VERMELHO", "#FF375F", "REF-VERMELHO"),
                ("COR004", "LARANJA", "#FF9F0A", "REF-LARANJA"),
                ("COR005", "VERDE", "#30D158", "REF-VERDE"),
                ("COR006", "AZUL", "#0A84FF", "REF-AZUL"),
                ("COR007", "ROXO", "#BF5AF2", "REF-ROXO"),
                ("COR008", "ROSA", "#FF2D55", "REF-ROSA"),
                ("COR009", "CINZA", "#8E8E93", "REF-CINZA"),
                ("COR010", "CASTANHO", "#A86B3C", "REF-CASTANHO"),
                ("COR011", "MARROM", "#A86B3C", "REF-MARROM")
            };

            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();

                const string existsSql = "SELECT COUNT(1) FROM Cor WHERE idcor=@id";
                const string insertSql = "INSERT INTO Cor (idcor, nomecor, codigohex, referencia) VALUES (@id, @nome, @hex, @ref)";

                foreach (var item in defaults)
                {
                    using var check = new MySqlCommand(existsSql, conn);
                    check.Parameters.AddWithValue("@id", item.Id);
                    var exists = Convert.ToInt32(await check.ExecuteScalarAsync()) > 0;
                    if (exists) continue;

                    using var insert = new MySqlCommand(insertSql, conn);
                    insert.Parameters.AddWithValue("@id", item.Id);
                    insert.Parameters.AddWithValue("@nome", item.Nome);
                    insert.Parameters.AddWithValue("@hex", item.Hex);
                    insert.Parameters.AddWithValue("@ref", item.Referencia);
                    await insert.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao semear cores: {ex.Message}");
            }
        }

        private static async Task NormalizeColorNamesAsync()
        {
            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();

                const string sql = @"UPDATE Cor
SET nomecor = UPPER(TRIM(nomecor))
WHERE nomecor IS NOT NULL AND nomecor <> UPPER(TRIM(nomecor));";

                using var cmd = new MySqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao normalizar nomes de cor: {ex.Message}");
            }
        }

        private static string NormalizeColorName(string? value)
        {
            var trimmed = value?.Trim();
            return string.IsNullOrEmpty(trimmed) ? "" : trimmed.ToUpperInvariant();
        }

        public static async Task<List<Tamanho>> GetTamanhosAsync()
        {
            var list = new List<Tamanho>();
            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();
                const string sql = "SELECT idtamanho, nometamanho FROM Tamanhos ORDER BY nometamanho";
                using var cmd = new MySqlCommand(sql, conn);
                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    list.Add(new Tamanho
                    {
                        IdTamanho = rd.IsDBNull(0) ? "" : rd.GetString(0),
                        NomeTamanho = rd.IsDBNull(1) ? "" : rd.GetString(1)
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao obter tamanhos: {ex.Message}");
            }
            return list;
        }

        // ================= ESTADO SERVIÇO =================

        public static async Task CreateServiceStatusTableAsync()
        {
            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();
                var sql = @"
                    CREATE TABLE IF NOT EXISTS ESTADO_SERVICO (
                        ID INT PRIMARY KEY AUTO_INCREMENT,
                        Descricao VARCHAR(50) NOT NULL,
                        Cor VARCHAR(20) DEFAULT '#808080'
                    );";
                using var cmd = new MySqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();

                // Seed se vazio
                var countSql = "SELECT COUNT(*) FROM ESTADO_SERVICO";
                using var cmdCount = new MySqlCommand(countSql, conn);
                long count = Convert.ToInt64(await cmdCount.ExecuteScalarAsync());
                
                if (count == 0)
                {
                    var insertSql = @"
                        INSERT INTO ESTADO_SERVICO (Descricao, Cor) VALUES 
                        ('Pendente', '#FFA500'),
                        ('Em Curso', '#0000FF'),
                        ('Concluído', '#008000'),
                        ('Cancelado', '#FF0000');";
                    using var cmdInsert = new MySqlCommand(insertSql, conn);
                    await cmdInsert.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao criar tabela ESTADO_SERVICO: {ex.Message}");
            }
        }

        public static async Task<List<ServiceStatus>> GetServiceStatusAsync()
        {
            var list = new List<ServiceStatus>();
            try
            {
                await CreateServiceStatusTableAsync();

                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();
                var sql = "SELECT ID, Descricao, Cor FROM ESTADO_SERVICO ORDER BY ID";
                using var cmd = new MySqlCommand(sql, conn);
                using var rd = await cmd.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    list.Add(new ServiceStatus
                    {
                        ID = rd.GetInt32(0),
                        Descricao = rd.GetString(1),
                        Cor = rd.IsDBNull(2) ? "#808080" : rd.GetString(2)
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao obter estados de serviço: {ex.Message}");
            }
            return list;
        }

        public static async Task<bool> AddServiceStatusAsync(ServiceStatus status)
        {
            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();
                var sql = "INSERT INTO ESTADO_SERVICO (Descricao, Cor) VALUES (@Descricao, @Cor)";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Descricao", status.Descricao);
                cmd.Parameters.AddWithValue("@Cor", status.Cor);
                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao adicionar estado: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> UpdateServiceStatusAsync(ServiceStatus status)
        {
            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();
                var sql = "UPDATE ESTADO_SERVICO SET Descricao=@Descricao, Cor=@Cor WHERE ID=@ID";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Descricao", status.Descricao);
                cmd.Parameters.AddWithValue("@Cor", status.Cor);
                cmd.Parameters.AddWithValue("@ID", status.ID);
                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao atualizar estado: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> DeleteServiceStatusAsync(int id)
        {
            try
            {
                using var conn = new MySqlConnection(GetConnectionString());
                await conn.OpenAsync();
                var sql = "DELETE FROM ESTADO_SERVICO WHERE ID=@ID";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ID", id);
                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao eliminar estado: {ex.Message}");
                return false;
            }
        }
    }
}
