using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;
using Math = System.Math;

namespace DynShop
{
    public class DShop : RocketPlugin<DShopConfig>
    {
        public static DShop Instance;
        public DataManager Database;
        internal static bool Debug = false;

        protected override void Load()
        {
            Instance = this;
            if (Configuration.Instance.Backend == BackendType.MySQL)
                Database = new MySQLDatabaseManager();
            else
                Database = new XMLDatabaseManager();
            if (Database.IsLoaded)
                Instance.Configuration.Instance.DefaultItems();
            Instance.Configuration.Save();
            Debug = Instance.Configuration.Instance.Debug;
            if (Instance.Configuration.Instance.EnableBuySellLogging)
            {
                Instance.OnShopBuy += Instance_OnShopBuy;
                Instance.OnShopSell += Instance_OnShopSell;
            }
        }

        protected override void Unload()
        {
            Database.Unload();
            Database = null;
            if (Instance.Configuration.Instance.EnableBuySellLogging)
            {
                Instance.OnShopBuy -= Instance_OnShopBuy;
                Instance.OnShopSell -= Instance_OnShopSell;
            }
        }

        public static RaycastInfo RaycastInfoVehicle(UnturnedPlayer player, float distance)
        {
            Ray ray = new Ray(player.Player.look.aim.position, player.Player.look.aim.forward);
            return DamageTool.raycast(ray, distance, RayMasks.VEHICLE);
        }

        // Gets ItemID out of buy/cost/shop command.
        public static bool GetItemID(IRocketPlayer caller, string[] command, ItemType type, int start, out ushort itemID, bool checkValidAsset = true)
        {
            if (!ushort.TryParse(type == ItemType.Item ? command[start] : command[start + 1], out itemID))
            {
                if (!(caller is ConsolePlayer) && (type == ItemType.Item ? command[start].ToLower() == "h" : command[start + 1].ToLower() == "h"))
                {
                    UnturnedPlayer player = (UnturnedPlayer)caller;
                    if (type == ItemType.Item)
                        itemID = player.Player.equipment.itemID;
                    else if (player.IsInVehicle)
                        itemID = player.CurrentVehicle.id;
                    else if (type == ItemType.Vehicle && !player.IsInVehicle)
                    {
                        RaycastInfo raycastInfo = RaycastInfoVehicle(player, 10);
                        if (raycastInfo.vehicle != null)
                        {
                            itemID = raycastInfo.vehicle.id;
                        }
                    }
                    if (itemID == 0)
                    {
                        if (type == ItemType.Item)
                            UnturnedChat.Say(caller, Instance.Translate("no_item_held"));
                        else
                            UnturnedChat.Say(caller, Instance.Translate("no_item_held_vehicle2"));
                        return false;
                    }
                }
                else
                    itemID = type == ItemType.Item ? command[start].AssetIDFromName(type) : command[start + 1].AssetIDFromName(type);
            }
            if (checkValidAsset && itemID.AssetFromID(type) == null)
            {
                UnturnedChat.Say(caller, DShop.Instance.Translate("invalid_id"));
                return false;
            }
            return true;
        }

        public delegate void PlayerDShopBuy(decimal curBallance, UnturnedPlayer player, ushort numItems, ShopObject sObject, ItemType type, decimal newCost, decimal totalCost, short totalItems);

        public event PlayerDShopBuy OnShopBuy;

        internal void _OnShopBuy(decimal curBallance, UnturnedPlayer player, ushort numItems, ShopObject sObject, ItemType type, decimal newCost, decimal totalCost, short totalItems)
        {
            OnShopBuy?.Invoke(curBallance, player, numItems, sObject, type, newCost, totalCost, totalItems);
        }

        public delegate void PlayerDShopSell(decimal curBallance, UnturnedPlayer player, ushort numItems, ShopObject sObject, ItemType type, decimal newCost, decimal totalCost, short totalItems, decimal totalAttatchmentCost);
        public event PlayerDShopSell OnShopSell;

        internal void _OnShopSell(decimal curBallance, UnturnedPlayer player, ushort numItems, ShopObject sObject, ItemType type, decimal newCost, decimal totalCost, short totalItems, decimal totalAttatchmentCost)
        {
            OnShopSell?.Invoke(curBallance, player, numItems, sObject, type, newCost, totalCost, totalItems, totalAttatchmentCost);
        }

