using System;
using MySqlConnector;

class Program
{
    static void Main(string[] args)
    {
        var builder = new MySqlConnectionStringBuilder
        {
            Server = "100.81.152.95",
            Port = 3308,
            Database = "YAHPUBLICIDADE2025",
            UserID = "YAH",
            Password = "#JONy2244&",
            ConnectionTimeout = 10
        };

        Console.WriteLine($"Connecting to {builder.Server}...");

        try
        {
            using var conn = new MySqlConnection(builder.ConnectionString);
            conn.Open();
            Console.WriteLine("Connected!");

            var sql = @"
                CREATE TABLE IF NOT EXISTS ESTADO_SERVICO (
                    ID INT PRIMARY KEY AUTO_INCREMENT,
                    Descricao VARCHAR(50) NOT NULL,
                    Cor VARCHAR(20) DEFAULT '#808080'
                );";
            
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.ExecuteNonQuery();
                Console.WriteLine("Table ESTADO_SERVICO created (or already exists).");
            }

            var countSql = "SELECT COUNT(*) FROM ESTADO_SERVICO";
            long count = 0;
            using (var cmdCount = new MySqlCommand(countSql, conn))
            {
                count = Convert.ToInt64(cmdCount.ExecuteScalar());
            }

            if (count == 0)
            {
                var insertSql = @"
                    INSERT INTO ESTADO_SERVICO (Descricao, Cor) VALUES 
                    ('Pendente', '#FFA500'),
                    ('Em Curso', '#0000FF'),
                    ('Concluído', '#008000'),
                    ('Cancelado', '#FF0000');";
                using (var cmdInsert = new MySqlCommand(insertSql, conn))
                {
                    cmdInsert.ExecuteNonQuery();
                    Console.WriteLine("Inserted default values.");
                }
            }
            else
            {
                Console.WriteLine($"Table already has {count} rows.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
