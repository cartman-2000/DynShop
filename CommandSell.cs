using fr34kyn01535.Uconomy;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynShop
{
    public class CommandSell : IRocketCommand
    {
        internal static readonly string help = "Sell's an item on the shop.";
        internal static readonly string syntax = "<\"Item Name\" | ItemID | h(held item)> [amount('all' = sell all.)] || <v> <\"Vehicle Name\" | VehicleID>";
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
            get { return help; }
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
            get { return syntax; }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            {
                ItemType type = ItemType.Item;
                if (command.Length >= 1)
                    type = command[0].ToLower() == "v" ? ItemType.Vehicle : ItemType.Item;

                if (command.Length == (type == ItemType.Item ? 0 : 1) || command.Length > 2)
                {
                    UnturnedChat.Say(caller, DShop.Instance.Translate("sell_help2"));
                    return;
                }

                if (!DShop.Database.IsLoaded)
                {
                    UnturnedChat.Say(caller, DShop.Instance.Translate("db_load_error"));
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
                        {
                            UnturnedChat.Say(caller, DShop.Instance.Translate("invalid_amount"));
                            return;
                        }
                    }
                    if (count == 0)
                        count = 1;
                }


                UnturnedPlayer player = caller as UnturnedPlayer;
                if (!ushort.TryParse(command[type == ItemType.Item ? 0 : 1], out itemID))
                {
                    if (type == ItemType.Item && command[0].ToLower() == "h")
                    {
                        itemID = player.Player.equipment.itemID;
                        if (itemID == 0)
                        {
                            UnturnedChat.Say(caller, DShop.Instance.Translate("no_item_held"));
                            return;
                        }
                    }
                    else
                        itemID = type == ItemType.Item ? command[0].AssetIDFromName(type) : command[1].AssetIDFromName(type);
                }
                if (itemID.AssetFromID(type) == null)
                {
                    UnturnedChat.Say(caller, DShop.Instance.Translate("invalid_id"));
                    return;
                }
                ShopObject sObject = DShop.Database.GetItem(type, itemID);
                if (sObject.ItemID != itemID)
                {
                    Asset asset = itemID.AssetFromID(type);
                    if (type == ItemType.Item)
                        UnturnedChat.Say(caller, DShop.Instance.Translate("item_not_in_db", (asset != null && ((ItemAsset)asset).itemName != null) ? ((ItemAsset)asset).itemName : string.Empty, itemID));
                    else
                        UnturnedChat.Say(caller, DShop.Instance.Translate("item_not_in_db", (asset != null && ((VehicleAsset)asset).vehicleName != null) ? ((VehicleAsset)asset).vehicleName : string.Empty, itemID));
                    return;
                }

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
                            UnturnedChat.Say(caller, DShop.Instance.Translate("sold_items_complete_w_attatchments", actualCount, sObject.ItemName, sObject.ItemID,
                                Math.Round(totalCost, 2), moneyName, Math.Round(totalAttatchmentCost, 2), moneyName,  Math.Round(balance + totalCost, 2), moneyName));
                        else
                            UnturnedChat.Say(caller, DShop.Instance.Translate("sold_items_complete", actualCount, sObject.ItemName, sObject.ItemID, Math.Round(totalCost, 2), moneyName, Math.Round(balance + totalCost, 2), moneyName));

                    }
                    else
                    {
                        if (actualCount == 0)
                        {
                            UnturnedChat.Say(caller, DShop.Instance.Translate("no_items_sell", sObject.ItemName, sObject.ItemID));
                            return;
                        }
                        if (actualCount < count)
                        {
                            if (totalAttatchmentCost > 0)
                                UnturnedChat.Say(caller, DShop.Instance.Translate("sold_items_partial_w_attatchments", actualCount, count, sObject.ItemName, sObject.ItemID, 
                                    Math.Round(totalCost, 2), moneyName, Math.Round(totalAttatchmentCost, 2), moneyName, Math.Round(balance + totalCost, 2), moneyName));
                            else
                                UnturnedChat.Say(caller, DShop.Instance.Translate("sold_items_partial", actualCount, count, sObject.ItemName, sObject.ItemID,
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
                        UnturnedChat.Say(caller, DShop.Instance.Translate("vehicle_sell_not_allowed"));
                        return;
                    }
                    if (sVehicle.Sell(balance, player, out totalCost, out actualCount))
                    {
                        UnturnedChat.Say(caller, DShop.Instance.Translate("vehicle_sold2", sObject.ItemName, sObject.ItemID, Math.Round(totalCost, 2), moneyName, Math.Round(balance + totalCost, 2), moneyName));
                    }
                    else
                    {
                        if (actualCount == 0)
                        {
                            UnturnedChat.Say(caller, DShop.Instance.Translate("vehicel_sell_no_own", sObject.ItemName, sObject.ItemID));
                            return;
                        }
                        if (actualCount == 2)
                        {
                            UnturnedChat.Say(caller, DShop.Instance.Translate("vehicle_sell_unlocked", sObject.ItemName, sObject.ItemID));
                            return;
                        }
                        if (actualCount == 3)
                        {
                            UnturnedChat.Say(caller, DShop.Instance.Translate("vehicle_sell_to_far", sObject.ItemName, sObject.ItemID));
                            return;
                        }
                        if (actualCount == 4)
                        {
                            UnturnedChat.Say(caller, DShop.Instance.Translate("vehicle_has_player", sObject.ItemName, sObject.ItemID));
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
