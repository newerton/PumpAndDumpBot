using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using PumpAndDumpBot.Attributes;
using PumpAndDumpBot.Data;
using PumpAndDumpBot.Data.Objects;
using PumpAndDumpBot.Models;

namespace PumpAndDumpBot.Modules
{
    [Group("announcement")]
    [RequiredChannel(401021271556620288, 400552471010869248)]
    public class AnnouncementModule : InteractiveBase<SocketCommandContext>
    {
        private static AnnouncementTimer _announcementTimer;

        public static async Task InitializeAsync(DiscordSocketClient client)
        {
            try
            {
                var announcement = await Database.GetAnnouncementAsync();
                if (announcement != null)
                {
                    if (announcement.Date <= DateTime.Now)
                        await Database.DeleteAnnouncementAsync();
                    else
                    {
                        _announcementTimer = new AnnouncementTimer(client, announcement);
                        await _announcementTimer.StartAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, "InitializeAsync", ex.Message, ex));
            }
        }

        private static void AnnouncementCompleted(object sender, EventArgs e)
        {
            Task.Run(() => DeleteAsync()).Wait();
        }

        private static async Task DeleteAsync()
        {
            await Database.DeleteAnnouncementAsync();
            _announcementTimer = null;
        }

        [Command("create", RunMode = RunMode.Async)]
        [Summary("Create a new announcement.")]
        [RequireContext(ContextType.Guild)]
        public async Task CreateAsync()
        {
            try
            {
                if (_announcementTimer != null)
                {
                    await ReplyAsync($"{Context.Message.Author.Mention}, there is already a scheduled announcement.");
                    return;
                }

                // coin question
                SocketMessage msg = null;
                await ReplyAsync("What coin to announce?");
                msg = await NextMessageAsync(timeout: new TimeSpan(0, 0, 30));
                if (msg == null)
                {
                    await ReplyAsync("Message timed out..");
                    return;
                }
                string coin = msg.Content;

                // pair question
                string pair = "";
                await ReplyAsync("Use BTC or ETH?");
                while (pair != "BTC" && pair != "ETH")
                {
                    msg = await NextMessageAsync(timeout: new TimeSpan(0, 0, 30));
                    if (msg == null)
                    {
                        await ReplyAsync("Message timed out..");
                        return;
                    }

                    pair = msg.Content.Trim().ToUpper();
                    if (pair != "BTC" && pair != "ETH")
                    {
                        await ReplyAsync("Invalid input.\nUse BTC or ETH?");
                    }
                }

                // pair goal question
                string pairGoal = "";
                await ReplyAsync($"{pair} goal?");
                while (pairGoal == "")
                {

                    msg = await NextMessageAsync(timeout: new TimeSpan(0, 0, 30));
                    if (msg == null)
                    {
                        await ReplyAsync("Message timed out..");
                        return;
                    }

                    if (decimal.TryParse(msg.Content, NumberStyles.Currency, CultureInfo.InvariantCulture, out decimal pairGoalTemp) && pairGoalTemp > 0)
                    {
                        pairGoal = msg.Content;
                    }
                    else
                    {
                        await ReplyAsync($"Invalid input.\n{pair} goal?");
                    }
                }

                // delay questions
                int days = -1;
                while (days < 0)
                {
                    await ReplyAsync("How many days delay?");
                    msg = await NextMessageAsync(timeout: new TimeSpan(0, 0, 30));
                    if (msg == null)
                    {
                        await ReplyAsync("Message timed out..");
                        return;
                    }

                    if (int.TryParse(msg.Content, out int daysTemp) && daysTemp >= 0)
                    {
                        days = daysTemp;
                    }
                    else
                    {
                        await ReplyAsync("Invalid input, try again.");
                    }
                }

                int hours = -1;
                await ReplyAsync("How many hours delay?");
                while (hours < 0)
                {
                    msg = await NextMessageAsync(timeout: new TimeSpan(0, 0, 30));
                    if (msg == null)
                    {
                        await ReplyAsync("Message timed out..");
                        return;
                    }

                    if (int.TryParse(msg.Content, out int hoursTemp) && hoursTemp >= 0 && hoursTemp < 24)
                    {
                        hours = hoursTemp;
                    }
                    else
                    {
                        await ReplyAsync("Invalid input, the value must be in the range [0-23].\nHow many hours delay?");
                    }
                }

                int minutes = -1;
                await ReplyAsync("How many minutes delay?");
                while (minutes < 0)
                {
                    msg = await NextMessageAsync(timeout: new TimeSpan(0, 0, 30));
                    if (msg == null)
                    {
                        await ReplyAsync("Message timed out..");
                        return;
                    }

                    if (int.TryParse(msg.Content, out int minutesTemp) && minutesTemp >= 0 && minutesTemp < 60)
                    {
                        minutes = minutesTemp;
                    }
                    else
                    {
                        await ReplyAsync("Invalid input, the value must be in the range [0-59].\nHow many minutes delay?");
                    }
                }

                if (days == 0 && hours == 0 && minutes == 0)
                {
                    await ReplyAsync($"Announcement creation failed, there must be a delay.");
                    return;
                }

                // preview and confirmation
                await ReplyAsync($"Are the following details correct, answer with **yes** if they are?\nPump in: {days}d {hours}h {minutes}m", false, AnnouncementTimer.GetAnnouncementEmbed(coin, pair, pairGoal));
                msg = await NextMessageAsync(timeout: new TimeSpan(0, 0, 30));
                if (msg == null)
                {
                    await ReplyAsync("Message timed out..");
                    return;
                }

                if (msg.Content.ToUpper() != "YES" && msg.Content.ToUpper() != "Y")
                {
                    await ReplyAsync("Announcement creation cancelled.");
                    return;
                }

                Announcement announcement = new Announcement()
                {
                    Date = DateTime.Now.Add(new TimeSpan(days, hours, minutes, 0)),
                    Coin = coin,
                    Pair = pair,
                    PairGoal = pairGoal
                };

                await Database.InsertAnnouncementAsync(announcement);
                _announcementTimer = new AnnouncementTimer(Context.Client, announcement);
                _announcementTimer.Completed += AnnouncementCompleted;
                await _announcementTimer.StartAsync();

                await ReplyAsync("Scheduled a new announcement.");
            }
            catch (SqlException ex)
            when (ex.Number == 2627) // unique key constraint
            {
                await ReplyAsync("Announcement creation cancelled, there is already one active.");
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        [Command("remove")]
        [Summary("Remove the active announcement.")]
        [RequireContext(ContextType.Guild)]
        public async Task RemoveAsync()
        {
            if (_announcementTimer == null)
            {
                await ReplyAsync("There is currently no scheduled announcement.");
                return;
            }

            await Database.DeleteAnnouncementAsync();
            _announcementTimer.Stop();
            await DeleteAsync();
            await ReplyAsync("Removed the active announcement.");

            await Task.Delay(10000);
            await Context.Client.SetGameAsync(ConfigurationManager.AppSettings["GAME"]);
        }
    }
}