using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using PumpAndDumpBot.Data.Objects;

namespace PumpAndDumpBot.Data
{
    public static class Database
    {
        private static SqlConnection GetSqlConnection()
        {
            Uri uri = new Uri(ConfigurationManager.AppSettings["SQLSERVER_URI"]);
            string connectionString = new SqlConnectionStringBuilder
            {
                DataSource = uri.Host,
                InitialCatalog = uri.AbsolutePath.Trim('/'),
                UserID = uri.UserInfo.Split(':').First(),
                Password = uri.UserInfo.Split(':').Last(),
                MultipleActiveResultSets = true
            }.ConnectionString;

            return new SqlConnection(connectionString);
        }

        public static async Task<Announcement> GetActiveAnnouncementAsync()
        {
            Announcement result = null;
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                try
                {
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                await reader.ReadAsync();
                                result = new Announcement()
                                {
                                    Date = new DateTime((long)reader["Date"]),
                                    Coin = (string)reader["Message"],
                                    Btc = (string)reader["BTC"],
                                    Eth = (string)reader["ETH"]
                                };
                            }
                            reader.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    conn.Close();
                }
            }
            return result;
        }
    }
}