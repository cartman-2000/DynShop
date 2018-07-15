using Rocket.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynShop
{
    public class CommandCost : IRocketCommand
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
            get { return "Displays the cost of an item."; }
        }

        public string Name
        {
            get { return "cost"; }
        }

        public List<string> Permissions
        {
            get { return new List<string> { "dshop.cost" }; }
        }

        public string Syntax
        {
            get { return "<\"Item Name\" | ItemID> | <v> <\"Vehicle Name\" | VehicleID>"; }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {

        }
    }
}
