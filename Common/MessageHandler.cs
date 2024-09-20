//代码来源：https://github.com/chi-rei-den/PluginTemplate/blob/master/src/PluginTemplate/Program.cs
//恋恋的TShock插件模板，有改动（为了配合章节名）
//来自棱镜的插件教程

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Terraria;
using Terraria.Map;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using Terraria.ModLoader;


namespace CaiBotMod.Common
{

    public class MessageHandle
    {
        public static async Task SendDateAsync(string message)
        {
            if (Terraria.Program.LaunchParameters.ContainsKey("-caidebug"))
               Console.WriteLine($"[CaiAPI]发送BOT数据包：{message}");
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            await CaiBotMod.ws.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);

        }

        public static bool isWebsocketConnected
        {
            get
            {
                return (CaiBotMod.ws != null && CaiBotMod.ws.State == WebSocketState.Open);
            }
        }

        public static string FileToBase64String(string path)
        {
            FileStream fsForRead = new FileStream(path, FileMode.Open);//文件路径
            string base64Str = "";
            try
            {
                fsForRead.Seek(0, SeekOrigin.Begin);
                byte[] bs = new byte[fsForRead.Length];
                int log = Convert.ToInt32(fsForRead.Length);
                fsForRead.Read(bs, 0, log);
                base64Str = Convert.ToBase64String(bs);
                return base64Str;
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                Console.ReadLine();
                return base64Str;
            }
            finally
            {
                fsForRead.Close();
            }
        }
        public static async Task HandleMessageAsync(string receivedData)
        {
            JObject jsonObject = JObject.Parse(receivedData);

            // 获取 JSON 属性的值
            string type = (string)jsonObject["type"];
            switch (type)
            {
                case "delserver":
                   Console.WriteLine("[CaiAPI]BOT发送解绑命令...");
                    Config.config.Token = "";
                    Config.config.Write();
                    Random rnd = new Random();
                    CaiBotMod.code = rnd.Next(10000000, 99999999);
                    Console.WriteLine($"[CaiBot]您的服务器绑定码为: {CaiBotMod.code}");
                    break;
                case "hello":
                    Console.WriteLine("[CaiAPI]CaiBOT连接成功...");
                    //发送服务器信息
                    var serverInfo = new RestObject
                            {
                                { "type","hello" },
                                { "tshock_version","无TShock"},
                                { "plugin_version","2024.7.1.0"},
                                { "terraria_version",  ModLoader.versionedName},
                                { "cai_whitelist", Config.config.WhiteList},
                                { "os",RuntimeInformation.RuntimeIdentifier },
                                { "world", Main.worldName},
                                { "group" , (long)jsonObject["group"]}
                            };
                    await SendDateAsync(serverInfo.ToJson());
                    break;
                case "groupid":
                    long groupId = (long)jsonObject["groupid"];
                    Console.WriteLine($"[CaiAPI]群号获取成功: {groupId}");
                    if (Config.config.GroupNumber != 0L)
                    {
                        Console.WriteLine($"[CaiAPI]检测到你在配置文件中已设置群号[{Config.config.GroupNumber}],BOT自动获取的群号将被忽略！");
                    }
                    else
                    {
                        Config.config.GroupNumber = groupId;
                    }
                    break;


                case "cmd":
                    string cmd = (string)jsonObject["cmd"];

                    CaiBotPlayer tr = new CaiBotPlayer();
                    Commands.HandleCommand(tr, cmd);
                    RestObject dictionary = new()
                    {
                        { "type", "cmd" },
                        { "result", string.Join('\n', tr.GetCommandOutput()) },
                        { "at" ,(string)jsonObject["at"] },
                        { "group" , (long)jsonObject["group"]}

                    };
                    await SendDateAsync(dictionary.ToJson());
                    break;
                case "online":
                    string result = "";
                    var players = new List<string>();
                    if (Main.player.Where(p => null != p && p.active).Count() == 0)
                    {
                        result = "当前没有玩家在线捏";
                    }
                    else
                    {
                        result += $"在线的玩家({Main.player.Where(p => null != p && p.active).Count()}/{Main.maxNetPlayers})\n";

                        for (int k = 0; k < 255; k++)
                        {
                            if (Main.player[k]!=null&&Main.player[k].active)
                            {
                                players.Add(Main.player[k].name);
                            }
                        }
                        result += string.Join(',', players).TrimEnd(',');
                    }
                    List<string> onlineProcessList = new();

                    #region 进度查询

                    if (!NPC.downedSlimeKing)
                        onlineProcessList.Add("史王");
                    if (!NPC.downedBoss1)
                        onlineProcessList.Add("克眼");
                    if (!NPC.downedBoss2)
                    {
                        if (Main.drunkWorld)
                        {
                            onlineProcessList.Add("世吞/克脑");
                        }
                        else
                        {
                            if (WorldGen.crimson)
                                onlineProcessList.Add("克脑");
                            else
                                onlineProcessList.Add("世吞");
                        }
                    }

                    if (!NPC.downedBoss3)
                        onlineProcessList.Add("骷髅王");
                    if (!Main.hardMode)
                        onlineProcessList.Add("血肉墙");
                    if (!NPC.downedMechBoss2 || !NPC.downedMechBoss1 || !NPC.downedMechBoss3)
                    {
                        if (Main.zenithWorld)
                            onlineProcessList.Add("美杜莎");
                        else
                            onlineProcessList.Add("新三王");
                    }

                    if (!NPC.downedPlantBoss)
                        onlineProcessList.Add("世花");
                    if (!NPC.downedGolemBoss)
                        onlineProcessList.Add("石巨人");
                    if (!NPC.downedAncientCultist)
                        onlineProcessList.Add("拜月教徒");
                    if (!NPC.downedTowers)
                        onlineProcessList.Add("四柱");
                    if (!NPC.downedMoonlord)
                        onlineProcessList.Add("月总");

                    string onlineProcess;
                    if (!onlineProcessList.Any())
                        onlineProcess = "已毕业";
                    else
                        onlineProcess = onlineProcessList.ElementAt(0) + "前";

                    #endregion
                    dictionary = new()
                    {
                        { "type", "online" },
                        { "result", result },
                        { "worldname", Main.worldName},
                        { "process",onlineProcess},
                        { "group" , (long)jsonObject["group"]}
                    };
                    await SendDateAsync(dictionary.ToJson());
                    break;
                case "process":
                    List<Dictionary<string, bool>> processList = new List<Dictionary<string, bool>>(new Dictionary<string, bool>[21]
                    {
                    #region 进度
                        new Dictionary<string, bool> {
                        {
                            "King Slime",
                            NPC.downedSlimeKing
                        } },
                        new Dictionary<string, bool> {
                        {
                            "Eye of Cthulhu",
                            NPC.downedBoss1
                        } },
                        new Dictionary<string, bool> {
                        {
                            "Eater of Worlds / Brain of Cthulhu",
                            NPC.downedBoss2
                        } },
                        new Dictionary<string, bool> {
                        {
                            "Queen Bee",
                            NPC.downedQueenBee
                        } },
                        new Dictionary<string, bool> {
                        {
                            "Skeletron",
                            NPC.downedBoss3
                        } },
                        new Dictionary<string, bool> {
                        {
                            "Deerclops",
                            NPC.downedDeerclops
                        } },
                        new Dictionary<string, bool> {
                        {
                            "Wall of Flesh",
                            Main.hardMode
                        } },
                        new Dictionary<string, bool> {
                        {
                            "Queen Slime",
                            NPC.downedQueenSlime
                        } },
                        new Dictionary<string, bool> {
                        {
                            "The Twins",
                            NPC.downedMechBoss2
                        } },
                        new Dictionary<string, bool> {
                        {
                            "The Destroyer",
                            NPC.downedMechBoss1
                        } },
                        new Dictionary<string, bool> {
                        {
                            "Skeletron Prime",
                            NPC.downedMechBoss3
                        } },
                        new Dictionary<string, bool> {
                        {
                            "Plantera",
                            NPC.downedPlantBoss
                        } },
                        new Dictionary<string, bool> {
                        {
                            "Golem",
                            NPC.downedGolemBoss
                        } },
                        new Dictionary<string, bool> {
                        {
                            "Duke Fishron",
                            NPC.downedFishron
                        } },
                        new Dictionary<string, bool> {
                        {
                            "Empress of Light",
                            NPC.downedEmpressOfLight
                        } },
                        new Dictionary<string, bool> {
                        {
                            "Lunatic Cultist",
                            NPC.downedAncientCultist
                        } },
                        new Dictionary<string, bool> {
                        {
                            "Moon Lord",
                            NPC.downedMoonlord
                        } },
                        new Dictionary<string, bool> {
                        {
                            "Solar Pillar",
                            NPC.downedTowerSolar
                        } },
                        new Dictionary<string, bool> {
                        {
                            "Nebula Pillar",
                            NPC.downedTowerNebula
                        } },
                        new Dictionary<string, bool> {
                        {
                            "Vortex Pillar",
                            NPC.downedTowerVortex
                        } },
                        new Dictionary<string, bool> {
                        {
                            "Stardust Pillar",
                            NPC.downedTowerStardust
                        } }
                    });
                    #endregion
                    var re = new RestObject
                    {
                        { "type","process" },
                        { "result",processList },
                        { "worldname",Main.worldName},
                        { "group" , (long)jsonObject["group"]}
                    };
                    await SendDateAsync(re.ToJson());
                    break;
                case "whitelist":
                    string name = (string)jsonObject["name"];
                    int code = (int)jsonObject["code"];
                    List<string> uuids = jsonObject["uuids"].ToObject<List<string>>();
                    if (await Login.CheckWhiteAsync(name, code, uuids))
                    {
                        List<TSPlayer> playerList = TSPlayer.FindByNameOrID("tsn:" + name);
                        if (playerList.Count == 0)
                        {
                            return;
                        }
                        Packet.Login[playerList[0].Index] = true;
                    }
                    break;
                case "selfkick":
                    name = (string)jsonObject["name"];
                    List<TSPlayer> playerList2 = TSPlayer.FindByNameOrID("tsn:" + name);
                    if (playerList2.Count == 0)
                    {
                        return;
                    }
                    playerList2[0].Kick("在群中使用自踢命令.");

                    break;
                case "mappng":
                    Image<Rgba32> image = new Image<Rgba32>(Main.maxTilesX, Main.maxTilesY);

                    MapHelper.Initialize();
                    
                    Main.Map = new WorldMap(Main.maxTilesX, Main.maxTilesY);
                    for (var x = 0; x < Main.maxTilesX; x++)
                    {
                        for (var y = 0; y < Main.maxTilesY; y++)
                        {
                            try
                            {
                                //Console.WriteLine($"{x}, {y}");
                                var tile = MapHelper.CreateMapTile(x, y, byte.MaxValue);
                                var col = MapHelper.GetMapTileXnaColor(ref tile);
                                image[x, y] = new Rgba32(col.R, col.G, col.B, col.A);
                                //Console.WriteLine($"{x}, {y} - {col.R}, {col.G}, {col.B}, {col.A}");
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                    }
                    image.Save("map.png");
                    string base64 = FileToBase64String("map.png");
                    re = new RestObject
                    {
                        { "type","mappng" },
                        { "result",base64 }
                    };
                    await SendDateAsync(re.ToJson());
                    break;
                case "lookbag":
                    name = (string)jsonObject["name"];
                    List<TSPlayer> playerList3 = TSPlayer.FindByNameOrID("tsn:" + name);
                    List<int> buffs = new();
                    if (playerList3.Count != 0)
                    {
                        
                        // 在线
                        Player plr = playerList3[0].TPlayer;
                        buffs = plr.buffType.ToList();
                        NetItem[] invs = new NetItem[NetItem.MaxInventory];
                        Item[] inventory = plr.inventory;
                        Item[] armor = plr.armor;
                        Item[] dye = plr.dye;
                        Item[] miscEqups = plr.miscEquips;
                        Item[] miscDyes = plr.miscDyes;
                        Item[] piggy = plr.bank.item;
                        Item[] safe = plr.bank2.item;
                        Item[] forge = plr.bank3.item;
                        Item[] voidVault = plr.bank4.item;
                        Item trash = plr.trashItem;
                        Item[] loadout1Armor = plr.Loadouts[0].Armor;
                        Item[] loadout1Dye = plr.Loadouts[0].Dye;
                        Item[] loadout2Armor = plr.Loadouts[1].Armor;
                        Item[] loadout2Dye = plr.Loadouts[1].Dye;
                        Item[] loadout3Armor = plr.Loadouts[2].Armor;
                        Item[] loadout3Dye = plr.Loadouts[2].Dye;
                        for (int i = 0; i < NetItem.MaxInventory; i++)
                        {
                            if (i < NetItem.InventoryIndex.Item2)
                            {
                                //0-58
                                invs[i] = (NetItem)inventory[i];
                            }
                            else if (i < NetItem.ArmorIndex.Item2)
                            {
                                //59-78
                                var index = i - NetItem.ArmorIndex.Item1;
                                invs[i] = (NetItem)armor[index];
                            }
                            else if (i < NetItem.DyeIndex.Item2)
                            {
                                //79-88
                                var index = i - NetItem.DyeIndex.Item1;
                                invs[i] = (NetItem)dye[index];
                            }
                            else if (i < NetItem.MiscEquipIndex.Item2)
                            {
                                //89-93
                                var index = i - NetItem.MiscEquipIndex.Item1;
                                invs[i] = (NetItem)miscEqups[index];
                            }
                            else if (i < NetItem.MiscDyeIndex.Item2)
                            {
                                //93-98
                                var index = i - NetItem.MiscDyeIndex.Item1;
                                invs[i] = (NetItem)miscDyes[index];
                            }
                            else if (i < NetItem.PiggyIndex.Item2)
                            {
                                //98-138
                                var index = i - NetItem.PiggyIndex.Item1;
                                invs[i] = (NetItem)piggy[index];
                            }
                            else if (i < NetItem.SafeIndex.Item2)
                            {
                                //138-178
                                var index = i - NetItem.SafeIndex.Item1;
                                invs[i] = (NetItem)safe[index];
                            }
                            else if (i < NetItem.TrashIndex.Item2)
                            {
                                //179-219
                                invs[i] = (NetItem)trash;
                            }
                            else if (i < NetItem.ForgeIndex.Item2)
                            {
                                //220
                                var index = i - NetItem.ForgeIndex.Item1;
                                invs[i] = (NetItem)forge[index];
                            }
                            else if (i < NetItem.VoidIndex.Item2)
                            {
                                //220
                                var index = i - NetItem.VoidIndex.Item1;
                                invs[i] = (NetItem)voidVault[index];
                            }
                            else if (i < NetItem.Loadout1Armor.Item2)
                            {
                                var index = i - NetItem.Loadout1Armor.Item1;
                                invs[i] = (NetItem)loadout1Armor[index];
                            }
                            else if (i < NetItem.Loadout1Dye.Item2)
                            {
                                var index = i - NetItem.Loadout1Dye.Item1;
                                invs[i] = (NetItem)loadout1Dye[index];
                            }
                            else if (i < NetItem.Loadout2Armor.Item2)
                            {
                                var index = i - NetItem.Loadout2Armor.Item1;
                                invs[i] = (NetItem)loadout2Armor[index];
                            }
                            else if (i < NetItem.Loadout2Dye.Item2)
                            {
                                var index = i - NetItem.Loadout2Dye.Item1;
                                invs[i] = (NetItem)loadout2Dye[index];
                            }
                            else if (i < NetItem.Loadout3Armor.Item2)
                            {
                                var index = i - NetItem.Loadout3Armor.Item1;
                                invs[i] = (NetItem)loadout3Armor[index];
                            }
                            else if (i < NetItem.Loadout3Dye.Item2)
                            {
                                var index = i - NetItem.Loadout3Dye.Item1;
                                invs[i] = (NetItem)loadout3Dye[index];
                            }
                        }
                        var itemList = new List<List<int>>();
                        foreach (var i in invs)
                        {
                            itemList.Add(new List<int>() { i.NetId, i.Stack });
                        }
                        re = new RestObject
                        {
                            { "type","lookbag" },
                            { "name",name},
                            { "exist",1},
                            { "inventory", itemList},
                            { "buffs", buffs},
                            { "group" , (long)jsonObject["group"]}
                        };
                        await SendDateAsync(re.ToJson());
                        return;
                    }
                    else
                    {
                        re = new RestObject
                        {
                            { "type","lookbag" },
                            { "exist",0},
                            { "group" , (long)jsonObject["group"]}
                        };
                        await SendDateAsync(re.ToJson());

                    }
                    break;

            }

        }
    }
}
