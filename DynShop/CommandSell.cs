using fr34kyn01535.Uconomy;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Math = System.Math;

namespace DynShop
{
    public class CommandSell : IRocketCommand
    {
        internal static readonly string help = "Sell's an item on the shop.";
        internal static readonly string syntax = "<\"Item Name\" | ItemID | h(held item)> [amount('all'|'a' = sell all.)] || <v> (While looking at a vehicle)";
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
                RaycastInfo raycastInfo = null;
                InteractableVehicle vehicle = null;
                if (command.Length >= 1)
                    type = command[0].ToLower() == "v" ? ItemType.Vehicle : ItemType.Item;

                if (command.Length == 0 || command.Length > (type == ItemType.Item ? 2 : 1))
                {
                    UnturnedChat.Say(caller, DShop.Instance.Translate("sell_help4"));
                    return;
                }

                if (!DShop.Instance.Database.IsLoaded)
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
                        if (command[1].ToLower() == "all" || command[1].ToLower() == "a")
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
                if (!ushort.TryParse(command[0], out itemID))
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
                    else if (type == ItemType.Item)
                        itemID = command[0].AssetIDFromName(type);
                    else if (type == ItemType.Vehicle)
                    {
                        // Try to grab the vehicle that the player is looking at.
                        raycastInfo = DShop.RaycastInfoVehicle(player, 10);
                        if (raycastInfo.vehicle == null)
                        {
                            UnturnedChat.Say(caller, DShop.Instance.Translate("vehicle_sell_to_far2"));
                            return;
                        }
                        else
                        {
                            itemID = raycastInfo.vehicle.id;
                            vehicle = raycastInfo.vehicle;
                            // Run checks before accepting this vehicle to run through ShopVehicle.sell.
                            if (vehicle.isDead)
                            {
                                // Don't allow a destroyed vehicle to be sold.
                                UnturnedChat.Say(caller, DShop.Instance.Translate("vehicle_sell_dead"));
                                return;
                            }
                            if (!vehicle.isLocked)
                            {
                                // Vehicle isn't locked to any player.
                                UnturnedChat.Say(caller, DShop.Instance.Translate("vehicle_sell_unlocked2"));
                                return;
                            }
                            if (vehicle.isLocked && vehicle.lockedOwner != player.CSteamID)
                            {
                                // This vehicle isn't locked to this player.
                                UnturnedChat.Say(caller, DShop.Instance.Translate("vehicle_sell_locked_mismatch"));
                                return;
                            }
                            if (!vehicle.isEmpty)
                            {
                                // The vehicle still has players in it, don't sell.
                                UnturnedChat.Say(caller, DShop.Instance.Translate("vehicle_has_player2"));
                                return;
                            }
                        }
                    }
                }
                if (itemID.AssetFromID(type) == null)
                {
                    UnturnedChat.Say(caller, DShop.Instance.Translate("invalid_id"));
                    return;
                }
                ShopObject sObject = DShop.Instance.Database.GetItem(type, itemID);
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
                short actualCount = 0;
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
                        if (actualCount == -1)
                        {
                            UnturnedChat.Say(caller, DShop.Instance.Translate("item_buy_only", sObject.ItemName, sObject.ItemID));
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
                    if (sVehicle.Sell(balance, player, raycastInfo, out totalCost, out actualCount))
                    {
                        UnturnedChat.Say(caller, DShop.Instance.Translate("vehicle_sold2", sObject.ItemName, sObject.ItemID, Math.Round(totalCost, 2), moneyName, Math.Round(balance + totalCost, 2), moneyName));
                    }
                    else
                    {
                        if (actualCount == -1)
                        {
                            UnturnedChat.Say(caller, DShop.Instance.Translate("vehicle_buy_only2"));
                            return;
                        }
                        if (actualCount == -2)
                        {
                            UnturnedChat.Say(caller, DShop.Instance.Translate("vehicel_sell_no_own2"));
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
