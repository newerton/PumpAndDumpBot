using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Discord.Commands;
using PumpAndDumpBot.Data;

namespace RLBot.Modules
{
    [Name("Database")]
    [RequireOwner]
    public class DatabaseModule : ModuleBase<SocketCommandContext>
    {
        [Command("sql", RunMode = RunMode.Async)]
        [Summary("Run an sql command against the database that does not return a result. (insert, update, delete)")]
        [Remarks("sql <sql command>")]
        public async Task RunSQLCommand([Remainder]string command)
        {
            command = command.Trim();
            if (command.Equals(""))
            {
                await ReplyAsync("No SQL-command provided!");
                return;
            }

            try
            {
                await Database.RunSQLAsync(command);
                await ReplyAsync("SQL-command executed.");
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.ToString());
            }
        }

        [Command("tables", RunMode = RunMode.Async)]
        [Summary("Show a list of all the tables in the database")]
        [Remarks("tables")]
        public async Task TablesAsync()
        {
            try
            {
                var schema = await Database.DatabaseTablesAsync();
                string colums = "";
                foreach (DataColumn column in schema.Columns)
                {
                    colums += column.ColumnName + "\t";
                }
                await ReplyAsync(colums);
                foreach (DataRow row in schema.Rows)
                {
                    string rows = "";
                    foreach (object value in row.ItemArray)
                    {
                        rows += value.ToString() + "\t";
                    }
                    await ReplyAsync(rows);
                }
                await ReplyAsync("-----done-----");
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.ToString());
            }
        }

        [Command("select", RunMode = RunMode.Async)]
        [Summary("Run a select command against the database")]
        [Remarks("select <rest of the select command>")]
        public async Task SelectAsync([Remainder]string command)
        {
            command = command.Trim();
            try
            {
                using (SqlConnection conn = Database.GetSqlConnection())
                {
                    await conn.OpenAsync();
                    try
                    {
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "select " + command;
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                bool showTypes = true;
                                string fieldtypes = "";
                                while (await reader.ReadAsync())
                                {
                                    string row = "";
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        if (showTypes)
                                            fieldtypes += reader.GetFieldType(i).ToString() + " ";
                                        row += reader.GetValue(i) + " ";
                                    }
                                    if (showTypes)
                                    {
                                        await ReplyAsync(fieldtypes);
                                        showTypes = false;
                                    }
                                    await ReplyAsync(row);
                                }
                            }
                        }
                        await ReplyAsync("-----done-----");
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.ToString());
            }
        }
    }
}