using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using PumpAndDumpBot.Data.Objects;

namespace PumpAndDumpBot.Data
{
    public static class Database
    {
        public static SqlConnection GetSqlConnection()
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

        #region Announcement queries
        public static async Task<Announcement> GetAnnouncementAsync()
        {
            Announcement result = null;
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                try
                {
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM Announcement WHERE AnnouncementID = 1";
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                await reader.ReadAsync();
                                result = new Announcement()
                                {
                                    Date = (DateTime)reader["AnnounceDate"],
                                    Coin = (string)reader["Coin"],
                                    Pair = (string)reader["Pair"],
                                    PairGoal = (string)reader["PairGoal"]
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

        public static async Task InsertAnnouncementAsync(Announcement announcement)
        {
            if (announcement == null)
                throw new ArgumentNullException();

            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tr;
                            cmd.Parameters.AddWithValue("@AnnounceDate", DbType.DateTime).Value = announcement.Date;
                            cmd.Parameters.AddWithValue("@Coin", DbType.String).Value = announcement.Coin;
                            cmd.Parameters.AddWithValue("@Pair", DbType.String).Value = announcement.Pair;
                            cmd.Parameters.AddWithValue("@PairGoal", DbType.String).Value = announcement.PairGoal;
                            cmd.CommandText = "INSERT INTO Announcement(AnnouncementID, AnnounceDate, Coin, Pair, PairGoal) VALUES(1, @AnnounceDate, @Coin, @Pair, @PairGoal)";
                            await cmd.ExecuteNonQueryAsync();
                        }
                        tr.Commit();
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        throw ex;
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }
        }

        public static async Task DeleteAnnouncementAsync()
        {
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tr;
                            cmd.CommandText = "DELETE FROM Announcement";
                            await cmd.ExecuteNonQueryAsync();
                        }
                        tr.Commit();
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        throw ex;
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }
        }
        #endregion

        #region Invite queries
        public static async Task<int> GetInviteCountAsync(ulong userId)
        {
            int result = 0;
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                try
                {
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.Parameters.AddWithValue("@ReferrerID", DbType.Decimal).Value = (decimal)userId;
                        cmd.CommandText = "SELECT COUNT(1) as Count FROM Invites WHERE ReferrerID = @ReferrerID GROUP BY ReferrerID";
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                await reader.ReadAsync();
                                result = (int)reader["Count"];
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

        public static async Task InsertInviteAsync(ulong newUserId, ulong referrerId)
        {
            if (newUserId == referrerId) return;

            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlTransaction tr = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tr;
                            cmd.Parameters.AddWithValue("@NewUserID", DbType.Decimal).Value = (decimal)newUserId;
                            cmd.Parameters.AddWithValue("@ReferrerID", DbType.Decimal).Value = (decimal)referrerId;
                            cmd.CommandText = "INSERT INTO Invites(UserID, ReferrerID, JoinDate) VALUES(@NewUserID, @ReferrerID, GETDATE())";
                            await cmd.ExecuteNonQueryAsync();
                        }
                        tr.Commit();
                    }
                    catch (DbException ex)
                    when (ex.HResult == -2146232060) // when it's a primary key violation do nothing
                    {
                        tr.Rollback();
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        throw ex;
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }
        }   

        public static async Task InsertInvitesAsync(ConcurrentQueue<Invite> invites)
        {
            DateTime startTime = DateTime.Now;
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                while (invites.TryPeek(out Invite invite) && invite.JoinDate <= startTime)
                {
                    try
                    {
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Parameters.AddWithValue("@NewUserID", DbType.Decimal).Value = (decimal)invite.UserId;
                            cmd.Parameters.AddWithValue("@ReferrerID", DbType.Decimal).Value = (decimal)invite.ReferrerId;
                            cmd.Parameters.AddWithValue("@JoinDate", DbType.DateTime).Value = invite.JoinDate;
                            cmd.CommandText = "INSERT INTO Invites(UserID, ReferrerID, JoinDate) VALUES(@NewUserID, @ReferrerID, @JoinDate)";
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    catch (DbException ex)
                    when (ex.HResult == -2146232060)
                    {
                        // when it's a primary key violation do nothing
                    }

                    // remove the invite from the list
                    invites.TryDequeue(out Invite removedInvite);
                }
            }
        }
        #endregion

        #region Database management
        public static async Task RunSQLAsync(string command)
        {
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                using (SqlTransaction tr = conn.BeginTransaction())
                {
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tr;

                        cmd.CommandText = command;
                        await cmd.ExecuteNonQueryAsync();
                    }
                    tr.Commit();
                }
            }
        }

        public static async Task<DataTable> DatabaseTablesAsync()
        {
            DataTable schemaDataTable = null;
            using (SqlConnection conn = GetSqlConnection())
            {
                await conn.OpenAsync();
                try
                {
                    schemaDataTable = conn.GetSchema("Tables");
                }
                finally
                {
                    conn.Close();
                }
            }
            return schemaDataTable;
        }
        #endregion
    }
}