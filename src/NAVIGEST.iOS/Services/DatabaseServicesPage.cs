using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using NAVIGEST.iOS.Models;

namespace NAVIGEST.iOS.Services;

public static partial class DatabaseService
{
    // ---------- Helpers ----------
    private static decimal GetDecimal(MySqlDataReader rd, string col)
        => rd.IsDBNull(rd.GetOrdinal(col)) ? 0m : rd.GetDecimal(col);

    private static DateTime? GetNullableDate(MySqlDataReader rd, string col)
        => rd.IsDBNull(rd.GetOrdinal(col)) ? null : rd.GetDateTime(col);
         

    // ---------- ELIMINAR SERVIÇO ----------
    public static async Task<bool> DeleteServiceAsync(string orderNo)
    {
        await using var cn = new MySqlConnection(GetConnectionString());
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();
        try
        {
            async Task Exec(string sql)
            {
                var cmd = new MySqlCommand(sql, cn, (MySqlTransaction)tx);
                cmd.Parameters.Add("@o", MySqlDbType.VarChar).Value = orderNo;
                await cmd.ExecuteNonQueryAsync();
            }

            await Exec("DELETE FROM BillInfo WHERE OrderNo=@o");
            await Exec("DELETE FROM OrderedProduct WHERE OrderNo=@o");
            var delMain = new MySqlCommand("DELETE FROM OrderInfo WHERE OrderNo=@o", cn, (MySqlTransaction)tx);
            delMain.Parameters.Add("@o", MySqlDbType.VarChar).Value = orderNo;
            var rows = await delMain.ExecuteNonQueryAsync();
            await tx.CommitAsync();
            return rows > 0;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
         
}