        // logging for sells.
        private void Instance_OnShopSell(decimal curBallance, UnturnedPlayer player, ushort numItems, ShopObject sObject, ItemType type, decimal newCost, decimal totalCost, short totalItems, decimal totalAttatchmentCost)
        {
            Logger.Log(string.Format("Player {0} [{1}] ({2}) at location: {3}, has sold {4} items, with type: {5}, with id: {6}({7}), for {8} credits, {9} credits from attatchments. Players balance is now {10} credits.",
                player.CharacterName, player.SteamName, player.CSteamID, player.IsInVehicle ? player.CurrentVehicle.transform.position.ToString() : player.Position.ToString(), totalItems, type.ToString(), sObject.ItemName, sObject.ItemID, Math.Round(totalCost, 4), Math.Round(totalAttatchmentCost, 4), Math.Round(curBallance, 2)));
        }

        // logging for buys.
        private void Instance_OnShopBuy(decimal curBallance, UnturnedPlayer player, ushort numItems, ShopObject sObject, ItemType type, decimal newCost, decimal totalCost, short totalItems)
        {
            Logger.Log(string.Format("Player {0} [{1}] ({2}) at location: {3}, has bought {4} items, with type: {5}, with id: {6}({7}), for {8} credits. Players balance is now {9} credits.",
                player.CharacterName, player.SteamName, player.CSteamID, player.IsInVehicle ? player.CurrentVehicle.transform.position.ToString() : player.Position.ToString(), totalItems, type.ToString(), sObject.ItemName, sObject.ItemID, Math.Round(totalCost, 4), Math.Round(curBallance, 2)));
        }

