using Ionic.Zlib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace CaiBotMod.Common;

public static class MessageHandle
{
    public static bool IsWebsocketConnected =>
        CaiBotMod.WebSocket.State == WebSocketState.Open;

    public static async Task SendDateAsync(string message)
    {
        if (Program.LaunchParameters.ContainsKey("-caidebug"))
        {
            Console.WriteLine($"[CaiAPI]发送BOT数据包：{message}");
        }

        var messageBytes = Encoding.UTF8.GetBytes(message);
        await CaiBotMod.WebSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true,
            CancellationToken.None);
    }

    private static string FileToBase64String(string path)
    {
        FileStream fsForRead = new (path, FileMode.Open); //文件路径
        var base64Str = "";
        try
        {
            fsForRead.Seek(0, SeekOrigin.Begin);
            var bs = new byte[fsForRead.Length];
            var log = Convert.ToInt32(fsForRead.Length);
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

    public static string CompressBase64(string base64String)
    {
        var base64Bytes = Encoding.UTF8.GetBytes(base64String);
        using (MemoryStream outputStream = new ())
        {
            using (GZipStream gzipStream = new (outputStream, CompressionMode.Compress))
            {
                gzipStream.Write(base64Bytes, 0, base64Bytes.Length);
            }

            return Convert.ToBase64String(outputStream.ToArray());
        }
    }

    public static async Task HandleMessageAsync(string receivedData)
    {
        var jsonObject = JObject.Parse(receivedData);
        var type = (string) jsonObject["type"]!;
        RestObject result;
        switch (type)
        {
            case "delserver":
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[CaiAPI]BOT发送解绑命令...");
                Console.ResetColor();
                Config.config.Token = "";
                Config.config.Write();
                CaiBotMod.GenCode();
                break;
            case "hello":
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[CaiAPI]CaiBOT连接成功...");
                Console.ResetColor();
                //发送服务器信息
                result = new RestObject
                {
                    { "type", "hello" },
                    { "tshock_version", "TModLoader" },
                    { "plugin_version", CaiBotMod.PluginVersion! },
                    { "terraria_version", ModLoader.versionedName },
                    { "cai_whitelist", Config.config.WhiteList },
                    { "os", RuntimeInformation.RuntimeIdentifier },
                    { "world", Main.worldName },
                    { "group", (long) jsonObject["group"]! }
                };
                await SendDateAsync(JsonConvert.SerializeObject(result));
                break;
            case "groupid":
                var groupId = (long) jsonObject["groupid"]!;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("[CaiAPI]群号获取成功: ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(groupId);
                Console.ResetColor();
                if (Config.config.GroupNumber != 0L)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("[CaiAPI]检测到你在配置文件中已设置群号: ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(Config.config.GroupNumber);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(",BOT自动获取的群号将被忽略！");
                    Console.ResetColor();
                }
                else
                {
                    Config.config.GroupNumber = groupId;
                }

                break;
            case "cmd":
                var cmd = (string) jsonObject["cmd"]!;
                CaiBotPlayer tr = new ();
                Commands.HandleCommand(tr, cmd);
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"[CaiBot] `{(string) jsonObject["at"]!}`来自群`{(long) jsonObject["group"]!}`执行了: {(string) jsonObject["cmd"]!}");
                Console.ResetColor();
                RestObject dictionary = new () { { "type", "cmd" }, { "result", string.Join('\n', tr.GetCommandOutput()) }, { "at", (string) jsonObject["at"]! }, { "group", (long) jsonObject["group"]! } };
                await SendDateAsync(dictionary.ToJson());
                break;
            case "online":
                var onlineResult = "";
                List<string> players = new ();
                if (!Main.player.Any(p => p is { active: true }))
                {
                    onlineResult = "当前没有玩家在线捏";
                }
                else
                {
                    onlineResult +=
                        $"在线的玩家({Main.player.Count(p => null != p && p.active)}/{Main.maxNetPlayers})\n";

                    for (var k = 0; k < 255; k++)
                    {
                        if (Main.player[k] != null && Main.player[k].active)
                        {
                            players.Add(Main.player[k].name);
                        }
                    }

                    onlineResult += string.Join(',', players).TrimEnd(',');
                }

                List<string> onlineProcessList = new ();

                #region 进度查询

                if (!NPC.downedSlimeKing)
                {
                    onlineProcessList.Add("史王");
                }

                if (!NPC.downedBoss1)
                {
                    onlineProcessList.Add("克眼");
                }

                if (!NPC.downedBoss2)
                {
                    if (Main.drunkWorld)
                    {
                        onlineProcessList.Add("世吞/克脑");
                    }
                    else
                    {
                        if (WorldGen.crimson)
                        {
                            onlineProcessList.Add("克脑");
                        }
                        else
                        {
                            onlineProcessList.Add("世吞");
                        }
                    }
                }

                if (!NPC.downedBoss3)
                {
                    onlineProcessList.Add("骷髅王");
                }

                if (!Main.hardMode)
                {
                    onlineProcessList.Add("血肉墙");
                }

                if (!NPC.downedMechBoss2 || !NPC.downedMechBoss1 || !NPC.downedMechBoss3)
                {
                    if (Main.zenithWorld)
                    {
                        onlineProcessList.Add("美杜莎");
                    }
                    else
                    {
                        onlineProcessList.Add("新三王");
                    }
                }

                if (!NPC.downedPlantBoss)
                {
                    onlineProcessList.Add("世花");
                }

                if (!NPC.downedGolemBoss)
                {
                    onlineProcessList.Add("石巨人");
                }

                if (!NPC.downedAncientCultist)
                {
                    onlineProcessList.Add("拜月教徒");
                }

                if (!NPC.downedTowers)
                {
                    onlineProcessList.Add("四柱");
                }

                if (!NPC.downedMoonlord)
                {
                    onlineProcessList.Add("月总");
                }

                string onlineProcess;
                onlineProcess = !onlineProcessList.Any() ? "已毕业" : onlineProcessList.ElementAt(0) + "前";

                #endregion

                result = new RestObject
                {
                    { "type", "online" },
                    { "result", onlineResult },
                    { "worldname", Main.worldName },
                    { "process", onlineProcess },
                    { "group", (long) jsonObject["group"]! }
                };
                await SendDateAsync(JsonConvert.SerializeObject(result));
                break;
            case "process":
                List<Dictionary<string, bool>> processList = new (
                    [new () { { "King Slime", NPC.downedSlimeKing } }, new () { { "Eye of Cthulhu", NPC.downedBoss1 } }, new () { { "Eater of Worlds / Brain of Cthulhu", NPC.downedBoss2 } }, new () { { "Queen Bee", NPC.downedQueenBee } }, new () { { "Skeletron", NPC.downedBoss3 } }, new () { { "Deerclops", NPC.downedDeerclops } }, new () { { "Wall of Flesh", Main.hardMode } }, new () { { "Queen Slime", NPC.downedQueenSlime } }, new () { { "The Twins", NPC.downedMechBoss2 } }, new () { { "The Destroyer", NPC.downedMechBoss1 } }, new () { { "Skeletron Prime", NPC.downedMechBoss3 } }, new () { { "Plantera", NPC.downedPlantBoss } }, new () { { "Golem", NPC.downedGolemBoss } }, new () { { "Duke Fishron", NPC.downedFishron } }, new () { { "Empress of Light", NPC.downedEmpressOfLight } }, new () { { "Lunatic Cultist", NPC.downedAncientCultist } }, new () { { "Moon Lord", NPC.downedMoonlord } }, new () { { "Solar Pillar", NPC.downedTowerSolar } }, new () { { "Nebula Pillar", NPC.downedTowerNebula } }, new () { { "Vortex Pillar", NPC.downedTowerVortex } }, new () { { "Stardust Pillar", NPC.downedTowerStardust } }]);
                result = new RestObject { { "type", "process" }, { "result", processList }, { "worldname", Main.worldName }, { "group", (long) jsonObject["group"]! } };
                await SendDateAsync(JsonConvert.SerializeObject(result));
                break;
            case "whitelist":
                var name = (string) jsonObject["name"]!;
                var code = (int) jsonObject["code"]!;
                await Login.CheckWhiteAsync(name, code);
                break;
            case "selfkick":
                name = (string) jsonObject["name"]!;
                var playerList2 = TSPlayer.FindByNameOrID("tsn:" + name);
                if (playerList2.Count == 0)
                {
                    return;
                }

                playerList2[0].Kick("在群中使用自踢命令.");

                break;
            case "mappng":
                break; //无法实现, 太Cai了5555
            case "lookbag":
                name = (string) jsonObject["name"]!;
                var playerList3 = TSPlayer.FindByNameOrID("tsn:" + name);
                List<int> buffs;
                if (playerList3.Count != 0)
                {
                    // 在线
                    var plr = playerList3[0].TPlayer;
                    buffs = plr.buffType.ToList();
                    var invs = new NetItem[NetItem.MaxInventory];
                    Item[] inventory = plr.inventory;
                    Item[] armor = plr.armor;
                    Item[] dye = plr.dye;
                    Item[] miscEqups = plr.miscEquips;
                    Item[] miscDyes = plr.miscDyes;
                    Item[] piggy = plr.bank.item;
                    Item[] safe = plr.bank2.item;
                    Item[] forge = plr.bank3.item;
                    Item[] voidVault = plr.bank4.item;
                    var trash = plr.trashItem;
                    Item[] loadout1Armor = plr.Loadouts[0].Armor;
                    Item[] loadout1Dye = plr.Loadouts[0].Dye;
                    Item[] loadout2Armor = plr.Loadouts[1].Armor;
                    Item[] loadout2Dye = plr.Loadouts[1].Dye;
                    Item[] loadout3Armor = plr.Loadouts[2].Armor;
                    Item[] loadout3Dye = plr.Loadouts[2].Dye;
                    for (var i = 0; i < NetItem.MaxInventory; i++)
                    {
                        if (i < NetItem.InventoryIndex.Item2)
                        {
                            //0-58
                            invs[i] = (NetItem) inventory[i];
                        }
                        else if (i < NetItem.ArmorIndex.Item2)
                        {
                            //59-78
                            var index = i - NetItem.ArmorIndex.Item1;
                            invs[i] = (NetItem) armor[index];
                        }
                        else if (i < NetItem.DyeIndex.Item2)
                        {
                            //79-88
                            var index = i - NetItem.DyeIndex.Item1;
                            invs[i] = (NetItem) dye[index];
                        }
                        else if (i < NetItem.MiscEquipIndex.Item2)
                        {
                            //89-93
                            var index = i - NetItem.MiscEquipIndex.Item1;
                            invs[i] = (NetItem) miscEqups[index];
                        }
                        else if (i < NetItem.MiscDyeIndex.Item2)
                        {
                            //93-98
                            var index = i - NetItem.MiscDyeIndex.Item1;
                            invs[i] = (NetItem) miscDyes[index];
                        }
                        else if (i < NetItem.PiggyIndex.Item2)
                        {
                            //98-138
                            var index = i - NetItem.PiggyIndex.Item1;
                            invs[i] = (NetItem) piggy[index];
                        }
                        else if (i < NetItem.SafeIndex.Item2)
                        {
                            //138-178
                            var index = i - NetItem.SafeIndex.Item1;
                            invs[i] = (NetItem) safe[index];
                        }
                        else if (i < NetItem.TrashIndex.Item2)
                        {
                            //179-219
                            invs[i] = (NetItem) trash;
                        }
                        else if (i < NetItem.ForgeIndex.Item2)
                        {
                            //220
                            var index = i - NetItem.ForgeIndex.Item1;
                            invs[i] = (NetItem) forge[index];
                        }
                        else if (i < NetItem.VoidIndex.Item2)
                        {
                            //220
                            var index = i - NetItem.VoidIndex.Item1;
                            invs[i] = (NetItem) voidVault[index];
                        }
                        else if (i < NetItem.Loadout1Armor.Item2)
                        {
                            var index = i - NetItem.Loadout1Armor.Item1;
                            invs[i] = (NetItem) loadout1Armor[index];
                        }
                        else if (i < NetItem.Loadout1Dye.Item2)
                        {
                            var index = i - NetItem.Loadout1Dye.Item1;
                            invs[i] = (NetItem) loadout1Dye[index];
                        }
                        else if (i < NetItem.Loadout2Armor.Item2)
                        {
                            var index = i - NetItem.Loadout2Armor.Item1;
                            invs[i] = (NetItem) loadout2Armor[index];
                        }
                        else if (i < NetItem.Loadout2Dye.Item2)
                        {
                            var index = i - NetItem.Loadout2Dye.Item1;
                            invs[i] = (NetItem) loadout2Dye[index];
                        }
                        else if (i < NetItem.Loadout3Armor.Item2)
                        {
                            var index = i - NetItem.Loadout3Armor.Item1;
                            invs[i] = (NetItem) loadout3Armor[index];
                        }
                        else if (i < NetItem.Loadout3Dye.Item2)
                        {
                            var index = i - NetItem.Loadout3Dye.Item1;
                            invs[i] = (NetItem) loadout3Dye[index];
                        }
                    }

                    List<List<int>> itemList = new ();
                    foreach (var i in invs)
                    {
                        itemList.Add(new List<int> { i.NetId, i.Stack });
                    }

                    result = new RestObject
                    {
                        { "type", "lookbag" },
                        { "name", name },
                        { "exist", 1 },
                        { "inventory", itemList },
                        { "buffs", buffs },
                        { "group", (long) jsonObject["group"]! }
                    };
                    await SendDateAsync(JsonConvert.SerializeObject(result));
                    return;
                }

                result = new RestObject { { "type", "lookbag" }, { "exist", 0 }, { "group", (long) jsonObject["group"]! } };
                await SendDateAsync(JsonConvert.SerializeObject(result));
                break;
            case "pluginlist":
                var mods = ModLoader.Mods.Skip(1);
                var modList = mods.Select(p => new ModInfo(p.DisplayName, p.Version)).ToList();
                result = new RestObject { { "type", "modlist" }, { "mods", modList }, { "group", (long) jsonObject["group"]! } };
                await SendDateAsync(JsonConvert.SerializeObject(result));
                break;
        }
    }
}