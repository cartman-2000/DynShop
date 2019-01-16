using Rocket.API;
using Rocket.Unturned.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynShop
{
    public class CommandDShop : IRocketCommand
    {
        internal CommandDShop Instance;
        public CommandDShop()
        {
            Instance = this;
        }

//        internal static readonly string help = "";
//        internal static readonly string syntax = "";

        public List<string> Aliases
        {
            get { return new List<string>(); }
        }

        public AllowedCaller AllowedCaller
        {
            get{ return AllowedCaller.Both; }
        }

        public string Help
        {
            get { return ""; }
        }

        public string Name
        {
            get { return "shop"; }
        }

        public List<string> Permissions
        {
            get { return new List<string> { "dshop.shop" }; }
        }

        public string Syntax
        {
            get { return "<convert|add|remove(alias: rem)|get|update(alias: upd)>"; }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length == 0)
            {
                UnturnedChat.Say(caller, Name + " - " + Syntax);
                return;
            }

            if (!DShop.Database.IsLoaded)
            {
                UnturnedChat.Say(caller, "The command can't be ran, There was an issue with loading the plugin.");
                return;
            }

            ItemType type = ItemType.Item;
            if (command.Length >= 2 && command[1].ToLower() == "v")
                type = ItemType.Vehicle;
            ShopObject shopObject = null;
            ushort itemID = 0;
            switch (command[0].ToLower())
            {
                case "convert":
                    {
                        if (command.Length != 2)
                        {
                            UnturnedChat.Say(caller, "convert <mysql|xml>");
                            return;
                        }
                        BackendType backend;
                        try
                        {
                            backend = (BackendType)Enum.Parse(typeof(BackendType), command[1], true);
                            UnturnedChat.Say(caller, "Converting Database to: " + backend.ToString());
                            if (DShop.Database.ConvertDB(backend))
                                UnturnedChat.Say(caller, "Database conversion Failed!");
                            else
                                UnturnedChat.Say(caller, "Database conversion Successful!");
                        }
                        catch (Exception ex)
                        {
                            UnturnedChat.Say(caller, "Invalid database type!" + ex.Message);
                        }
                        break;
                    }
                case "add":
                    {
                        if (command.Length < (type == ItemType.Item ? 2 : 3) || command.Length > (type == ItemType.Item ? 6 : 5))
                        {
                            UnturnedChat.Say(caller, "add <ItemID|\"Item Name\"> [Cost] [SellMult] [MinBuyPrice] [ChangeRate] || add v <VehicleID| \"VehicleName\"> [cost]");
                            return;
                        }
                        if (!ushort.TryParse(type == ItemType.Item ? command[1] : command[2], out itemID))
                            itemID = type == ItemType.Item ? command[1].AssetIDFromName(type) : command[2].AssetIDFromName(type);
                        if (itemID.AssetFromID(type) == null)
                        {
                            UnturnedChat.Say(caller, "Invalid ID or Name entered.");
                            return;
                        }
                        shopObject = DShop.Database.GetItem(type, itemID);
                        if (shopObject.ItemID == itemID)
                        {
                            UnturnedChat.Say(caller, "Warning: Duplicate found in DB, replacing.");
                        }

                        // Parse the vars used in command, if short of variables, or can't parse, defaults would be used for those vars.
                        decimal buyCost = DShop.Instance.Configuration.Instance.DefaultBuyCost;
                        if (command.Length >= (type == ItemType.Item ? 3 : 4) && !decimal.TryParse(type == ItemType.Item ? command[2] : command[3], out buyCost))
                            UnturnedChat.Say(caller, "Warning: Couldn't parse the Buy Cost, using default!");

                        decimal sellMultiplier = DShop.Instance.Configuration.Instance.DefaultSellMultiplier;
                        if (command.Length >= (type == ItemType.Item ? 4 : 5) && !decimal.TryParse(type == ItemType.Item ? command[3] : command[4], out sellMultiplier))
                            UnturnedChat.Say(caller, "Warning: Couldn't parse the Sell Multiplier, using default!");

                        decimal minBuyPrice = DShop.Instance.Configuration.Instance.MinDefaultBuyCost;
                        if (type == ItemType.Item && command.Length >= 5 && !decimal.TryParse(command[4], out minBuyPrice))
                            UnturnedChat.Say(caller, "Warning: Couldn't parse the Min Buy Price, using default!");

                        decimal changeRate = DShop.Instance.Configuration.Instance.DefaultIncrement;
                        if (type == ItemType.Item && command.Length == 6 && !decimal.TryParse(command[5], out changeRate))
                            UnturnedChat.Say(caller, "Warning: Couldn't parse the Change rate, using default!");

                        // Construct new item to add to the database.
                        shopObject = (type == ItemType.Item ? (ShopObject)new ShopItem(itemID, buyCost, sellMultiplier, minBuyPrice, changeRate) : new ShopVehicle(itemID, buyCost, sellMultiplier));

                        if (DShop.Database.AddItem(type, shopObject))
                        {
                            UnturnedChat.Say(caller, FormatItemInfo("Item: {0}({1}), With Type: {2}, With BuyCost: {3}, With Sell Multiplier: {4}{5} Added to the Database!", shopObject, type));
                        }
                        else
                            UnturnedChat.Say(caller, "Failed to add Item to Database!");
                        break;
                    }
                case "rem":
                case "remove":
                    {
                        if (command.Length < (type == ItemType.Item ? 2 : 3) || command.Length > (type == ItemType.Item ? 2 : 3))
                        {
                            UnturnedChat.Say(caller, "rem <ItemID|\"Item Name\"> | rem v <VehicleID|\"Vehicle Name\">");
                            return;
                        }
                        if (!ushort.TryParse(type == ItemType.Item ? command[1] : command[2], out itemID))
                            itemID = type == ItemType.Item ? command[1].AssetIDFromName(type) : command[2].AssetIDFromName(type);
                        if (itemID.AssetFromID(type) == null)
                        {
                            UnturnedChat.Say(caller, "Invalid ID or Name entered.");
                            return;
                        }

                        shopObject = DShop.Database.GetItem(type, itemID);

                        if (DShop.Database.DeleteItem(type, itemID))
                            UnturnedChat.Say(caller, FormatItemInfo("Deleted Item: {0}({1}), With Type: {2}, With BuyCost: {3}, With Sell Multiplier: {4}{5} From the Database!", shopObject, type));
                        else
                            UnturnedChat.Say(caller, "Item Not in Database!");

                        break;
                    }
                case "get":
                    {
                        if (command.Length < (type == ItemType.Item ? 2 : 3) || command.Length > (type == ItemType.Item ? 2 : 3))
                        {
                            UnturnedChat.Say(caller, "get <ItemID|\"Item Name\"> | get v <VehicleID|\"Vehicle Name\">");
                            return;
                        }
                        if (!ushort.TryParse(type == ItemType.Item ? command[1] : command[2], out itemID))
                            itemID = type == ItemType.Item ? command[1].AssetIDFromName(type) : command[2].AssetIDFromName(type);
                        if (itemID.AssetFromID(type) == null)
                        {
                            UnturnedChat.Say(caller, "Invalid ID or Name entered.");
                            return;
                        }
                        shopObject = DShop.Database.GetItem(type, itemID);
                        if (shopObject.ItemID == itemID)
                        {
                            UnturnedChat.Say(caller, FormatItemInfo("Info for Item: {0}({1}), With Type: {2}, With BuyCost: {3}, With Sell Multiplier: {4}{5}.", shopObject, type));
                        }
                        else
                            UnturnedChat.Say(caller, "Item Not in Database!");
                        break;
                    }
                case "upd":
                case "update":
                    {
                        if (command.Length < 3)
                        {
                            UnturnedChat.Say(caller, "update <cost|mult|min|rate> <ItemID|\"Item Name\"> <amount> | update cost v <VehicleID|\"Vehicle Name\"> <amount>");
                            return;
                        }
                        if (command.Length == (type == ItemType.Item ? 4 : 5))
                        {
                            type = ItemType.Item;
                            if (command.Length >= 3 && command[2].ToLower() == "v")
                                type = ItemType.Vehicle;
                            if (!ushort.TryParse(type == ItemType.Item ? command[2] : command[3], out itemID))
                                itemID = type == ItemType.Item ? command[2].AssetIDFromName(type) : command[3].AssetIDFromName(type);
                            if (itemID.AssetFromID(type) == null)
                            {
                                UnturnedChat.Say(caller, "Invalid ID or Name entered.");
                                return;
                            }
                            shopObject = DShop.Database.GetItem(type, itemID);
                            if (shopObject.ItemID != itemID)
                            {
                                UnturnedChat.Say(caller, "Item Not in Database!");
                            }
                        }
                        switch (command[1].ToLower())
                        {
                            case "cost":
                                {
                                    if (command.Length != (type == ItemType.Item ? 4 : 5))
                                        goto default;

                                    decimal buyCost = DShop.Instance.Configuration.Instance.DefaultBuyCost;
                                    if (!decimal.TryParse(type == ItemType.Item ? command[3] : command[4], out buyCost))
                                    {
                                        UnturnedChat.Say(caller, "Error: Couldn't parse the Buy Cost value!");
                                        return;
                                    }
                                    shopObject.BuyCost = buyCost;
                                    goto set;
                                }
                            case "mult":
                                {
                                    if (command.Length != (type == ItemType.Item ? 4 : 5))
                                        goto default;

                                    decimal sellMult = DShop.Instance.Configuration.Instance.DefaultSellMultiplier;
                                    if (!decimal.TryParse(type == ItemType.Item ? command[3] : command[4], out sellMult))
                                    {
                                        UnturnedChat.Say(caller, "Error: Couldn't parse the Sell Multiplier value!");
                                        return;
                                    }
                                    shopObject.SellMultiplier = sellMult;
                                    goto set;
                                }
                            case "min":
                                {
                                    if (command.Length != 4)
                                        goto default;

                                    decimal minCost = DShop.Instance.Configuration.Instance.MinDefaultBuyCost;
                                    if (!decimal.TryParse(command[3], out minCost))
                                    {
                                        UnturnedChat.Say(caller, "Error: Couldn't parse the Minimum Buy Price value!");
                                        return;
                                    }
                                    ((ShopItem)shopObject).MinBuyPrice = minCost;
                                    goto set;
                                }
                            case "rate":
                                {
                                    if (command.Length != 4)
                                        goto default;

                                    decimal rate = DShop.Instance.Configuration.Instance.DefaultIncrement;
                                    if (!decimal.TryParse(command[3], out rate))
                                    {
                                        UnturnedChat.Say(caller, "Error: Couldn't parse the Minimum Buy Price value!");
                                        return;
                                    }
                                    ((ShopItem)shopObject).Change = rate;
                                    goto set;
                                }
                            default:
                                {
                                    UnturnedChat.Say(caller, "update <cost|mult|min|rate> <ItemID|\"Item Name\"> <amount> | update cost v <VehicleID|\"Vehicle Name\"> <amount>");
                                    return;
                                }
                            set:
                                {
                                    if (DShop.Database.AddItem(type, shopObject))
                                        UnturnedChat.Say(caller, FormatItemInfo("Updated Info for Item: {0}({1}), With Type: {2}, With BuyCost: {3}, With Sell Multiplier: {4}{5}.", shopObject, type));
                                    else
                                        UnturnedChat.Say(caller, "Failed to Update Database Record!");
                                    break;
                                }
                        }

                        break;
                    }
                default:
                    UnturnedChat.Say(caller, Syntax);
                    return;
            }
        }

        public string FormatItemInfo(string primaryLiteral, ShopObject shopObject, ItemType type)
        {
            return string.Format(primaryLiteral, shopObject.ItemName, shopObject.ItemID, type.ToString(), shopObject.BuyCost, shopObject.SellMultiplier, 
                type == ItemType.Item ? string.Format(", With Min Cost: {0}, With Change Rate: {1}", ((ShopItem)shopObject).MinBuyPrice, ((ShopItem)shopObject).Change) : string.Empty);
        }
    }
}
