using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Binance.Net;
using Discord;

namespace MarketPriceGap
{
    // ReSharper disable once ClassNeverInstantiated.Global
    class Program
    {
        static BinanceClient Client = new BinanceClient();

        static void Main(string[] args)
        {
            var webhook = Regex.Split(File.ReadAllText(Environment.CurrentDirectory + "\\webhook.txt"), "/");
            var webhookId = ulong.Parse(webhook[0]);
            var token = webhook[1];
            var discord = new Discord(webhookId, token);

            var interval = int.Parse(File.ReadAllText(Environment.CurrentDirectory + "\\interval.txt"));
            var pairs = Regex.Split(File.ReadAllText(Environment.CurrentDirectory + "\\pair.txt"), "\r\n");
            // ReSharper disable once IdentifierTypo
            Dictionary<string, int> gaplist = new Dictionary<string, int>();

            while (true)
            {
                Thread.Sleep(interval);

                foreach (var pair in pairs)
                {
                    var market = GetMarketPrice(pair);
                    var price = GetPrice(pair);
                    var gapPercent = GetPrcentAbs(market, price);
                    //Console.WriteLine($"Pair: {pair} \r\nPriceGap: {gapPercent:##.##}");

                    if (gapPercent >= 2m)
                    {
                        if (!gaplist.ContainsKey(pair))
                        {
                            gaplist.Add(pair, 0);
                        }
                        else
                        {
                            gaplist[pair]++;
                            if (gaplist[pair] > 2)
                            {
                                Console.WriteLine($"Pair: {pair} \r\nPriceGap: {gapPercent:##.##}");
                                discord.Send(pair, $"Market price: {price}\r\nPrice: {price}\r\nGap: {gapPercent:##.##}%", Color.Gold);
                            }
                        }
                    }
                    else
                    {
                        if (gaplist.ContainsKey(pair))
                        {
                            gaplist.Remove(pair);
                        }
                    }
                }

                GC.Collect();
            }
        }

        static decimal GetMarketPrice(string pair)
        {
            return Client.FuturesUsdt.Market.GetMarkPrices(pair).Data.Last().MarkPrice;
        }

        static decimal GetPrice(string pair)
        {
            return Client.FuturesUsdt.Market.GetPrice(pair).Data.Price;
        }

        static decimal GetPrcentAbs(decimal value1, decimal value2)
        {
            var percentage = value1 / value2 * 100 - 100;
            return percentage < 0 ? percentage * -1 : percentage;
        }
    }
}
