using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace Music.PostgresSQL
{
    public class TextSample
    {
        public int Id { get; set; }
        public string ShortText1 { get; set; }
        public string ShortText2 { get; set; }
        public string LongText { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class PostgresDateBase
    {
        private readonly string _connectionString;

        public PostgresDateBase(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void InitializeDatabase()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            var command = new NpgsqlCommand(@"
            CREATE TABLE IF NOT EXISTS tgbottables (
                id SERIAL PRIMARY KEY,
                short_text1 VARCHAR(50),
                short_text2 VARCHAR(100),
                long_text TEXT,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )", connection);

            command.ExecuteNonQuery();
        }

        public int Insert(TextSample sample)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            var command = new NpgsqlCommand(@"
            INSERT INTO tgbottables (short_text1, short_text2, long_text)
            VALUES (@short1, @short2, @long)
            RETURNING id", connection);

            command.Parameters.AddWithValue("@short1", sample.ShortText1);
            command.Parameters.AddWithValue("@short2", sample.ShortText2);
            command.Parameters.AddWithValue("@long", sample.LongText);

            return (int)command.ExecuteScalar();
        }
        public void InstallExstension()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var installExtension = new NpgsqlCommand(
                "CREATE EXTENSION IF NOT EXISTS fuzzystrmatch",
                connection);
            
            installExtension.ExecuteNonQuery();
        }
        public TextSample GetById(int id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            var command = new NpgsqlCommand(@"
            SELECT id, short_text1, short_text2, long_text, created_at
            FROM tgbottables
            WHERE id = @id", connection);

            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new TextSample
                {
                    Id = reader.GetInt32(0),
                    ShortText1 = reader.GetString(1),
                    ShortText2 = reader.GetString(2),
                    LongText = reader.GetString(3),
                    CreatedAt = reader.GetDateTime(4)
                };
            }

            return null;
        }
        public async Task<(string Word, int Distance)> GetSimilarName(string target)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.OpenAsync();

            var command = new NpgsqlCommand(@$"
                SELECT 
                    short_text1, levenshtein(short_text1, '{target}') AS distance
                FROM 
                    tgbottables
                ORDER BY 
                    distance ASC;", connection);

            command.Parameters.AddWithValue("@target", target);

            using var reader = await command.ExecuteReaderAsync();

            await reader.ReadAsync();

            return (reader.GetString(0), reader.GetInt32(1));
        }
        public static async void Demo()
        {
            var repo = new PostgresDateBase("Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=76127612d");
            repo.InitializeDatabase();

            repo.InstallExstension();

            var item = await repo.GetSimilarName("HELLO");

            Console.WriteLine(item);
        }
    }
}
