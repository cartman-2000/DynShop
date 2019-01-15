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
    public class CommandSell : IRocketCommand
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
            get { return "Sell's an item on the shop."; }
        }

        public string Name
        {
            get { return "dsell"; }
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
            {
                ItemType type = ItemType.Item;
                if (command.Length >= 1)
                    type = command[0].ToLower() == "v" ? ItemType.Vehicle : ItemType.Item;

                if (command.Length == (type == ItemType.Item ? 0 : 1) || command.Length > 2)
                {
                    UnturnedChat.Say(caller, Name + " - " + Syntax);
                    return;
                }

                if (!DShop.Database.IsLoaded)
                {
                    UnturnedChat.Say(caller, "The command can't be ran, There was an issue with loading the plugin.");
                    return;
                }

                ushort itemID = 0;
                ushort count = 1;

                if (command.Length == 2 && type == ItemType.Item)
                {
                    if (!ushort.TryParse(command[1], out count))
                    {
                        if (command[1].ToLower() == "all")
                            count = ushort.MaxValue;
                        else
                            UnturnedChat.Say(caller, "Invalid item count value used.");
                    }
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
                decimal totalAttatchmentCost = 0;
                string moneyName = Uconomy.Instance.Configuration.Instance.MoneyName;

                if (type == ItemType.Item)
                {
                    ShopItem sItem = sObject as ShopItem;
                    if (sItem.Sell(balance, player, count, out newCost, out totalCost, out actualCount, out totalAttatchmentCost))
                    {
                        if (totalAttatchmentCost > 0)
                            UnturnedChat.Say(caller, string.Format("You've sold: {0} items, of: {1}({2}), for: {3} {4}(s) ({5} {6}(s) is from attachments.) , your current balance is now: {7} {8}(s)", actualCount, sObject.ItemName, sObject.ItemID,
                                Math.Round(totalCost, 2), moneyName, Math.Round(totalAttatchmentCost, 2), moneyName,  Math.Round(balance + totalCost, 2), moneyName));
                        else
                            UnturnedChat.Say(caller, string.Format("You've sold: {0} items, of: {1}({2}), for: {3} {4}(s) , your current balance is now: {5} {6}(s)", actualCount, sObject.ItemName, sObject.ItemID, Math.Round(totalCost, 2), moneyName, Math.Round(balance + totalCost, 2), moneyName));

                    }
                    else
                    {
                        if (actualCount == 0)
                        {
                            UnturnedChat.Say(caller, string.Format("You don't have any of: {0}({1}), to sell!", sObject.ItemName, sObject.ItemID));
                            return;
                        }
                        if (actualCount < count)
                        {
                            if (totalAttatchmentCost > 0)
                                UnturnedChat.Say(caller, string.Format("You only had enough to sell: {0} of {1} items, of: {2}({3}), for: {4} {5}(s) ({6} {7}(s) is from attachments.), your current balance is now: {8} {9}(s)", actualCount, count, sObject.ItemName, sObject.ItemID, 
                                    Math.Round(totalCost, 2), moneyName, Math.Round(totalAttatchmentCost, 2), moneyName, Math.Round(balance + totalCost, 2), moneyName));
                            else
                                UnturnedChat.Say(caller, string.Format("You only had enough to sell: {0} of {1} items, of: {2}({3}), for: {4} {5}(s), your current balance is now: {6} {7}(s)", actualCount, count, sObject.ItemName, sObject.ItemID,
    Math.Round(totalCost, 2), moneyName, Math.Round(balance + totalCost, 2), moneyName));


                        }
                    }
                }
                else
                {
                    ShopVehicle sVehicle = sObject as ShopVehicle;
                    // placeholder code until the vehicle buy/sell tracking update.
                    if (!DShop.Instance.Configuration.Instance.CanSellVehicles)
                    {
                        UnturnedChat.Say(caller, "You can't sell vehicles on this server!");
                        return;
                    }
                    if (sVehicle.Sell(balance, player, out totalCost, out actualCount))
                    {
                        UnturnedChat.Say(caller, string.Format("You've sold the Vehicle: {0}({1}), your current balance is now: {2} {3}(s)", sObject.ItemName, sObject.ItemID, Math.Round(balance + totalCost, 2), moneyName));
                    }
                    else
                    {
                        if (actualCount == 0)
                        {
                            UnturnedChat.Say(caller, string.Format("You don't own any of: {0}({1}), to sell, on this map!", sObject.ItemName, sObject.ItemID));
                            return;
                        }
                        if (actualCount == 2)
                        {
                            UnturnedChat.Say(caller, string.Format("You don't have any of: {0}({1}), locked to you on the map!", sObject.ItemName, sObject.ItemID));
                            return;
                        }
                        if (actualCount == 3)
                        {
                            UnturnedChat.Say(caller, string.Format("There was an error selliing your vehicle: {0}({1}), you need to be standing next to it(within 10 units)!", sObject.ItemName, sObject.ItemID));
                            return;
                        }
                    }
                }
                if (totalCost > 0)
                    Uconomy.Instance.Database.IncreaseBalance(caller.Id, (Math.Round(totalCost, 2)));
            }
        }
    }
}
