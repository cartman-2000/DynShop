using Rocket.API;
using Rocket.Unturned.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynShop
{
    public class CommandDShop : IRocketCommand
    {
        internal CommandDShop Instance;
        public CommandDShop()
        {
            Instance = this;
        }

//        internal static readonly string help = "";
//        internal static readonly string syntax = "";

        public List<string> Aliases
        {
            get { return new List<string>(); }
        }

        public AllowedCaller AllowedCaller
        {
            get{ return AllowedCaller.Both; }
        }

        public string Help
        {
            get { return ""; }
        }

        public string Name
        {
            get { return "dshop"; }
        }

        public List<string> Permissions
        {
            get { return new List<string> { "dshop.dshop" }; }
        }

        public string Syntax
        {
            get { return ""; }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length == 0)
            {
                UnturnedChat.Say(caller, Name + " - " + Syntax);
                return;
            }
            switch (command[0].ToLower())
            {
                case "convert":
                    {
                        if (command.Length != 2)
                        {
                            return;
                        }
                        BackendType backend;
                        try
                        {
                            backend = (BackendType)Enum.Parse(typeof(BackendType), command[1], true);
                            DShop.Database.ConvertDB(backend);
                        }
                        catch
                        {
                            
                        }
                        break;
                    }
                case "add":
                    {
                        break;
                    }
                case "remove":
                    {
                        break;
                    }
                case "get":
                    {
                        break;
                    }
                case "update":
                    {
                        break;
                    }
            }
        }
    }
}
