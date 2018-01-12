using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using PumpAndDumpBot.Data.Objects;

namespace PumpAndDumpBot.Modules
{
    public class AnnouncementModule : InteractiveBase<SocketCommandContext>
    {
        private readonly DiscordSocketClient _client;
        private static Timer announcementTimer;
        private static volatile bool _continueTimer = true;
        private static Announcement announcement;

        public AnnouncementModule(DiscordSocketClient client)
        {
            _client = client;
            Task.Run(() => Initialize()).Wait();
        }

        public async Task Initialize()
        {
            /*announcement = await Database.GetActiveAnnouncementAsync();
            if (diff )
            var diff = announcement.Date.Subtract(DateTime.Now);*/
            if (announcementTimer == null)
            {
                announcementTimer = new Timer(10000);
                announcementTimer.AutoReset = false;
                announcementTimer.Elapsed += AnnouncementTimer_Elapsed;
            }
        }

        private void AnnouncementTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Task.Run(() => AnnouncementCheck()).Wait();
        }

        private async Task AnnouncementCheck()
        {
            var diff = announcement.Date.Subtract(DateTime.Now);

            if (_continueTimer)
            {
                if (diff.TotalSeconds > 0)
                {
                    announcementTimer.Start();
                    await _client.SetGameAsync($"Pump in: {diff.Days}d {diff.Hours}:{diff.Minutes}:{diff.Seconds}");
                }
                else
                {
                    _continueTimer = false;
                    await _client.SetGameAsync($"Pumping!");

                    var embed = new EmbedBuilder()
                    .AddField("Coin", announcement.Coin)
                    .AddField("Btc goal", announcement.Btc, true)
                    .AddField("Eth goal", announcement.Eth, true)
                    .WithFooter("Trade safe, don't buy higher then the target!").Build();
                    await (_client.GetChannel(400461176921915402) as SocketTextChannel).SendMessageAsync("", false, embed);
                    await Task.Delay(3000);
                    await (_client.GetChannel(400461161998450688) as SocketTextChannel).SendMessageAsync("", false, embed);
                    await Task.Delay(3000);
                    await (_client.GetChannel(400461138040717314) as SocketTextChannel).SendMessageAsync("", false, embed);
                    await Task.Delay(4000);
                    await (_client.GetChannel(400461122718793728) as SocketTextChannel).SendMessageAsync("", false, embed);
                    await Task.Delay(2000);
                    await (_client.GetChannel(400461103538241546) as SocketTextChannel).SendMessageAsync("", false, embed);
                    await Task.Delay(2000);
                    await (_client.GetChannel(400461084160557057) as SocketTextChannel).SendMessageAsync("", false, embed);
                    await Task.Delay(1000);
                    await (_client.GetChannel(400461051642118145) as SocketTextChannel).SendMessageAsync("", false, embed);
                    await StopAsync();
                }
            }
        }

        [Command("announce", RunMode = RunMode.Async)]
        [Summary("Manage the announcements")]
        public async Task AnnounceAsync()
        {
            //var announcement = await Database.GetActiveAnnouncementAsync();
            SocketMessage msg = null;
            await ReplyAsync("What coin to announce?");
            msg = await NextMessageAsync(timeout: new TimeSpan(0, 0, 30));
            if (msg == null)
            {
                await ReplyAsync("Message timed out..");
                return;
            }

            string announcementMessage = msg.Content;

            string btc = "";
            await ReplyAsync("Btc goal?");
            while (btc == "")
            {
                
                msg = await NextMessageAsync(timeout: new TimeSpan(0, 0, 30));
                if (msg == null)
                {
                    await ReplyAsync("Message timed out..");
                    return;
                }

                if (double.TryParse(msg.Content, out double btcTemp) && btcTemp > 0)
                {
                    btc = msg.Content;
                }
                else
                {
                    await ReplyAsync("Invalid input.\nBtc goal?");
                }
            }

            string eth = "";
            await ReplyAsync("Eth goal?");
            while (eth == "")
            {

                msg = await NextMessageAsync(timeout: new TimeSpan(0, 0, 30));
                if (msg == null)
                {
                    await ReplyAsync("Message timed out..");
                    return;
                }

                if (double.TryParse(msg.Content, out double ethTemp) && ethTemp > 0)
                {
                    eth = msg.Content;
                }
                else
                {
                    await ReplyAsync("Invalid input.\nEth goal?");
                }
            }

            int days = -1;
            while (days < 0)
            {
                await ReplyAsync("How many days delay?");
                msg = await NextMessageAsync(timeout: new TimeSpan(0, 0, 10));
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
                msg = await NextMessageAsync(timeout: new TimeSpan(0, 0, 10));
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
                msg = await NextMessageAsync(timeout: new TimeSpan(0, 0, 10));
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

            if (days != 0 || hours != 0 || minutes != 0)
                await ReplyAsync($"Announcement will be shown in: {days}d {hours}h {minutes}m");
            else
                await ReplyAsync($"Announcement creation failed, there must be a delay.");

            TimeSpan delay = new TimeSpan(days, hours, minutes, 0);

            announcement = new Announcement()
            {
                Date = DateTime.Now.Add(delay),
                Coin = announcementMessage,
                Btc = btc,
                Eth = eth
            };

            await _client.SetGameAsync($"Pump in: {days}d {hours}:{minutes}:0");
            await StartAsync();
        }

        public async Task StartAsync()
        {
            _continueTimer = true;
            announcementTimer.Start();
        }

        [Command("stoptimer")]
        public async Task StopAsync()
        {
            _continueTimer = false;
            announcementTimer.Stop();
            await Task.Delay(10000);
            await _client.SetGameAsync(ConfigurationManager.AppSettings["GAME"]);
        }
    }
}
