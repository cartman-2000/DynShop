using fr34kyn01535.Uconomy;
using Rocket.API;
using Rocket.Unturned.Chat;
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
            get { return "dcost"; }
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
            ItemType type = ItemType.Item;
            if (command.Length >= 1)
                type = command[0].ToLower() == "v" ? ItemType.Vehicle : ItemType.Item;

            if (command.Length == (type == ItemType.Item ? 0 : 1) || command.Length > (type == ItemType.Item ? 1 : 2))
            {
                UnturnedChat.Say(caller, Name + " - " + Syntax);
                return;
            }

            ushort itemID = 0;

            if (!ushort.TryParse(type == ItemType.Item ? command[0] : command[1], out itemID))
                itemID = type == ItemType.Item ? command[0].AssetIDFromName(type) : command[1].AssetIDFromName(type);
            if (itemID.AssetFromID(type) == null)
            {
                UnturnedChat.Say(caller, "Invalid ID or Name entered.");
                return;
            }
            ShopObject shopObject = DShop.Database.GetItem(type, itemID);
            if (shopObject.ItemID != itemID)
            {
                UnturnedChat.Say(caller, string.Format("Item/Vehicle: {0}({1}) not in the shop database.",itemID.AssetFromID(type), itemID));
            }

            if (type == ItemType.Item)
                UnturnedChat.Say(caller, string.Format("Item: {0}({1}), Costs: {2} {3}(s) to buy and {4} {5}(s) to sell.", shopObject.ItemName, shopObject.ItemID, Math.Round(shopObject.BuyCost, 2), Uconomy.Instance.Configuration.Instance.MoneyName,
                    Math.Round(decimal.Multiply(shopObject.BuyCost, ((ShopItem)shopObject).SellMultiplier), 2), Uconomy.Instance.Configuration.Instance.MoneyName));
            else
                UnturnedChat.Say(caller, string.Format("Vehicle: {0}({1}), Costs: {2} {3}(s) to buy.", shopObject.ItemName, shopObject.ItemID, Math.Round(shopObject.BuyCost, 2), Uconomy.Instance.Configuration.Instance.MoneyName));
        }
    }
}
