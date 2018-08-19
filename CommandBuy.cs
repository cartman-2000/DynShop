using fr34kyn01535.Uconomy;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
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
            get { return "dbuy"; }
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
            ItemType type = ItemType.Item;
            if (command.Length >= 1)
                type = command[0].ToLower() == "v" ? ItemType.Vehicle : ItemType.Item;

            if (command.Length == (type == ItemType.Item ? 0 : 1) || command.Length > 2)
            {
                UnturnedChat.Say(caller, Name + " - " + Syntax);
                return;
            }

            ushort itemID = 0;
            ushort count = 1;

            if (command.Length == 2 && type == ItemType.Item)
            {
                if (!ushort.TryParse(command[type == ItemType.Item ? 1 : 2], out count))
                    UnturnedChat.Say(caller, "Invalid item count value used.");
                if (count > DShop.Instance.Configuration.Instance.MaxBuyCount)
                    count = DShop.Instance.Configuration.Instance.MaxBuyCount;
                if (count == 0)
                    count = 1;
            }



            if (!ushort.TryParse(command[type == ItemType.Item ? 0 : 1], out itemID))
                itemID = type == ItemType.Item ? command[0].AssetIDFromName(type) : command[1].AssetIDFromName(type);
            if (itemID.AssetFromID(type) == null)
            {
                UnturnedChat.Say(caller, "Invalid ID or Name entered.");
                return;
            }
            ShopObject sObject = DShop.Database.GetItem(type, itemID);
            if (sObject.ItemID != itemID)
            {
                UnturnedChat.Say(caller, string.Format("Item/Vehicle: {0}({1}) not in the shop database.", itemID.AssetFromID(type), itemID));
            }

            UnturnedPlayer player = caller as UnturnedPlayer;
            decimal balance = Uconomy.Instance.Database.GetBalance(caller.Id);

            decimal newCost = sObject.BuyCost;
            decimal totalCost = 0;
            ushort actualCount = 0;
            string moneyName = Uconomy.Instance.Configuration.Instance.MoneyName;
            if (type == ItemType.Item)
            {
                ShopItem sItem = sObject as ShopItem;
                if (sItem.Buy(balance, player, count, out newCost, out totalCost, out actualCount))
                {
                        UnturnedChat.Say(caller, string.Format("You've bought: {0} items, of: {1}({2}), your current balance is now: {3} {4}(s)", actualCount, sObject.ItemName, sObject.ItemID, Math.Round(balance - totalCost), moneyName));
                }
                else
                {
                    if (actualCount == 0)
                    {
                        UnturnedChat.Say(caller, string.Format("You don't have enough {0}(s) to buy any of: {1}(2)!", moneyName, sObject.ItemName, sObject.ItemID));
                        return;
                    }
                    if (actualCount < count)
                        UnturnedChat.Say(caller, string.Format("You only had enough to buy: {0} of {1} items, of: {2}({3}), your current balance is now: {4} {5}(s)", actualCount, count, sObject.ItemName, sObject.ItemID, Math.Round(balance - totalCost), moneyName));
                }
            }
            else
            {
                ShopVehicle sVehicle = sObject as ShopVehicle;
                if (sVehicle.Buy(balance, player, out totalCost, out actualCount))
                {

                    UnturnedChat.Say(caller, string.Format("You've bought the Vehicle: {0}({1}), your current balance is now: {2} {3}(s)", sObject.ItemName, sObject.ItemID, Math.Round(balance - totalCost), moneyName));
                }
                else
                {
                    if (actualCount == 0)
                    {
                        UnturnedChat.Say(caller, string.Format("You don't have enough {0}(s) to buy any of: {1}(2)!", moneyName, sObject.ItemName, sObject.ItemID));
                        return;
                    }
                    if (actualCount < 0)
                    {
                        UnturnedChat.Say(caller, string.Format("There was an error giving you the vehicle: {0}(1), you haven't been charged!", sObject.ItemName, sObject.ItemID));
                    }
                }
            }
        }
    }
}
