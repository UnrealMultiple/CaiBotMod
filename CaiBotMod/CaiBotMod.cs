using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CaiBotMod.Common;
using Newtonsoft.Json.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Config = CaiBotMod.Common.Config;

namespace CaiBotMod
{

    public class CaiBotMod : Mod
    {

        public static int ExampleCustomCurrencyId;
        public static List<TSPlayer> Players
        {
            get
            {
                var players = new List<TSPlayer>();
                var indexs = Netplay.Clients.Where(p => null != p).Select(p => p.Id);
                foreach (var index in indexs)
                {
                    players.Add(new TSPlayer(index));
                }
                return players;
            }
        }
        public static int code = -1;
        public static bool showCode = false;
        public static ClientWebSocket ws = new ClientWebSocket();
        public static Dictionary<string, Point> PlayerDeath = new Dictionary<string, Point>();
        public override void Load()
        {
            if (!Main.dedServ)
            {
                return;
            }
            GenCode();
            Commands.ChatCommands.Add(new Command(TpNpc, "tpnpc", "tpn"));
            Commands.ChatCommands.Add(new Command(Home, "home", "spawn"));
            Commands.ChatCommands.Add(new Command(Who, "who", "online"));
            Commands.ChatCommands.Add(new Command(Back, "back", "b"));
            Commands.ChatCommands.Add(new Command(Help, "help"));
            Config.Read();
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        while (Main.worldName == "")
                        {
                            await Task.Delay(TimeSpan.FromSeconds(1));
                        }
                        while (Config.config.Token == "")
                        {
                            await Task.Delay(TimeSpan.FromSeconds(10));
                            HttpClient client = new();
                            HttpResponseMessage? response;
                            client.Timeout = TimeSpan.FromSeconds(5.0);
                            response = client.GetAsync($"http://api.terraria.ink:22334/bot/get_token?" +
                                                       $"code={code}")
                                .Result;
                            //TShock.Log.ConsoleInfo($"[CaiAPI]尝试被动绑定,状态码:{response.StatusCode}");
                            if (response.StatusCode == HttpStatusCode.OK && Config.config.Token == "")
                            {
                                string responseBody = await response.Content.ReadAsStringAsync();
                                JObject json = JObject.Parse(responseBody);
                                string token = json["token"]!.ToString();
                                Config.config.Token = token;
                                Config.config.Write();
                                Console.WriteLine($"[CaiAPI]被动绑定成功!");
                            }
                        
                        }
                        ws = new ClientWebSocket();
                        while (Config.config.Token == "")
                        {
                            await Task.Delay(TimeSpan.FromSeconds(5));
                            if (Netplay.TcpListener != null&&!showCode)
                            {
                                showCode = true;
                                Console.WriteLine($"[CaiBot]您的服务器绑定码为: {code}");
                            }
                                
                        }
                        if (Terraria.Program.LaunchParameters.ContainsKey("-cailocaldebug"))
                            await ws.ConnectAsync(new Uri("ws://127.0.0.1:22334/bot/" + Config.config.Token), CancellationToken.None);
                        else
                            await ws.ConnectAsync(new Uri("ws://api.terraria.ink:22334/bot/" + Config.config.Token), CancellationToken.None);
                        
                        while (true)
                        {
                            byte[] buffer = new byte[1024];
                            WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                            string receivedData = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            if (Terraria.Program.LaunchParameters.ContainsKey("-caidebug"))
                               Console.WriteLine($"[CaiAPI]收到BOT数据包: {receivedData}");
                            MessageHandle.HandleMessageAsync(receivedData);

                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CaiAPI]CaiBot断开连接...");
                        if (Terraria.Program.LaunchParameters.ContainsKey("-caidebug"))
                            Console.WriteLine(ex.ToString());
                        else
                            Console.WriteLine("链接失败原因: " + ex.Message);
                    }
                    
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }

            });
        }

        private static void Help(CommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                args.Player.SendErrorMessage("无效格式.正确格式: /help <命令/页码>");
                return;
            }

            if (args.Parameters.Count == 0 || int.TryParse(args.Parameters[0], out int pageNumber))
            {
                if (!PaginationTools.TryParsePageNumber(args.Parameters, 0, args.Player, out pageNumber))
                {
                    return;
                }

                IEnumerable<string> cmdNames = from cmd in Commands.ChatCommands
                                               where cmd.CanRun(args.Player) && (cmd.Name != "setup")
                                               select "/" + cmd.Name;

                PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(cmdNames),
                    new PaginationTools.Settings
                    {
                        HeaderFormat = "命令列表:",
                        FooterFormat = "输入/help {{0}}翻页."
                    });
            }
            else
            {
                string commandName = args.Parameters[0].ToLower();
                if (commandName.StartsWith('/'))
                {
                    commandName = commandName.Substring(1);
                }

                Command? command = Commands.ChatCommands.Find(c => c.Names.Contains(commandName));
                if (command == null)
                {
                    args.Player.SendErrorMessage("无效命令.");
                    return;
                }

                args.Player.SendSuccessMessage("/{0}的帮助清单: ", command.Name);
                if (command.HelpDesc == null)
                {
                    args.Player.SendInfoMessage(command.HelpText);
                    return;
                }
                foreach (string line in command.HelpDesc)
                {
                    args.Player.SendInfoMessage(line);
                }
            }
        }


        public static void GenCode()
        {
            if (Config.config.Token != "")
            {
                return;
            }
            Random rnd = new Random();
            code = rnd.Next(10000000, 99999999);
            Console.WriteLine($"[CaiBot]您的服务器绑定码为: {code}");
        }
        private void Back(CommandArgs args)
        {
            if (args.Player.Dead)
            {
                args.Player.SendErrorMessage("[i:3457]你还没复活呢,不能回到出生点！");
                return;
            }
            if (!PlayerDeath.TryGetValue(args.Player.Name, out Point value)) 
            {
                args.Player.SendErrorMessage("[i:3457]你还没有去世过呢！");
                return;
            }
            args.Player.Teleport(value.X, value.Y);
            args.Player.SendSuccessMessage("[i:3457]你已经回到上一次死亡点啦！");

        }

        private void Who(CommandArgs args)
        {
            string result = "";
            var players = new List<string>();
            List<string> strings = new();
            strings.Add($"[i:603]在线玩家 ({Main.player.Where(p => null != p && p.active).Count()}/{Main.maxNetPlayers})");
            for (int k = 0; k < 255; k++)
            {
                if (Main.player[k].active)
                {
                    result += Main.player[k].name + ",";
                }
            }
            strings.Add($"{result.TrimEnd(',')}");
            args.Player.SendSuccessMessage(string.Join("\n", strings));

        }

        private static void Home(CommandArgs args)
        {
            if (args.Player.Dead)
            {
                args.Player.SendErrorMessage("[i:50]你死了,不能回到出生点!");
                return;
            }
            args.Player.Teleport(Main.spawnTileX*16, Main.spawnTileY*16 -3);
            args.Player.SendSuccessMessage("[i:50]已经将你传送到世界出生点~");
        }
        private void TpNpc(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("[i:267]无效用法.正确用法: /tpnpc <NPC>.");
                return;
            }

            var npcStr = string.Join(" ", args.Parameters);
            var matches = new List<NPC>();
            bool isIndex = false;
            int npcId=0;
            try
            {
                 npcId = int.Parse(npcStr);
                if (npcId>=0&& Main.npc[npcId]!=null)
                {
                    isIndex = true;
                }
            }
            catch
            {

            }
            if (isIndex) 
            {
                var target2 = Main.npc[npcId];
                args.Player.Teleport(target2.position.X, target2.position.Y);
                args.Player.SendSuccessMessage("[i:267]已传送到'{0}'附近.", target2.FullName);
                return;
            }
            foreach (var npc in Main.npc.Where(npc => npc.active))
            {
                
                if (string.Equals(npc.FullName, npcStr, StringComparison.InvariantCultureIgnoreCase))
                {
                    matches = new List<NPC> { npc };
                    break;
                }
                if (npc.FullName.StartsWith(npcStr, StringComparison.InvariantCultureIgnoreCase))
                    matches.Add(npc);
            }

            if (matches.Count > 1)
            {
                args.Player.SendMultipleMatchError(matches.Select(n => $"{Lang.GetNPCNameValue(n.netID)}({n.whoAmI})"));
                return;
            }
            if (matches.Count == 0)
            {
                args.Player.SendErrorMessage("[i:267]没有找到指定的NPC.");
                return;
            }

            var target = matches[0];
            args.Player.Teleport(target.position.X, target.position.Y);
            args.Player.SendSuccessMessage("[i:267]已传送到'{0}'附近.", target.FullName);
        }

        public static void Kick(int Index, string reason)
        {
            NetMessage.SendData(MessageID.Kick, Index, -1, Terraria.Localization.NetworkText.FromLiteral(reason));
        }
        public override void Unload()
        {
        }
    }
}


