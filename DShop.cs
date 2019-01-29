using Rocket.API.Collections;
using Rocket.Core.Plugins;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynShop
{
    public class DShop : RocketPlugin<DShopConfig>
    {
        public static DShop Instance;
        internal static DataManager Database;
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
        }

        protected override void Unload()
        {
            Database.Unload();
            Database = null;
        }

        public delegate void PlayerDShopBuy(decimal curBallance, UnturnedPlayer player, ushort numItems, ShopObject sObject, ItemType type, decimal newCost, decimal totalCost, ushort totalItems);

        public event PlayerDShopBuy OnShopBuy;

        internal void _OnShopBuy(decimal curBallance, UnturnedPlayer player, ushort numItems, ShopObject sObject, ItemType type, decimal newCost, decimal totalCost, ushort totalItems)
        {
            OnShopBuy?.Invoke(curBallance, player, numItems, sObject, type, newCost, totalCost, totalItems);
        }

        public delegate void PlayerDShopSell(decimal curBallance, UnturnedPlayer player, ushort numItems, ShopObject sObject, ItemType type, decimal newCost, decimal totalCost, ushort totalItems, decimal totalAttatchmentCost);
        public event PlayerDShopSell OnShopSell;

        internal void _OnShopSell(decimal curBallance, UnturnedPlayer player, ushort numItems, ShopObject sObject, ItemType type, decimal newCost, decimal totalCost, ushort totalItems, decimal totalAttatchmentCost)
        {
            OnShopSell?.Invoke(curBallance, player, numItems, sObject, type, newCost, totalCost, totalItems, totalAttatchmentCost);
        }

        public override TranslationList DefaultTranslations
        {
            get
            {
                return new TranslationList
                {
                    // Command help messages.
                    { "buy_help2", CommandBuy.syntax + " - " + CommandBuy.help },
                    { "cost_help2", CommandCost.syntax + " - " + CommandCost.help },
                    { "sell_help2", CommandSell.syntax + " - " + CommandSell.help },
                    { "shop_help", CommandDShop.syntax + " - " + CommandDShop.help },

                    // Shared messages.
                    { "invalid_id", "Invalid ID or Name entered!" },
                    { "invalid_amount", "Invalid item count value used." },
                    { "item_not_in_db", "Item/Vehicle: {0}({1}) not in the shop database." },
                    { "db_load_error", "The command can't be ran, There was an issue with loading the plugin." },
                    { "no_item_held", "There's no item being held." },
                    { "no_item_held_vehicle", "You're currently not in a vehicle." },

                    // Cost Command.
                    { "costs_item", "Item: {0}({1}), Costs: {2} {3}(s) to buy and {4} {5}(s) to sell." },
                    { "costs_vehicle", "Vehicle: {0}({1}), Costs: {2} {3}(s) to buy and {4} {5}(s) to sell." },

                    // Buy Command.
                    { "not_enough_to_buy", "You don't have enough {0}(s) to buy any of: {1}({2})!" },
                    { "bought_item_complete", "You've bought: {0} items, of: {1}({2}), for {3} {4}(s), your current balance is now: {5} {6}(s)" },
                    { "bought_item_partial", "You only had enough to buy: {0} of {1} items, of: {2}({3}), for {4} {5}(s), your current balance is now: {6} {7}(s)" },
                    { "bought_vehicle", "You've bought the Vehicle: {0}({1}), for {2} {3}(s), your current balance is now: {4} {5}(s)" },
                    { "bought_vehicle_error", "There was an error giving you the vehicle: {0}({1}), you haven't been charged!" },

                    // Sell Command.
                    { "sold_items_complete", "You've sold: {0} items, of: {1}({2}), for: {3} {4}(s), your current balance is now: {5} {6}(s)" },
                    { "sold_items_complete_w_attatchments", "You've sold: {0} items, of: {1}({2}), for: {3} {4}(s) ({5} {6}(s) is from attachments.), your current balance is now: {7} {8}(s)" },
                    { "no_items_sell", "You don't have any of: {0}({1}), to sell!" },
                    { "sold_items_partial", "You only had enough to sell: {0} of {1} items, of: {2}({3}), for: {4} {5}(s), your current balance is now: {6} {7}(s)" },
                    { "sold_items_partial_w_attatchments", "You only had enough to sell: {0} of {1} items, of: {2}({3}), for: {4} {5}(s) ({6} {7}(s) is from attachments.), your current balance is now: {8} {9}(s)" },
                    { "vehicle_sell_not_allowed", "You can't sell vehicles on this server!" },
                    { "vehicel_sell_no_own", "You don't own any of: {0}({1}), to sell, on this map!" },
                    { "vehicle_sell_unlocked", "You don't have any of: {0}({1}), locked to you on the map!" },
                    { "vehicle_sell_to_far", "There was an error selliing your vehicle: {0}({1}), you need to be standing next to it(within 10 units)!" },
                    { "vehicle_has_player", "The vehicle: {0}({1}), has players in it, you can't sell it until they exit the vehicle!" },
                    { "vehicle_sold2", "You've sold the Vehicle: {0}({1}), for: {2} {3}(s), your current balance is now: {4} {5}(s)" },

                    // Shop command.

                    { "convert_help", "convert <mysql|xml>" },
                    { "add_help3", "add <ItemID | \"Item Name\" | h(held item)> [Cost] [SellMult] [MinBuyPrice] [ChangeRate] [MaxBuyPrice] || add v <VehicleID | \"VehicleName\" | h(in vehicle)> [cost] [mult]" },
                    { "remove_help2", "rem <ItemID | \"Item Name\" | h(held item)> | rem v <VehicleID | \"Vehicle Name\" | h(in vehicle)>" },
                    { "get_help2", "get <ItemID | \"Item Name\" | h(held item)> | get v <VehicleID | \"Vehicle Name\" | h(in vehicle)>" },
                    { "update_help3", "update <cost|mult|min|rate|max> <ItemID | \"Item Name\" | h(held item)> <amount> | update <cost|mult> v <VehicleID | \"Vehicle Name\" | h(in vehicle)> <amount>" },

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
                    { "item_add_fail", "Failed to add Item to Database!" },

                    { "bad_cost", "Error: Couldn't parse the Buy Cost value!" },
                    { "bad_mult", "Error: Couldn't parse the Sell Multiplier value!" },
                    { "bad_minprice", "Error: Couldn't parse the Minimum Buy Price value!" },
                    { "bad_chagerate", "Error: Couldn't parse the Change Rate value!" },
                    { "bad_maxprice", "Error: Couldn't parse the Maximum Buy Price value!" },
                    { "update_fail", "Failed to Update Database Record!" },

                    { "format_item_info_p1_add", "Item: {0}({1}), With Type: {2}, With BuyCost: {3}, With Sell Multiplier: {4}{5} Added to the Database!" },
                    { "format_item_info_p1_delete", "Deleted Item: {0}({1}), With Type: {2}, With BuyCost: {3}, With Sell Multiplier: {4}{5} From the Database!" },
                    { "format_item_info_p1_get", "Info for Item: {0}({1}), With Type: {2}, With BuyCost: {3}, With Sell Multiplier: {4}{5}." },
                    { "format_item_info_p1_update", "Updated Info for Item: {0}({1}), With Type: {2}, With BuyCost: {3}, With Sell Multiplier: {4}{5}." },
                    { "format_item_info_p2v2", ", With Min Cost: {0}, With Change Rate: {1}, With Max Buy Price: {2}" },
                    { "item_not_in_shop_db", "Item Not in Database!" },

                    // Misc

                };
            }
        }

    }
}
