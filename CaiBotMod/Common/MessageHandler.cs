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

    private static string GetItemDesc(Item item, bool isFlag = false)
    {
        if (item.netID == 0)
        {
            return "";
        }

        if (item.ModItem == null)
        {
            return GetItemDesc(item.netID, item.Name, item.stack, item.prefix, isFlag);
        }
        else
        {
            var name = item.ModItem.LocalizationCategory;
            return GetItemDesc(item.netID, name, item.stack, item.prefix, isFlag);
        }
    }
    

    private static string GetItemDesc(int id, bool Tag = true)
    {
        return Tag ? $"[i:{id}]" : $"[{Lang.GetItemNameValue(id)}]";
    }

    private static string GetItemDesc(int id, string name, int stack, int prefix, bool isFlag = false)
    {
        if (isFlag)
        {
            // https://terraria.fandom.com/wiki/Chat
            // [i:29]   数量
            // [i/s10:29]   数量
            // [i/p57:4]    词缀
            // 控制台显示 物品名称
            // 4.4.0 -1.4.1.2   [i:4444]
            // 4.5.0 -1.4.2.2   [女巫扫帚]
            //ChatItemIsIcon = TShock.VersionNum.CompareTo(new Version(4, 5, 0, 0)) >= 0;
            //Console.WriteLine($"ChatItemIsIcon:");
            if (stack > 1)
            {
                return $"[i/s{stack}:{id}]";
            }

            if (prefix.Equals(0))
            {
                return $"[i:{id}]";
            }

            return $"[i/p{prefix}:{id}]";
        }

        var s = name;
        var prefixName = Lang.prefix[prefix].Value;
        if (prefixName != "")
        {
            s = $"{prefixName}的 {s}";
        }

        if (stack > 1)
        {
            s = $"{s} ({stack})";
        }

        return $"[{s}]";
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
                    [new Dictionary<string, bool> { { "King Slime", NPC.downedSlimeKing } }, new Dictionary<string, bool> { { "Eye of Cthulhu", NPC.downedBoss1 } }, new Dictionary<string, bool> { { "Eater of Worlds / Brain of Cthulhu", NPC.downedBoss2 } }, new Dictionary<string, bool> { { "Queen Bee", NPC.downedQueenBee } }, new Dictionary<string, bool> { { "Skeletron", NPC.downedBoss3 } }, new Dictionary<string, bool> { { "Deerclops", NPC.downedDeerclops } }, new Dictionary<string, bool> { { "Wall of Flesh", Main.hardMode } }, new Dictionary<string, bool> { { "Queen Slime", NPC.downedQueenSlime } }, new Dictionary<string, bool> { { "The Twins", NPC.downedMechBoss2 } }, new Dictionary<string, bool> { { "The Destroyer", NPC.downedMechBoss1 } }, new Dictionary<string, bool> { { "Skeletron Prime", NPC.downedMechBoss3 } }, new Dictionary<string, bool> { { "Plantera", NPC.downedPlantBoss } }, new Dictionary<string, bool> { { "Golem", NPC.downedGolemBoss } }, new Dictionary<string, bool> { { "Duke Fishron", NPC.downedFishron } }, new Dictionary<string, bool> { { "Empress of Light", NPC.downedEmpressOfLight } }, new Dictionary<string, bool> { { "Lunatic Cultist", NPC.downedAncientCultist } }, new Dictionary<string, bool> { { "Moon Lord", NPC.downedMoonlord } }, new Dictionary<string, bool> { { "Solar Pillar", NPC.downedTowerSolar } }, new Dictionary<string, bool> { { "Nebula Pillar", NPC.downedTowerNebula } }, new Dictionary<string, bool> { { "Vortex Pillar", NPC.downedTowerVortex } }, new Dictionary<string, bool> { { "Stardust Pillar", NPC.downedTowerStardust } }]);
                result = new RestObject { { "type", "process" }, { "result", processList }, { "worldname", Main.worldName }, { "group", (long) jsonObject["group"]! } };
                await SendDateAsync(JsonConvert.SerializeObject(result));
                break;
            case "whitelist":
                var name = (string) jsonObject["name"]!;
                var code = (int) jsonObject["code"]!;
                Login.CheckWhiteAsync(name, code);
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
                    var plr = playerList3[0].TPlayer;
                    var msgs = new List<string>();
                    // 在线
                    msgs.Add($"玩家: {plr.name}");
                    msgs.Add($"生命: {plr.statLife}/{plr.statLifeMax}");
                    msgs.Add($"魔力: {plr.statMana}/{plr.statManaMax}");
                    msgs.Add($"渔夫任务: {plr.anglerQuestsFinished} 次");

                    List<string> enhance = new ();
                    if (plr.extraAccessory)
                    {
                        enhance.Add(GetItemDesc(3335)); // 3335 恶魔之心
                    }

                    if (plr.unlockedBiomeTorches)
                    {
                        enhance.Add(GetItemDesc(5043)); // 5043 火把神徽章
                    }

                    if (plr.ateArtisanBread)
                    {
                        enhance.Add(GetItemDesc(5326)); // 5326	工匠面包
                    }

                    if (plr.usedAegisCrystal)
                    {
                        enhance.Add(GetItemDesc(5337)); // 5337 生命水晶	永久强化生命再生 
                    }

                    if (plr.usedAegisFruit)
                    {
                        enhance.Add(GetItemDesc(5338)); // 5338 埃癸斯果	永久提高防御力 
                    }

                    if (plr.usedArcaneCrystal)
                    {
                        enhance.Add(GetItemDesc(5339)); // 5339 奥术水晶	永久提高魔力再生 
                    }

                    if (plr.usedGalaxyPearl)
                    {
                        enhance.Add(GetItemDesc(5340)); // 5340	银河珍珠	永久增加运气 
                    }

                    if (plr.usedGummyWorm)
                    {
                        enhance.Add(GetItemDesc(5341)); // 5341	黏性蠕虫	永久提高钓鱼技能  
                    }

                    if (plr.usedAmbrosia)
                    {
                        enhance.Add(GetItemDesc(5342)); // 5342	珍馐	 永久提高采矿和建造速度 
                    }

                    if (plr.unlockedSuperCart)
                    {
                        enhance.Add(GetItemDesc(5289)); // 5289	矿车升级包
                    }

                    if (enhance.Count != 0)
                    {
                        msgs.Add("永久增强: " + string.Join(",", enhance));
                    }
                    else
                    {
                        msgs.Add("永久增强: " + "啥都没有...");
                    }

                    List<string> inventory = new ();
                    List<string> assist = new ();
                    List<string> armor = new ();
                    List<string> vanity = new ();
                    List<string> dye = new ();
                    List<string> miscEquips = new ();
                    List<string> miscDyes = new ();
                    List<string> bank = new ();
                    List<string> bank2 = new ();
                    List<string> bank3 = new ();
                    List<string> bank4 = new ();
                    List<string> armor1 = new ();
                    List<string> armor2 = new ();
                    List<string> armor3 = new ();
                    List<string> vanity1 = new ();
                    List<string> vanity2 = new ();
                    List<string> vanity3 = new ();
                    List<string> dye1 = new ();
                    List<string> dye2 = new ();
                    List<string> dye3 = new ();

                    string s;
                    for (var i = 0; i < 59; i++)
                    {
                        s = GetItemDesc(plr.inventory[i].Clone());
                        if (i < 50)
                        {
                            if (s != "")
                            {
                                inventory.Add(s);
                            }
                        }
                        else if (i >= 50 && i < 59)
                        {
                            if (s != "")
                            {
                                assist.Add(s);
                            }
                        }
                    }

                    for (var i = 0; i < plr.armor.Length; i++)
                    {
                        s = GetItemDesc(plr.armor[i]);
                        if (i < 10)
                        {
                            if (s != "")
                            {
                                armor.Add(s);
                            }
                        }
                        else
                        {
                            if (s != "")
                            {
                                vanity.Add(s);
                            }
                        }
                    }

                    for (var i = 0; i < plr.dye.Length; i++)
                    {
                        s = GetItemDesc(plr.dye[i]);
                        if (s != "")
                        {
                            dye.Add(s);
                        }
                    }

                    for (var i = 0; i < plr.miscEquips.Length; i++)
                    {
                        s = GetItemDesc(plr.miscEquips[i]);
                        if (s != "")
                        {
                            miscEquips.Add(s);
                        }
                    }

                    for (var i = 0; i < plr.miscDyes.Length; i++)
                    {
                        s = GetItemDesc(plr.miscDyes[i]);
                        if (s != "")
                        {
                            miscDyes.Add(s);
                        }
                    }

                    for (var i = 0; i < plr.bank.item.Length; i++)
                    {
                        s = GetItemDesc(plr.bank.item[i]);
                        if (s != "")
                        {
                            bank.Add(s);
                        }
                    }

                    for (var i = 0; i < plr.bank2.item.Length; i++)
                    {
                        s = GetItemDesc(plr.bank2.item[i]);
                        if (s != "")
                        {
                            bank2.Add(s);
                        }
                    }

                    for (var i = 0; i < plr.bank3.item.Length; i++)
                    {
                        s = GetItemDesc(plr.bank3.item[i]);
                        if (s != "")
                        {
                            bank3.Add(s);
                        }
                    }

                    for (var i = 0; i < plr.bank4.item.Length; i++)
                    {
                        s = GetItemDesc(plr.bank4.item[i]);
                        if (s != "")
                        {
                            bank4.Add(s);
                        }
                    }

                    // 装备（loadout）
                    for (var i = 0; i < plr.Loadouts.Length; i++)
                    {
                        Item[] items = plr.Loadouts[i].Armor;
                        // 装备 和 时装
                        for (var j = 0; j < items.Length; j++)
                        {
                            s = GetItemDesc(items[j]);
                            if (!string.IsNullOrEmpty(s))
                            {
                                if (i == 0)
                                {
                                    if (j < 10)
                                    {
                                        armor1.Add(s);
                                    }
                                    else
                                    {
                                        vanity1.Add(s);
                                    }
                                }
                                else if (i == 1)
                                {
                                    if (j < 10)
                                    {
                                        armor2.Add(s);
                                    }
                                    else
                                    {
                                        vanity2.Add(s);
                                    }
                                }
                                else if (i == 2)
                                {
                                    if (j < 10)
                                    {
                                        armor3.Add(s);
                                    }
                                    else
                                    {
                                        vanity3.Add(s);
                                    }
                                }
                            }
                        }

                        // 染料
                        items = plr.Loadouts[i].Dye;
                        for (var j = 0; j < items.Length; j++)
                        {
                            s = GetItemDesc(items[j]);
                            if (!string.IsNullOrEmpty(s))
                            {
                                if (i == 0)
                                {
                                    dye1.Add(s);
                                }
                                else if (i == 1)
                                {
                                    dye2.Add(s);
                                }
                                else if (i == 2)
                                {
                                    dye3.Add(s);
                                }
                            }
                        }
                    }

                    List<string> trash = new ();
                    s = GetItemDesc(plr.trashItem);
                    if (s != "")
                    {
                        trash.Add(s);
                    }

                    if (inventory.Count != 0)
                    {
                        msgs.Add("背包：" + string.Join(",", inventory));
                    }
                    else
                    {
                        msgs.Add("背包：啥都没有...");
                    }

                    if (trash.Count != 0)
                    {
                        msgs.Add("垃圾桶：" + string.Join(",", trash));
                    }
                    else
                    {
                        msgs.Add("垃圾桶：啥都没有...");
                    }

                    if (assist.Count != 0)
                    {
                        msgs.Add("钱币弹药：" + string.Join(",", assist));
                    }


                    var num = plr.CurrentLoadoutIndex + 1;

                    if (armor.Count != 0)
                    {
                        msgs.Add($">装备{num}：" + string.Join(",", armor));
                    }


                    if (vanity.Count != 0)
                    {
                        msgs.Add($">时装{num}：" + string.Join(",", vanity));
                    }


                    if (dye.Count != 0)
                    {
                        msgs.Add($">染料{num}：" + string.Join(",", dye));
                    }


                    if (armor1.Count != 0)
                    {
                        msgs.Add("装备1：" + string.Join(",", armor1));
                    }


                    if (vanity1.Count != 0)
                    {
                        msgs.Add("时装1：" + string.Join(",", vanity1));
                    }


                    if (dye1.Count != 0)
                    {
                        msgs.Add("染料1：" + string.Join(",", dye1));
                    }


                    if (armor2.Count != 0)
                    {
                        msgs.Add("装备2：" + string.Join(",", armor2));
                    }


                    if (vanity2.Count != 0)
                    {
                        msgs.Add("时装2：" + string.Join(",", vanity2));
                    }


                    if (dye2.Count != 0)
                    {
                        msgs.Add("染料2：" + string.Join(",", dye2));
                    }


                    if (armor3.Count != 0)
                    {
                        msgs.Add("装备3：" + string.Join(",", armor3));
                    }


                    if (vanity3.Count != 0)
                    {
                        msgs.Add("时装3：" + string.Join(",", vanity3));
                    }


                    if (dye3.Count != 0)
                    {
                        msgs.Add("染料3：" + string.Join(",", dye3));
                    }


                    if (miscEquips.Count != 0)
                    {
                        msgs.Add("工具栏：" + string.Join(",", miscEquips));
                    }


                    if (miscDyes.Count != 0)
                    {
                        msgs.Add("染料2：" + string.Join(",", miscDyes));
                    }


                    if (bank.Count != 0)
                    {
                        msgs.Add("储蓄罐：" + string.Join(",", bank));
                    }


                    if (bank2.Count != 0)
                    {
                        msgs.Add("保险箱：" + string.Join(",", bank2));
                    }


                    if (bank3.Count != 0)
                    {
                        msgs.Add("护卫熔炉：" + string.Join(",", bank3));
                    }


                    if (bank4.Count != 0)
                    {
                        msgs.Add("虚空保险箱：" + string.Join(",", bank4));
                    }

                    result = new RestObject
                    {
                        { "type", "lookbag_text" },
                        { "name", name },
                        { "exist", 1 },
                        // { "inventory", string.Join("\n", msgs) },
                        { "inventory", string.Join("暂时无法使用") },
                        { "group", (long) jsonObject["group"]! }
                    };
                    //Console.WriteLine(string.Join("\n", msgs));
                }
                else
                {
                    result = new RestObject { { "type", "lookbag" }, { "exist", 0 }, { "group", (long) jsonObject["group"]! } };
                }

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