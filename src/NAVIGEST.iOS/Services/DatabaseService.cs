using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using NAVIGEST.iOS.Models;
using System.IO;
using System.Linq;

namespace NAVIGEST.iOS.Services
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
        }

        public static async Task<List<CompanyInfo>> GetActiveCompaniesAsync()
        {
            var list = new List<CompanyInfo>();
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            const string sql = @"
                SELECT CodEmp, Empresa, Ano, Logotipo
                FROM SETUP
                WHERE Ativa = '1'
                ORDER BY Empresa;";

            using var cmd = new MySqlCommand(sql, conn);
            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(new CompanyInfo
                {
                    CodEmp = rd.GetString("CodEmp"),
                    Empresa = rd.IsDBNull(rd.GetOrdinal("Empresa")) ? null : rd.GetString("Empresa"),
                    Ano = rd.IsDBNull(rd.GetOrdinal("Ano")) ? null : rd.GetString("Ano"),
                    Logotipo = rd.IsDBNull(rd.GetOrdinal("Logotipo")) ? null : (byte[])rd["Logotipo"]
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
                SELECT CLINOME, CLICODIGO, TELEFONE, EMAIL, EXTERNO, ANULADO,
                       VENDEDOR, VALORCREDITO, PastasSincronizadas
                FROM CLIENTES";
            if (!string.IsNullOrWhiteSpace(filtro))
                sql += " WHERE (LOWER(CLINOME) LIKE @f OR LOWER(CLICODIGO) LIKE @f OR LOWER(EMAIL) LIKE @f)";
            sql += " ORDER BY CLINOME;";

            using var cmd = new MySqlCommand(sql, conn);
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
                        int i => i != 0,
                        long l => l != 0,
                        string str => str == "1" || str.Equals("true", StringComparison.OrdinalIgnoreCase),
                        _ => false
                    };
                }

                list.Add(new Cliente
                {
                    CLINOME = rd.IsDBNull(rd.GetOrdinal("CLINOME")) ? null : rd.GetString("CLINOME"),
                    CLICODIGO = rd.IsDBNull(rd.GetOrdinal("CLICODIGO")) ? null : rd.GetString("CLICODIGO"),
                    TELEFONE = rd.IsDBNull(rd.GetOrdinal("TELEFONE")) ? null : rd.GetString("TELEFONE"),
                    EMAIL = rd.IsDBNull(rd.GetOrdinal("EMAIL")) ? null : rd.GetString("EMAIL"),
                    EXTERNO = GetBool("EXTERNO"),
                    ANULADO = GetBool("ANULADO"),
                    VENDEDOR = rd.IsDBNull(rd.GetOrdinal("VENDEDOR")) ? null : rd.GetString("VENDEDOR"),
                    VALORCREDITO = rd.IsDBNull(rd.GetOrdinal("VALORCREDITO")) ? null : rd.GetString("VALORCREDITO"),
                    PastasSincronizadas = GetBool("PastasSincronizadas")
                });
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
                        CLINOME=@nome, TELEFONE=@tel, EMAIL=@mail,
                        EXTERNO=@ext, ANULADO=@anu, VENDEDOR=@vend,
                        VALORCREDITO=@cred, PastasSincronizadas=@past
                    WHERE CLICODIGO=@cod LIMIT 1;"
                : @"INSERT INTO CLIENTES
                    (CLICODIGO, CLINOME, TELEFONE, EMAIL, EXTERNO, ANULADO, VENDEDOR, VALORCREDITO, PastasSincronizadas)
                    VALUES (@cod,@nome,@tel,@mail,@ext,@anu,@vend,@cred,@past);";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.Add("@cod", MySqlDbType.VarChar, 30).Value = c.CLICODIGO ?? "";
            cmd.Parameters.Add("@nome", MySqlDbType.VarChar, 150).Value = c.CLINOME ?? "";
            cmd.Parameters.Add("@tel", MySqlDbType.VarChar, 40).Value = c.TELEFONE ?? "";
            cmd.Parameters.Add("@mail", MySqlDbType.VarChar, 150).Value = c.EMAIL ?? "";
            cmd.Parameters.Add("@ext", MySqlDbType.Bit).Value = c.EXTERNO ? 1 : 0;
            cmd.Parameters.Add("@anu", MySqlDbType.Bit).Value = c.ANULADO ? 1 : 0;
            cmd.Parameters.Add("@vend", MySqlDbType.VarChar, 80).Value = c.VENDEDOR ?? "";
            cmd.Parameters.Add("@cred", MySqlDbType.VarChar, 40).Value = c.VALORCREDITO ?? "";
            cmd.Parameters.Add("@past", MySqlDbType.Bit).Value = c.PastasSincronizadas ? 1 : 0;
            var rows = await cmd.ExecuteNonQueryAsync(ct);
            return rows > 0;
        }

        public static async Task<bool> DeleteClienteAsync(string? codigo, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return false;

            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync(ct);

            const string depSql = "SELECT COUNT(*) FROM orderinfo WHERE CustomerNO=@cod;";
            using (var dep = new MySqlCommand(depSql, conn))
            {
                dep.Parameters.Add("@cod", MySqlDbType.VarChar, 30).Value = codigo;
                var count = Convert.ToInt32(await dep.ExecuteScalarAsync(ct));
                if (count > 0)
                    throw new InvalidOperationException("Impossível eliminar. Existem serviços associados.");
            }

            const string del = "DELETE FROM CLIENTES WHERE CLICODIGO=@cod LIMIT 1;";
            using var cmd = new MySqlCommand(del, conn);
            cmd.Parameters.Add("@cod", MySqlDbType.VarChar, 30).Value = codigo;
            var rows = await cmd.ExecuteNonQueryAsync(ct);
            return rows > 0;
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

        public static async Task<string> GetNextProductCodigoAsync(CancellationToken ct = default)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync(ct);

            string sql = $"SELECT MAX(CAST(SUBSTRING(PRODCODIGO,3) AS UNSIGNED)) FROM {ProdutosTable} WHERE PRODCODIGO REGEXP '^PRD[0-9]+$';";
            using var cmd = new MySqlCommand(sql, conn);
            var result = await cmd.ExecuteScalarAsync(ct);
            long max = (result == null || result == DBNull.Value) ? 0 : Convert.ToInt64(result);
            if (max >= 999999)
                throw new InvalidOperationException("Limite de códigos de produto atingido (PRD999999).");
            return "PRD" + (max + 1).ToString("D6");
        }

        public static async Task<List<Product>> GetProductsAsync(string? filtro = null, CancellationToken ct = default)
        {
            var list = new List<Product>();
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync(ct);

            string sql = $@"
                SELECT PRODCODIGO, PRODNOME, FAMILIA, COLABORADOR
                FROM {ProdutosTable}";
            if (!string.IsNullOrWhiteSpace(filtro))
                sql += " WHERE (LOWER(PRODNOME) LIKE @f OR LOWER(PRODCODIGO) LIKE @f OR LOWER(FAMILIA) LIKE @f)";
            sql += " ORDER BY PRODNOME;";

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
                    COLABORADOR = rd.IsDBNull(rd.GetOrdinal("COLABORADOR")) ? null : rd.GetString("COLABORADOR")
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
                        COLABORADOR=@col
                    WHERE PRODCODIGO=@cod LIMIT 1;"
                : $@"INSERT INTO {ProdutosTable}
                        (PRODCODIGO, PRODNOME, FAMILIA, COLABORADOR)
                    VALUES
                        (@cod,@nome,@fam,@col);";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.Add("@cod", MySqlDbType.VarChar, 30).Value = p.PRODCODIGO ?? "";
            cmd.Parameters.Add("@nome", MySqlDbType.VarChar, 150).Value = p.PRODNOME ?? "";
            cmd.Parameters.Add("@fam", MySqlDbType.VarChar, 80).Value = p.FAMILIA ?? "";
            cmd.Parameters.Add("@col", MySqlDbType.VarChar, 80).Value = p.COLABORADOR ?? "";
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
        public static async Task<(int Count, string? SampleOrderNo, string? SampleCustomer)> DebugOrdersProbeAsync(CancellationToken ct = default)
        {
            using var conn = new MySqlConnector.MySqlConnection(GetConnectionString());
            await conn.OpenAsync(ct);

            // Quantas linhas?
            using var cmdCount = new MySqlConnector.MySqlCommand("SELECT COUNT(*) FROM OrderInfo;", conn);
            var countObj = await cmdCount.ExecuteScalarAsync(ct);
            int count = (countObj == null || countObj == DBNull.Value) ? 0 : Convert.ToInt32(countObj);

            // Um exemplo (se houver)
            string? orderNo = null, customer = null;
            if (count > 0)
            {
                using var cmdOne = new MySqlConnector.MySqlCommand(
                    "SELECT OrderNo, CustomerName FROM OrderInfo ORDER BY OrderDate DESC LIMIT 1;", conn);
                using var rd = await cmdOne.ExecuteReaderAsync(ct);
                if (await rd.ReadAsync(ct))
                {
                    orderNo = rd.IsDBNull(0) ? null : rd.GetString(0);
                    customer = rd.IsDBNull(1) ? null : rd.GetString(1);
                }
            }
            return (count, orderNo, customer);
        }


    }
}
