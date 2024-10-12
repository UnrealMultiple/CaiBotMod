using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Terraria;

namespace CaiBotMod.Common;

public static class Login
{
    public static async Task<bool> CheckWhiteAsync(string name, int code)
    {
        var playerList = TSPlayer.FindByNameOrID("tsn:" + name);
        var number = Config.config.GroupNumber;
        if (playerList.Count == 0)
        {
            return false;
        }

        var plr = playerList[0];
        //NetMessage.SendData(9, args.Who, -1, Terraria.Localization.NetworkText.FromLiteral($"[白名单]正在校验白名单..."), 1);
        if (string.IsNullOrEmpty(name))
        {
            Console.WriteLine($"[白名单]玩家[{name}](IP: {plr.IP})版本可能过低...");
            plr.Disconnect("你的游戏版本可能过低,请使用Terraria1.4.4+游玩");
            Netplay.Clients[plr.Index].Socket.Close();
            return false;
        }

        try
        {
            switch (code)
            {
                case 200:
                {
                    Console.WriteLine($"[白名单]玩家[{name}](IP: {plr.IP})已通过白名单验证...");
                    Packet.Login[plr.Index] = true;
                    break;
                }
                case 404:
                {
                    Console.WriteLine($"[白名单]玩家[{name}](IP: {plr.IP})没有添加白名单...");
                    plr.SilentKickInProgress = true;
                    plr.Disconnect($"没有添加白名单\n请在群{number}内发送'添加白名单 角色名字'");
                    Netplay.Clients[plr.Index].Socket.Close();
                    return false;
                }
                case 403:
                {
                    Console.WriteLine($"[白名单]玩家[{name}](IP: {plr.IP})白名单被冻结...");
                    plr.SilentKickInProgress = true;
                    plr.Disconnect("[白名单]你已被服务器屏蔽\n你在云黑名单内!");
                    Netplay.Clients[plr.Index].Socket.Close();
                    return false;
                }
                case 401:
                {
                    Console.WriteLine($"[白名单]玩家[{name}](IP: {plr.IP})不在本群内...");
                    plr.SilentKickInProgress = true;
                    plr.Disconnect($"[白名单]不在本服务器群内!\n请加入服务器群：{number}");
                    Netplay.Clients[plr.Index].Socket.Close();
                    return false;
                }
                case 405:
                {
                    Console.WriteLine(($"[Cai白名单]玩家[{name}](IP: {plr.IP})使用未授权的设备..."));
                    plr.SilentKickInProgress = true;
                    plr.Disconnect($"[Cai白名单]在群{number}内发送'登录',\n" +
                                   $"以批准此设备登录");
                    Netplay.Clients[plr.Index].Socket.Close();
                    
                    return false;
                }
            }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[白名单]玩家[{name}](IP: {plr.IP})验证白名单时出现错误...");
            Console.WriteLine("[XSB适配插件]:\n" + ex);
            plr.SilentKickInProgress = true;
            plr.Disconnect($"[白名单]服务器发生错误无法处理该请求!请尝试重新加入游戏或者联系服务器群{number}管理员");
            Netplay.Clients[plr.Index].Socket.Close();
            return false;
        }


        return true;
        //NetMessage.SendData(9, plr.Index, -1, NetworkText.FromLiteral("正在检查白名单..."), 1);
    }
}