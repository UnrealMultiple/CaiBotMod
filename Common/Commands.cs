/*
TShock, a server mod for Terraria
Copyright (C) 2011-2019 Pryaxis & TShock Contributors

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.GameContent.Events;
using Microsoft.Xna.Framework;
using System.Text.RegularExpressions;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ModLoader;

namespace CaiBotMod.Common
{
    public delegate void CommandDelegate(CommandArgs args);

    public class CommandArgs : EventArgs
    {
        public string Message { get; private set; }
        public TSPlayer Player { get; private set; }
        public bool Silent { get; private set; }

        /// <summary>
        /// Parameters passed to the argument. Does not include the command name.
        /// IE '/kick "jerk face"' will only have 1 argument
        /// </summary>
        public List<string> Parameters { get; private set; }

        public Player TPlayer
        {
            get { return Player.TPlayer; }
        }

        public CommandArgs(string message, TSPlayer ply, List<string> args)
        {
            Message = message;
            Player = ply;
            Parameters = args;
            Silent = false;
        }

        public CommandArgs(string message, bool silent, TSPlayer ply, List<string> args)
        {
            Message = message;
            Player = ply;
            Parameters = args;
            Silent = silent;
        }
    }

    public class Command
    {
        /// <summary>
        /// Gets or sets whether to allow non-players to use this command.
        /// </summary>
        public bool AllowServer { get; set; }
        /// <summary>
        /// Gets or sets whether to do logging of this command.
        /// </summary>
        public bool DoLog { get; set; }
        /// <summary>
        /// Gets or sets the help text of this command.
        /// </summary>
        public string HelpText { get; set; }
        /// <summary>
        /// Gets or sets an extended description of this command.
        /// </summary>
        public string[] HelpDesc { get; set; }
        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        public string Name { get { return Names[0]; } }
        /// <summary>
        /// Gets the names of the command.
        /// </summary>
        public List<string> Names { get; protected set; }
        /// <summary>
        /// Gets the permissions of the command.
        /// </summary>
        public List<string> Permissions { get; protected set; }

        private CommandDelegate commandDelegate;
        public CommandDelegate CommandDelegate
        {
            get { return commandDelegate; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();

                commandDelegate = value;
            }
        }

        public Command(List<string> permissions, CommandDelegate cmd, params string[] names)
            : this(cmd, names)
        {
            Permissions = permissions;
        }

        public Command(string permissions, CommandDelegate cmd, params string[] names)
            : this(cmd, names)
        {
            Permissions = new List<string> { permissions };
        }

        public Command(CommandDelegate cmd, params string[] names)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");
            if (names == null || names.Length < 1)
                throw new ArgumentException("names");

            AllowServer = true;
            CommandDelegate = cmd;
            DoLog = true;
            HelpText = "没有可用的帮助.";
            HelpDesc = null;
            Names = new List<string>(names);
            Permissions = new List<string>();
        }

        public bool Run(string msg, bool silent, TSPlayer ply, List<string> parms)
        {
            if (!CanRun(ply))
                return false;

            try
            {
                CommandDelegate(new CommandArgs(msg, silent, ply, parms));
            }
            catch (Exception e)
            {
                ply.SendErrorMessage("命令爆了,查看日志获得详细信息.");
            }

            return true;
        }

        public bool Run(string msg, TSPlayer ply, List<string> parms)
        {
            return Run(msg, false, ply, parms);
        }

        public bool HasAlias(string name)
        {
            return Names.Contains(name);
        }

        public bool CanRun(TSPlayer ply)
        {
            if (Permissions == null || Permissions.Count < 1)
                return true;
            foreach (var Permission in Permissions)
            {
                return true;
            }
            return false;
        }
    }

    public static class Commands
    {
        public static List<Command> ChatCommands = new List<Command>();
        public static ReadOnlyCollection<Command> TShockCommands = new ReadOnlyCollection<Command>(new List<Command>());

        /// <summary>
        /// The command specifier, defaults to "/"
        /// </summary>
        public static string Specifier
        {
            get { return "/"; }
        }

        /// <summary>
        /// The silent command specifier, defaults to "."
        /// </summary>
        public static string SilentSpecifier
        {
            get { return "#"; }
        }

        private delegate void AddChatCommand(string permission, CommandDelegate command, params string[] names);

        public static void InitCommands()
        {
            List<Command> tshockCommands = new List<Command>(100);
            Action<Command> add = (cmd) =>
            {
                tshockCommands.Add(cmd);
                ChatCommands.Add(cmd);
            };

            TShockCommands = new ReadOnlyCollection<Command>(tshockCommands);
        }

        public static bool HandleCommand(TSPlayer player, string text)
        {
            string cmdText = text.Remove(0, 1);
            string cmdPrefix = text[0].ToString();
            bool silent = false;

            if (cmdPrefix == SilentSpecifier)
                silent = true;
            else if (cmdPrefix == Specifier)
                silent = false;
            else 
                return false;
            int index = -1;
            for (int i = 0; i < cmdText.Length; i++)
            {
                if (IsWhiteSpace(cmdText[i]))
                {
                    index = i;
                    break;
                }
            }
            string cmdName;
            if (index == 0) // Space after the command specifier should not be supported
            {
                player.SendErrorMessage("您在{0}后输入了空格，而不是命令。键入{0}帮助以获取有效命令的列表.", Specifier);
                return true;
            }
            else if (index < 0)
                cmdName = cmdText.ToLower();
            else
                cmdName = cmdText.Substring(0, index).ToLower();

            List<string> args;
            if (index < 0)
                args = new List<string>();
            else
                args = ParseParameters(cmdText.Substring(index));

            IEnumerable<Command> cmds = ChatCommands.FindAll(c => c.HasAlias(cmdName));


            if (cmds.Count() == 0)
            {
                if (player.AwaitingResponse.ContainsKey(cmdName))
                {
                    Action<CommandArgs> call = player.AwaitingResponse[cmdName];
                    player.AwaitingResponse.Remove(cmdName);
                    call(new CommandArgs(cmdText, player, args));
                    return true;
                }
                player.SendErrorMessage("无效的命令,输入/help获取命令列表.", Specifier);
                return true;
            }
            foreach (Command cmd in cmds)
            {
                if (!cmd.AllowServer && !player.RealPlayer)
                {
                    player.SendErrorMessage("你必须在游戏中执行这个命令.");
                }
                else
                {
                    Console.WriteLine("{0}执行了{1}{2}.", player.Name, silent ? SilentSpecifier : Specifier, cmdText);
                    cmd.Run(cmdText, silent, player, args);
                }
            }
            return true;
        }

        /// <summary>
        /// Parses a string of parameters into a list. Handles quotes.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static List<String> ParseParameters(string str)
        {
            var ret = new List<string>();
            var sb = new StringBuilder();
            bool instr = false;
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                if (c == '\\' && ++i < str.Length)
                {
                    if (str[i] != '"' && str[i] != ' ' && str[i] != '\\')
                        sb.Append('\\');
                    sb.Append(str[i]);
                }
                else if (c == '"')
                {
                    instr = !instr;
                    if (!instr)
                    {
                        ret.Add(sb.ToString());
                        sb.Clear();
                    }
                    else if (sb.Length > 0)
                    {
                        ret.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                else if (IsWhiteSpace(c) && !instr)
                {
                    if (sb.Length > 0)
                    {
                        ret.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                else
                    sb.Append(c);
            }
            if (sb.Length > 0)
                ret.Add(sb.ToString());

            return ret;
        }

        private static bool IsWhiteSpace(char c)
        {
            return c == ' ' || c == '\t' || c == '\n';
        }


    }

}