using System;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using PumpAndDumpBot.Data.Objects;

namespace PumpAndDumpBot.Models
{
    public class AnnouncementTimer : IDisposable
    {
        private DiscordSocketClient _client;
        private Timer _timer;
        private Announcement _announcement;
        public event EventHandler Completed;

        public AnnouncementTimer(DiscordSocketClient client, Announcement announcement)
        {
            _client = client;
            // make a new copy of the object
            _announcement = new Announcement()
            {
                Date = announcement.Date,
                Coin = announcement.Coin,
                Pair = announcement.Pair,
                PairGoal = announcement.PairGoal
            };

            _timer = new Timer(GetTimerInterval());
            _timer.AutoReset = false;
            _timer.Elapsed += TimerTick;
        }

        public async Task StartAsync()
        {
            await _client.SetGameAsync(GetTimerMessage());
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }

        private void TimerTick(object sender, ElapsedEventArgs e)
        {
            Task.Run(() => CheckProgress()).Wait();
        }

        private async Task CheckProgress()
        {
            var diff = GetTimeUntilAnnouncement();
            await _client.SetGameAsync(GetTimerMessage());
            if (diff.TotalSeconds > 0)
            {
                _timer.Interval = GetTimerInterval();
                _timer.Start();
            }
            else
            {
                var embed = GetAnnouncementEmbed(_announcement.Coin, _announcement.Pair, _announcement.PairGoal);
                if (_client.CurrentUser.Id == 400573066134028288) // live bot
                {
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
                }
                else // test bot
                {
                    await (_client.GetChannel(400552471010869248) as SocketTextChannel).SendMessageAsync("", false, embed);
                }
                _timer.Stop();
                Completed?.Invoke(null, EventArgs.Empty);
            }
        }

        private double GetTimerInterval()
        {
            var countdown = GetTimeUntilAnnouncement();
            if (countdown.TotalDays >= 1) return 3600000; // 60 * 60 * 1000 aka once per hour
            if (countdown.TotalHours >= 12) return 1800000; // 15 * 60 * 1000 aka once every 15minutes
            if (countdown.TotalMinutes >= 10) return 60000; // 60 * 1000 aka once per minute
            if (countdown.TotalMinutes >= 5) return 30000; // 30 * 1000 aka once 30 seconds
            return 10000; // 10 * 1000 aka once every 10 seconds
        }

        private TimeSpan GetTimeUntilAnnouncement()
        {
            return _announcement.Date.Subtract(DateTime.Now);
        }

        private string GetTimerMessage()
        {
            var countdown = GetTimeUntilAnnouncement();

            if (countdown.TotalSeconds <= 0) return "Pumping!";
            
            string message = $"Pump in:";
            if (countdown.TotalDays >= 1)
                message += $" {countdown.Days.ToString("00")}d";
            message += $" {countdown.Hours.ToString("00")}:{countdown.Minutes.ToString("00")}:{countdown.Seconds.ToString("00")}";

            return message;
        }

        public static Embed GetAnnouncementEmbed(string coin, string pair, string pairGoal)
        {
            return new EmbedBuilder()
                .AddField("Coin", coin)
                .AddField($"{pair} goal", pairGoal)
                .WithFooter("Trade safe, don't buy higher then the target!").Build();
        }
    }
}