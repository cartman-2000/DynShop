using Rocket.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynShop
{
    public class CommandBuy : IRocketCommand
    {
        public List<string> Aliases
        {
            get { return new List<string>(); }
        }

        public AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Player; }
        }

        public string Help
        {
            get { return "Buys an item off of the shop."; }
        }

        public string Name
        {
            get { return "buy"; }
        }

        public List<string> Permissions
        {
            get { return new List<string> { "dshop.buy" }; }
        }

        public string Syntax
        {
            get { return "<\"Item Name\" | ItemID> [amount] | <v> <\"Vehicle Name\" | VehicleID>"; }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {

        }
    }
}
