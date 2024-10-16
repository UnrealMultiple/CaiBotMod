﻿using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.GameContent.NetModules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Net;

namespace CaiBotMod.Common;

public class Packet : ModSystem
{
    public static readonly string[] UUIDs = new string[256];
    public static readonly bool[] Login = new bool[256];

    public override bool HijackSendData(int whoAmI, int msgType, int remoteClient, int ignoreClient, NetworkText text,
        int number, float number2, float number3, float number4, int number5, int number6, int number7)
    {
        if (msgType == 2 && text.ToString() == Lang.mp[2].Value)
        {
            return true;
        }

        return false;
    }

    public override bool HijackGetData(ref byte messageType, ref BinaryReader Reader, int playerNumber)
    {
        BinaryReader reader = new (Reader.BaseStream);
        if (!Main.dedServ)
        {
            return false;
        }

        switch (messageType)
        {
            case 4:
            {
                
                if (!Config.config.WhiteList)
                {
                    return false;
                }
                
                reader.ReadByte();
                reader.ReadByte();
                reader.ReadByte();
                
                var name = reader.ReadString().Trim();
                
                if (name.Length == 17)
                {
                    return false;
                }

                Task.Run(async () =>
                {

                    var timeout = Task.Delay(1000); //貌似不是很优雅捏
                    await Task.Run(async () =>
                    {
                        while ((Login[playerNumber] &&  UUIDs[playerNumber]!=null  && timeout.IsCompleted == false) || (UUIDs[playerNumber]==null && timeout.IsCompleted == false))
                        {
                            await Task.Delay(10);
                        }

                    });
                    if (!Login[playerNumber] && timeout.IsCompleted)
                    {
                        NetMessage.SendData(MessageID.Kick, playerNumber, -1, NetworkText.FromLiteral("[CaiBot]UUID等待超时"));
                        Netplay.Clients[playerNumber].Socket.Close();
                        return true;
                    }
                    if (Login[playerNumber])
                    {
                        return false;
                    }
                    RestObject re = new ()
                    {
                        { "type", "whitelistV2" }, { "name", name }, { "uuid", UUIDs[playerNumber] }, { "ip", Netplay.Clients[playerNumber].Socket.GetRemoteAddress().ToString()! },
                    };
                    if (!MessageHandle.IsWebsocketConnected)
                    {
                        Console.WriteLine("[CaiBot]机器人处于未连接状态, 玩家无法加入。\n" +
                                          "如果你不想使用Cai白名单，可以在tshock/CaiBot.json中将其关闭。");
                        return true;
                    }

                    await MessageHandle.SendDateAsync(re.ToJson());

                    return false;
                });
                return false;
            }
            case 68:
            {
                UUIDs[playerNumber] = reader.ReadString();
                Login[playerNumber] = false;
                //Console.WriteLine($"[CaiBot]获取UUID:{UUIDs[playerNumber]}({playerNumber})");
                break;
            }
            case 82:
                var moduleId = reader.ReadUInt16();
                //LoadNetModule is now used for sending chat text.
                //Read the module ID to determine if this is in fact the text module
                if (moduleId == NetManager.Instance.GetId<NetTextModule>())
                {
                    //Then deserialize the message from the reader
                    var msg = ChatMessage.Deserialize(reader);
                    TSPlayer player = new (playerNumber);
                    if (Commands.HandleCommand(player, msg.Text))
                    {
                        return true;
                    }
                }

                break;
            case 118:
            {
                var id = reader.ReadByte();
                var playerDeathReason = PlayerDeathReason.FromReader(new BinaryReader(reader.BaseStream));
                var dmg = reader.ReadInt16();
                var direction = (byte) (reader.ReadByte() - 1);
                BitsByte bits = reader.ReadByte();
                var pvp = bits[0];
                if (CaiBotMod.PlayerDeath.ContainsKey(Main.player[playerNumber].name))
                {
                    CaiBotMod.PlayerDeath[Main.player[playerNumber].name] = new Point(
                        (int) Main.player[playerNumber].position.X, (int) Main.player[playerNumber].position.Y);
                }
                else
                {
                    CaiBotMod.PlayerDeath.Add(Main.player[playerNumber].name,
                        new Point((int) Main.player[playerNumber].position.X,
                            (int) Main.player[playerNumber].position.Y));
                }

                break;
            }
            case 217:
            {
                if (!string.IsNullOrEmpty(Config.config.Token))
                {
                    NetMessage.SendData(MessageID.Kick, playerNumber, -1, NetworkText.FromFormattable("exist"));
                    return false;
                }

                var data = reader.ReadString();
                var token = Guid.NewGuid().ToString();
                if (data == CaiBotMod.InitCode.ToString())
                {
                    NetMessage.SendData(MessageID.Kick, playerNumber, -1, NetworkText.FromFormattable(token));
                    Config.config.Token = token;
                    Config.config.Write();
                }
                else
                {
                    NetMessage.SendData(MessageID.Kick, playerNumber, -1, NetworkText.FromFormattable("code"));
                }

                return true;
            }
        }

        return false;
    }
}