using Rocket.API;
using Rocket.Core.Logging;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynShop
{
    public class CommandDShop : IRocketCommand
    {
        internal static readonly string help = "Allows you to setup items in the dshop database.";
        internal static readonly string syntax = "<convert|add|remove(alias: rem)|get|update(alias: upd)>";

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
            get { return help; }
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
            get { return syntax; }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length == 0)
            {
                UnturnedChat.Say(caller, DShop.Instance.Translate("shop_help"));
                return;
            }

            if (!DShop.Instance.Database.IsLoaded)
            {
                UnturnedChat.Say(caller, DShop.Instance.Translate("db_load_error"));
                return;
            }

            ItemType type = ItemType.Item;
            if (command.Length >= 2 && command[1].ToLower() == "v")
                type = ItemType.Vehicle;
            ShopObject shopObject = null;
            switch (command[0].ToLower())
            {
                case "convert":
                    {
                        if (command.Length != 2)
                        {
                            UnturnedChat.Say(caller, DShop.Instance.Translate("convert_help"));
                            return;
                        }
                        BackendType backend;
                        try
                        {
                            backend = (BackendType)Enum.Parse(typeof(BackendType), command[1], true);
                            UnturnedChat.Say(caller, DShop.Instance.Translate("converting", backend.ToString()));
                            if (DShop.Instance.Database.ConvertDB(backend))
                                UnturnedChat.Say(caller, DShop.Instance.Translate("conversion_success"));
                            else
                                UnturnedChat.Say(caller, DShop.Instance.Translate("conversion_fail"));
                        }
                        catch (Exception ex)
                        {
                            UnturnedChat.Say(caller, DShop.Instance.Translate("invalid_db_type", ex.Message));
                        }
                        break;
                    }
                case "add":
                    {
                        if (command.Length < (type == ItemType.Item ? 2 : 3) || command.Length > (type == ItemType.Item ? 8 : 6))
                        {
                            UnturnedChat.Say(caller, DShop.Instance.Translate("add_help5"));
                            return;
                        }
                        if (!DShop.GetItemID(caller, command, type, 1, out ushort itemID))
                            return;
                        if (itemID.AssetFromID(type) == null)
                        {
                            UnturnedChat.Say(caller, DShop.Instance.Translate("invalid_id"));
                            return;
                        }
                        shopObject = DShop.Instance.Database.GetItem(type, itemID);
                        if (shopObject.ItemID == itemID)
                        {
                            UnturnedChat.Say(caller, DShop.Instance.Translate("duplicate"));
                        }

                        // Parse the vars used in command, if short of variables, or can't parse, defaults would be used for those vars.
                        decimal buyCost = DShop.Instance.Configuration.Instance.DefaultBuyCost;
                        if (command.Length >= (type == ItemType.Item ? 3 : 4) && !decimal.TryParse(type == ItemType.Item ? command[2] : command[3], out buyCost))
                        {
                            buyCost = DShop.Instance.Configuration.Instance.DefaultBuyCost;
                            UnturnedChat.Say(caller, DShop.Instance.Translate("parse_fail_cost"));
                        }

                        decimal sellMultiplier = DShop.Instance.Configuration.Instance.DefaultSellMultiplier;
                        if (command.Length >= (type == ItemType.Item ? 4 : 5) && !decimal.TryParse(type == ItemType.Item ? command[3] : command[4], out sellMultiplier))
                        {
                            decimal fraction = 0;
                            if ((type == ItemType.Item && command[3].IsFraction(out fraction)) || (type == ItemType.Vehicle && command[4].IsFraction(out fraction)))
                            {
                                sellMultiplier = fraction;
                            }
                            else
                            {
                                sellMultiplier = DShop.Instance.Configuration.Instance.DefaultSellMultiplier;
                                UnturnedChat.Say(caller, DShop.Instance.Translate("parse_fail_mult"));
                            }
                        }
                        decimal minBuyPrice = DShop.Instance.Configuration.Instance.MinDefaultBuyCost;
                        if (type == ItemType.Item && command.Length >= 5 && !decimal.TryParse(command[4], out minBuyPrice))
                        {
                            minBuyPrice = DShop.Instance.Configuration.Instance.MinDefaultBuyCost;
                            UnturnedChat.Say(caller, DShop.Instance.Translate("parse_fail_minprice"));
                        }

                        decimal changeRate = DShop.Instance.Configuration.Instance.DefaultIncrement;
                        if (type == ItemType.Item && command.Length >= 6 && !decimal.TryParse(command[5], out changeRate))
                        {
                            changeRate = DShop.Instance.Configuration.Instance.DefaultIncrement;
                            UnturnedChat.Say(caller, DShop.Instance.Translate("parse_fail_changerate"));
                        }

                        decimal maxBuyPrice = 0;
                        if (type == ItemType.Item && command.Length >= 7 && !decimal.TryParse(command[6], out maxBuyPrice))
                        {
                            UnturnedChat.Say(caller, DShop.Instance.Translate("parse_fail_maxprice"));
                        }

                        RestrictBuySell restrictBuySell = RestrictBuySell.None;
                        if (command.Length >= (type == ItemType.Item ? 8 : 6))
                        {
                            try
                            {
                                restrictBuySell = (RestrictBuySell)Enum.Parse(typeof(RestrictBuySell), type == ItemType.Item ? command[7] : command[5], true);
                            }
                            catch
                            {
                                restrictBuySell = RestrictBuySell.None;
                                UnturnedChat.Say(caller, DShop.Instance.Translate("parse_fail_shoprestrict"));
                            }
                        }

                        // Construct new item to add to the database.
                        shopObject = (type == ItemType.Item ? (ShopObject)new ShopItem(itemID, buyCost, sellMultiplier, minBuyPrice, changeRate, maxBuyPrice, restrictBuySell) : new ShopVehicle(itemID, buyCost, sellMultiplier, restrictBuySell));

                        if (DShop.Instance.Database.AddItem(type, shopObject))
                            UnturnedChat.Say(caller, FormatItemInfo("format_item_info_p1_addv2", shopObject, type));
                        else
                            UnturnedChat.Say(caller, DShop.Instance.Translate("item_add_fail"));
                        break;
                    }
                case "rem":
                case "remove":
                    {
                        if (command.Length != (type == ItemType.Item ? 2 : 3))
                        {
                            UnturnedChat.Say(caller, DShop.Instance.Translate("remove_help3"));
                            return;
                        }
                        if (!DShop.GetItemID(caller, command, type, 1, out ushort itemID))
                            return;
                        if (itemID.AssetFromID(type) == null)
                        {
                            UnturnedChat.Say(caller, DShop.Instance.Translate("invalid_id"));
                            return;
                        }

                        shopObject = DShop.Instance.Database.GetItem(type, itemID);

                        if (DShop.Instance.Database.DeleteItem(type, itemID))
                            UnturnedChat.Say(caller, FormatItemInfo("format_item_info_p1_deletev2", shopObject, type));
                        else
                            UnturnedChat.Say(caller, DShop.Instance.Translate("item_not_in_shop_db"));
                        break;
                    }
                case "get":
                    {
                        if (command.Length != (type == ItemType.Item ? 2 : 3))
                        {
                            UnturnedChat.Say(caller, DShop.Instance.Translate("get_help3"));
                            return;
                        }
                        if (!DShop.GetItemID(caller, command, type, 1, out ushort itemID))
                            return;
                        if (itemID.AssetFromID(type) == null)
                        {
                            UnturnedChat.Say(caller, DShop.Instance.Translate("invalid_id"));
                            return;
                        }
                        shopObject = DShop.Instance.Database.GetItem(type, itemID);
                        if (shopObject.ItemID == itemID)
                        {
                            UnturnedChat.Say(caller, FormatItemInfo("format_item_info_p1_getv2", shopObject, type));
                        }
                        else
                            UnturnedChat.Say(caller, DShop.Instance.Translate("item_not_in_shop_db"));
                        break;
                    }
                case "upd":
                case "update":
                    {
                        if (command.Length > (type == ItemType.Item ? 4 : 5) || command.Length < (type == ItemType.Item ? 2 : 4))
                        {
                            UnturnedChat.Say(caller, DShop.Instance.Translate("update_help5"));
                            return;
                        }
                        type = ItemType.Item;
                        if (command.Length >= 3 && command[2].ToLower() == "v")
                            type = ItemType.Vehicle;
                        if (command.Length == (type == ItemType.Item ? 4 : 5))
                        {
                            if (!DShop.GetItemID(caller, command, type, 2, out ushort itemID))
                                return;
                            if (itemID.AssetFromID(type) == null)
                            {
                                UnturnedChat.Say(caller, DShop.Instance.Translate("invalid_id"));
                                return;
                            }
                            shopObject = DShop.Instance.Database.GetItem(type, itemID);
                            if (shopObject.ItemID != itemID)
                            {
                                UnturnedChat.Say(caller, DShop.Instance.Translate("item_not_in_shop_db"));
                                return;
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
                                        UnturnedChat.Say(caller, DShop.Instance.Translate("bad_cost"));
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
                                        decimal fraction = 0;
                                        if ((type == ItemType.Item && command[3].IsFraction(out fraction)) || (type == ItemType.Vehicle && command[4].IsFraction(out fraction)))
                                        {
                                            sellMult = fraction;
                                        }
                                        else
                                        {
                                            UnturnedChat.Say(caller, DShop.Instance.Translate("bad_mult"));
                                            return;
                                        }
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
                                        UnturnedChat.Say(caller, DShop.Instance.Translate("bad_minprice"));
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
                                        UnturnedChat.Say(caller, DShop.Instance.Translate("bad_chagerate"));
                                        return;
                                    }
                                    ((ShopItem)shopObject).Change = rate;
                                    goto set;
                                }
                            case "max":
                                {
                                    if (command.Length != 4)
                                        goto default;

                                    decimal maxCost = 0;
                                    if (!decimal.TryParse(command[3], out maxCost))
                                    {
                                        UnturnedChat.Say(caller, DShop.Instance.Translate("bad_maxprice"));
                                        return;
                                    }
                                    ((ShopItem)shopObject).MaxBuyPrice = maxCost;
                                    goto set;
                                }
                            case "sr":
                                {
                                    if (command.Length != (type == ItemType.Item ? 4 : 5))
                                    {
                                        UnturnedChat.Say(caller, DShop.Instance.Translate("update_shoprestrict_help"));
                                        return;
                                    }

                                    RestrictBuySell restrictBuySell = RestrictBuySell.None;
                                    try
                                    {
                                        restrictBuySell = (RestrictBuySell)Enum.Parse(typeof(RestrictBuySell), type == ItemType.Item ? command[3] : command[4], true);
                                    }
                                    catch
                                    {
                                        UnturnedChat.Say(caller, DShop.Instance.Translate("bad_shoprestrict"));
                                        return;
                                    }
                                    shopObject.RestrictBuySell = restrictBuySell;
                                    goto set;

                                }
                            default:
                                {
                                    UnturnedChat.Say(caller, DShop.Instance.Translate("update_help4"));
                                    return;
                                }
                                set:
                                {
                                    if (DShop.Instance.Database.AddItem(type, shopObject))
                                        UnturnedChat.Say(caller, FormatItemInfo("format_item_info_p1_updatev2", shopObject, type));
                                    else
                                        UnturnedChat.Say(caller, DShop.Instance.Translate("update_fail"));
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
            return DShop.Instance.Translate(primaryLiteral, shopObject.ItemName, shopObject.ItemID, type.ToString(), shopObject.BuyCost, shopObject.SellMultiplier, Enum.GetName(typeof(RestrictBuySell), shopObject.RestrictBuySell), ((byte)shopObject.RestrictBuySell).ToString(),
                type == ItemType.Item ? DShop.Instance.Translate("format_item_info_p2v2", ((ShopItem)shopObject).MinBuyPrice, ((ShopItem)shopObject).Change, ((ShopItem)shopObject).MaxBuyPrice) : string.Empty);
        }
    }
}
