using Terraria.ModLoader;
using tModPorter;
using Terraria;
using CaiBot扩展.Common;
using System;
using System.Threading.Tasks;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;
using FullSerializer;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using MonoMod.Utils;
using Terraria.ID;
using log4net.Repository.Hierarchy;
using System.Threading;
using Terraria.Net;
using System.Reflection;
using XPT.Core.Audio.MP3Sharp.Decoding;
using Terraria.DataStructures;
using System.Collections.Generic;
using static log4net.Appender.ColoredConsoleAppender;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections;
using Terraria.Localization;

namespace CaiBot扩展.Common
{
    public class Packet : ModSystem
    {
        public static string[] UUIDs = new string[256];
        public static bool[] Login = new bool[256];
        public override bool HijackSendData(int whoAmI, int msgType, int remoteClient, int ignoreClient, NetworkText text, int number, float number2, float number3, float number4, int number5, int number6, int number7)
        {
            
            if (msgType == 2 && text.ToString() == Lang.mp[2].Value)
                return true;
            return false;

        }
        public override bool HijackGetData(ref byte messageType, ref BinaryReader Reader, int playerNumber)
        {
            var reader = new BinaryReader(Reader.BaseStream);
            if (!Main.dedServ)
            {
                return false;
            }
            // Console.WriteLine("钩子");
            switch (messageType)
            {
                case 4:
                    {
                        if (!Config.config.WhiteList)
                        {
                            return false;
                        }
                        //Console.WriteLine($"玩家登录：{Login[playerNumber]}");
                        if (Login[playerNumber])
                        {
                            return false;
                        }
                        reader.ReadByte();
                        reader.ReadByte();
                        reader.ReadByte();
                        var name = reader.ReadString().Trim().Trim();
                        if (name.Length == 17)
                        {
                            return false;
                        }
                        var re = new RestObject
                        {
                            { "type","whitelist" },
                            { "name",name }
                        };
                        if (!MessageHandle.isWebsocketConnected)
                        {
                            Console.WriteLine("[CaiBot]机器人处于未连接状态, 玩家无法加入。\n" +
                                "如果你不想使用Cai白名单，可以在tshock/CaiBot.json中将其关闭。");
                            return true;
                        }
                        MessageHandle.SendDateAsync(re.ToJson()).Wait();
                        break;
                    }
                case 68:
                    {
                        UUIDs[playerNumber] = reader.ReadString();
                        Login[playerNumber] = false;
                        //Console.WriteLine($"[CaiBot]获取UUID:{UUIDs[playerNumber]}({playerNumber})");
                        break;
                    }
                case 82:
                    ushort moduleId = reader.ReadUInt16();
                    //LoadNetModule is now used for sending chat text.
                    //Read the module ID to determine if this is in fact the text module
                    if (moduleId == Terraria.Net.NetManager.Instance.GetId<Terraria.GameContent.NetModules.NetTextModule>())
                    {
                        //Then deserialize the message from the reader
                        Terraria.Chat.ChatMessage msg = Terraria.Chat.ChatMessage.Deserialize(reader);
                        TSPlayer player = new TSPlayer(playerNumber);
                        if (Commands.HandleCommand(player, msg.Text))
                        {
                            return true;
                        }
                    }
                    break;
                case 118:
                    {
                        var id = reader.ReadByte();
                        PlayerDeathReason playerDeathReason = PlayerDeathReason.FromReader(new BinaryReader(reader.BaseStream));
                        var dmg = reader.ReadInt16();
                        var direction = (byte)(reader.ReadByte() - 1);
                        BitsByte bits = (BitsByte)reader.ReadByte();
                        bool pvp = bits[0];
                        if (CaiBot扩展.PlayerDeath.ContainsKey(Main.player[playerNumber].name))
                        {
                            CaiBot扩展.PlayerDeath[Main.player[playerNumber].name] = new System.Drawing.Point((int)Main.player[playerNumber].position.X, (int)Main.player[playerNumber].position.Y);
                        }
                        else
                        {
                            CaiBot扩展.PlayerDeath.Add(Main.player[playerNumber].name, new System.Drawing.Point((int)Main.player[playerNumber].position.X, (int)Main.player[playerNumber].position.Y));
                        }
                        break;
                    }
                case 217:
                    {
                        if (!string.IsNullOrEmpty(Config.config.Token))
                        {
                            NetMessage.SendData(2, playerNumber, -1, NetworkText.FromFormattable("exist"));
                            return false;
                        }
                        string data = reader.ReadString();
                        string token = Guid.NewGuid().ToString();
                        if (data == CaiBot扩展.code.ToString())
                        {

                            NetMessage.SendData(2, playerNumber, -1, NetworkText.FromFormattable(token));
                            Config.config.Token = token;
                            Config.config.Write();
                        }
                        else
                        {
                            NetMessage.SendData(2, playerNumber, -1, NetworkText.FromFormattable("code"));
                        }
                        return true;
                    }

            }
            return false;
        }
        
    }

}
