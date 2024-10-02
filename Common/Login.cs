using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CaibotExtension.Common
{
    public static class Login
    {
        public static async Task<bool> CheckWhiteAsync(string name, int code,List<string> uuids)
        {
            var playerList = TSPlayer.FindByNameOrID("tsn:" + name);
            var number = Config.config.GroupNumber;
            if (playerList.Count == 0)
            {
                return false;
            }
            TSPlayer plr = playerList[0];
            //NetMessage.SendData(9, args.Who, -1, Terraria.Localization.NetworkText.FromLiteral($"[白名单]正在校验白名单..."), 1);
            if (string.IsNullOrEmpty(name))
            {
                Console.WriteLine($"[白名单]玩家[{name}](IP: {plr.IP})版本可能过低...");
                plr.Disconnect("你的游戏版本可能过低,请使用Terraria1.4.4+游玩");
                return false;
            }
            try
            {
                switch (code)
                {
                    case 200:
                        {
                            Console.WriteLine($"[白名单]玩家[{name}](IP: {plr.IP})已通过白名单验证...");
                            //NetMessage.SendData(9, args.Who, -1, Terraria.Localization.NetworkText.FromLiteral($"[白名单]白名单校验成功!\n"), 1);
                            break;
                        }
                    case 404:
                        {
                            
                            Console.WriteLine($"[白名单]玩家[{name}](IP: {plr.IP})没有添加白名单...");
                            plr.SilentKickInProgress = true;
                            plr.Disconnect($"没有添加白名单\n请在群{number}内发送'添加白名单 角色名字'");
                            return false;
                        }
                    case 403:
                        {
                            
                            Console.WriteLine($"[白名单]玩家[{name}](IP: {plr.IP})白名单被冻结...");
                            plr.SilentKickInProgress = true;
                            plr.Disconnect("[白名单]你已被服务器屏蔽\n你在云黑名单内!");
                            return false;
                        }
                    case 401:
                        {
                            
                            Console.WriteLine($"[白名单]玩家[{name}](IP: {plr.IP})不在本群内...");
                            plr.SilentKickInProgress = true;
                            plr.Disconnect($"[白名单]不在本服务器群内!\n请加入服务器群：{number}");
                            return false;
                        }
                    default:
                        {
                            
                            Console.WriteLine($"[白名单]玩家[{name}](IP: {plr.IP})触发了百年一遇的白名单bug...");
                            plr.SilentKickInProgress = true;
                            plr.Disconnect("[白名单]这个情况可能只是一个摆设\n但是你触发了它？");
                            return false;
                        }
                }
                if (!uuids.Contains(plr.UUID))
                {
                    if (string.IsNullOrEmpty(plr.UUID))
                    {
                        //Console.WriteLine(plr.Index);
                        plr.SilentKickInProgress = true;
                        plr.Disconnect("[白名单]UUID为空\n请尝试重新加入游戏或者联系服务器管理员");
                        return false;
                    }
                    Console.WriteLine($"[白名单]玩家[{name}](IP: {plr.IP})没有批准登录...");
                    plr.SilentKickInProgress = true;
                    plr.Disconnect($"[白名单]在群{number}内发送'登录'，\n以批准此设备登录");

                    var re = new RestObject
                        {
                            { "type","device" },
                            { "uuid", plr.UUID },
                            { "ip", plr.IP },
                            { "name", name }
                        };
                    await MessageHandle.SendDateAsync(re.ToJson());

                    return false;
                }

            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"[白名单]玩家[{name}](IP: {plr.IP})验证白名单时出现错误...");
                Console.WriteLine("[XSB适配插件]:\n" + ex);
                plr.SilentKickInProgress = true;
                plr.Disconnect($"[白名单]服务器发生错误无法处理该请求!请尝试重新加入游戏或者联系服务器群{number}管理员");
                return false;
            }
            

           
            return true;
            //NetMessage.SendData(9, plr.Index, -1, NetworkText.FromLiteral("正在检查白名单..."), 1);

        }

    }
}
