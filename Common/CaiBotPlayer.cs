﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI.Chat;

namespace CaibotExtension.Common;
  public class CaiBotPlayer : TSPlayer
    {
        internal List<string> CommandOutput = new List<string>();

        public CaiBotPlayer()
            : base("CaiBot")
        {
            AwaitingResponse = new Dictionary<string, Action<object>>();
            
        }

        //public override void SendMessage(string msg, Color color)
        //{
        //    SendMessage(msg, color.R, color.G, color.B);
        //}

        public override void SendMessage(string msg, byte red, byte green, byte blue)
        {
            string result1 = "";
            foreach (TextSnippet item in ChatManager.ParseMessage(msg, new Color(red, green, blue)))
            {
                result1+= item.Text;
            }
            Regex regex = new Regex(@"\[i(tem)?(?:\/s(?<Stack>\d{1,4}))?(?:\/p(?<Prefix>\d{1,3}))?:(?<NetID>-?\d{1,4})\]");

            string result = regex.Replace(result1, m =>
            {
                string netID = m.Groups["NetID"].Value;
                string prefix = m.Groups["Prefix"].Success ? m.Groups["Prefix"].Value : "0";
                string stack = m.Groups["Stack"].Success ? m.Groups["Stack"].Value : "0";
                if (stack == "0")
                    return "";
                if (stack == "1")
                    if (prefix == "0")
                        return $"[{Lang.GetItemName(int.Parse(netID))}]";
                    else
                        return $"[{Lang.prefix[int.Parse(prefix)]} {Lang.GetItemName(int.Parse(netID))}]"; //return $"[{Terraria.Lang.prefix[int.Parse(netID)]}]";
                return $"[{Lang.prefix[int.Parse(prefix)]} {Lang.GetItemName(int.Parse(netID))} ({stack})]";
            });
            CommandOutput.Add(result);
        }

        public override void SendInfoMessage(string msg)
        {
            SendMessage(msg, System.Drawing.Color.Yellow);
        }

        public override void SendSuccessMessage(string msg)
        {
            SendMessage(msg, System.Drawing.Color.Green);
        }

        public override void SendWarningMessage(string msg)
        {
            SendMessage(msg, System.Drawing.Color.OrangeRed);
        }

        public override void SendErrorMessage(string msg)
        {
            SendMessage(msg, System.Drawing.Color.Red);
        }

        public List<string> GetCommandOutput()
        {
            return CommandOutput;
        }
    }