        public override TranslationList DefaultTranslations
        {
            get
            {
                return new TranslationList
                {
                    // Command help messages.
                    { "buy_help3", CommandBuy.syntax + " - " + CommandBuy.help },
                    { "cost_help3", CommandCost.syntax + " - " + CommandCost.help },
                    { "sell_help4", CommandSell.syntax + " - " + CommandSell.help },
                    { "shop_help", CommandDShop.syntax + " - " + CommandDShop.help },

                    // Shared messages.
                    { "invalid_id", "Invalid ID or Name entered!" },
                    { "invalid_amount", "Invalid item count value used." },
                    { "item_not_in_db", "Item/Vehicle: {0}({1}) not in the shop database." },
                    { "db_load_error", "The command can't be ran, There was an issue with loading the plugin." },
                    { "no_item_held", "There's no item being held." },
                    { "no_item_held_vehicle2", "You're currently not in, looking at, a vehicle." },

                    // Cost Command.
                    { "costs_item2", "Item: {0}({1}), Costs: {2} {3}(s) to buy and {4} {5}(s) to sell, Shop Restictions: {6}." },
                    { "costs_vehicle2", "Vehicle: {0}({1}), Costs: {2} {3}(s) to buy and {4} {5}(s) to sell, Shop Restictions: {6}." },

                    // Buy Command.
                    { "not_enough_to_buy", "You don't have enough {0}(s) to buy any of: {1}({2})!" },
                    { "bought_item_complete", "You've bought: {0} items, of: {1}({2}), for {3} {4}(s), your current balance is now: {5} {6}(s)" },
                    { "bought_item_partial", "You only had enough to buy: {0} of {1} items, of: {2}({3}), for {4} {5}(s), your current balance is now: {6} {7}(s)" },
                    { "bought_item_error", "There was an error giving you the Item: {0}({1}), you haven't been charged!" },
                    { "item_sell_only", "The item: {0}({1}), can't be bought from the shop, it's been set to sell only in the shop!" },
                    { "bought_vehicle", "You've bought the Vehicle: {0}({1}), for {2} {3}(s), your current balance is now: {4} {5}(s)" },
                    { "bought_vehicle_error", "There was an error giving you the vehicle: {0}({1}), you haven't been charged!" },
                    { "vehicle_sell_only", "The vehicle: {0}({1}), can't be bought from the shop, it's been set to sell only in the shop!" },


                    // Sell Command.
                    { "sold_items_complete", "You've sold: {0} items, of: {1}({2}), for: {3} {4}(s), your current balance is now: {5} {6}(s)" },
                    { "sold_items_complete_w_attatchments", "You've sold: {0} items, of: {1}({2}), for: {3} {4}(s) ({5} {6}(s) is from attachments.), your current balance is now: {7} {8}(s)" },
                    { "no_items_sell", "You don't have any of: {0}({1}), to sell!" },
                    { "item_buy_only", "The item: {0}({1}), can't be sold to the shop, it's been set to buy only in the shop!" },
                    { "sold_items_partial", "You only had enough to sell: {0} of {1} items, of: {2}({3}), for: {4} {5}(s), your current balance is now: {6} {7}(s)" },
                    { "sold_items_partial_w_attatchments", "You only had enough to sell: {0} of {1} items, of: {2}({3}), for: {4} {5}(s) ({6} {7}(s) is from attachments.), your current balance is now: {8} {9}(s)" },
                    { "vehicle_sell_not_allowed", "You can't sell vehicles on this server!" },
                    { "vehicel_sell_no_own2", "You haven't bought any of the vehicle you're looking at before." },
                    { "vehicle_sell_dead", "The vehicle you're looking at has been destroyed, unable to sell!" },
                    { "vehicle_sell_unlocked2", "The vehicle you're looking at isn't locked to anyone, unable to sell!" },
                    { "vehicle_sell_locked_mismatch", "The vehicle you're looking at isn't locked to you, unable to sell!" },
                    { "vehicle_sell_to_far2", "Vehicle being looked at is either too far away, or isn't a vehicle(within 10 units)!" },
                    { "vehicle_has_player2", "The vehicle that you're looking at still has players in it, unable to sell!" },
                    { "vehicle_buy_only2", "The vehicle being looked at can't be sold to the shop, it's been set to buy only in the shop!" },
                    { "vehicle_sold2", "You've sold the Vehicle: {0}({1}), for: {2} {3}(s), your current balance is now: {4} {5}(s)" },

                    // Shop command.

                    { "convert_help", "convert <mysql|xml>" },
                    { "add_help5", "add <ItemID | \"Item Name\" | h(held item)> [Cost] [SellMult] [MinBuyPrice] [ChangeRate] [MaxBuyPrice] [ShopRestrict] || add v <VehicleID | \"VehicleName\" | h(in/looking at vehicle)> [cost] [mult] [ShopRestrict]" },
                    { "remove_help3", "rem <ItemID | \"Item Name\" | h(held item)> | rem v <VehicleID | \"Vehicle Name\" | h(in/looking at vehicle)>" },
                    { "get_help3", "get <ItemID | \"Item Name\" | h(held item)> | get v <VehicleID | \"Vehicle Name\" | h(in/looking at vehicle)>" },
                    { "update_help5", "update <cost|mult|min|rate|max|sr> <ItemID | \"Item Name\" | h(held item)> <amount> | update <cost|mult|sr> v <VehicleID | \"Vehicle Name\" | h(in/looking at vehicle)> <amount>" },
                    { "update_shoprestrict_help", "Valid values are: none(0), sellonly(1), buyonly(2)" },

                    { "converting", "Converting Database to: {0}." },
                    { "conversion_success", "Database conversion Successful!" },
                    { "conversion_fail", "Database conversion Failed!" },
                    { "invalid_db_type", "Invalid database type! {0}" },

                    { "duplicate", "Warning: Duplicate found in DB, replacing." },
                    { "parse_fail_cost", "Warning: Couldn't parse the Buy Cost, using default!" },
                    { "parse_fail_mult", "Warning: Couldn't parse the Sell Multiplier, using default!" },
                    { "parse_fail_minprice", "Warning: Couldn't parse the Min Buy Price, using default!" },
                    { "parse_fail_chagerate", "Warning: Couldn't parse the Change rate, using default!" },
                    { "parse_fail_maxprice", "Warning: Couldn't parse the Max Buy Price, using default!" },
                    { "parse_fail_shoprestrict", "Warning: Couldn't parse the Sell Only value, using default!" },
                    { "item_add_fail", "Failed to add Item to Database!" },

                    { "bad_cost", "Error: Couldn't parse the Buy Cost value!" },
                    { "bad_mult", "Error: Couldn't parse the Sell Multiplier value!" },
                    { "bad_minprice", "Error: Couldn't parse the Minimum Buy Price value!" },
                    { "bad_chagerate", "Error: Couldn't parse the Change Rate value!" },
                    { "bad_maxprice", "Error: Couldn't parse the Maximum Buy Price value!" },
                    { "bad_shoprestrict", "Error: Couldn't parse the Sell Only value!" },
                    { "update_fail", "Failed to Update Database Record!" },

                    { "format_item_info_p1_addv2", "Item: {0}({1}), With Type: {2}, With BuyCost: {3}, With Sell Multiplier: {4}, With RestrictBuySell: {5}({6}){7} Added to the Database!" },
                    { "format_item_info_p1_deletev2", "Deleted Item: {0}({1}), With Type: {2}, With BuyCost: {3}, With Sell Multiplier: {4}, With RestrictBuySell: {5}({6}){7} From the Database!" },
                    { "format_item_info_p1_getv2", "Info for Item: {0}({1}), With Type: {2}, With BuyCost: {3}, With Sell Multiplier: {4}, With RestrictBuySell: {5}({6}){7}." },
                    { "format_item_info_p1_updatev2", "Updated Info for Item: {0}({1}), With Type: {2}, With BuyCost: {3}, With Sell Multiplier: {4}, With RestrictBuySell: {5}({6}){7}." },
                    { "format_item_info_p2v2", ", With Min Cost: {0}, With Change Rate: {1}, With Max Buy Price: {2}" },
                    { "item_not_in_shop_db", "Item Not in Database!" },

                    // Misc

                };
            }
        }

    }
}
