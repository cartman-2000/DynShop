using Rocket.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynShop
{
    public class CommandSell : IRocketCommand
    {
        public List<string> Aliases
        {
            get { return new List<string>(); }
        }

        public AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Both; }
        }

        public string Help
        {
            get { return "Sell's an item on the shop."; }
        }

        public string Name
        {
            get { return "sell"; }
        }

        public List<string> Permissions
        {
            get { return new List<string> { "dshop.sell" }; }
        }

        public string Syntax
        {
            get { return "<\"Item Name\" | ItemID> [amount]"; }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {

        }
    }
}
