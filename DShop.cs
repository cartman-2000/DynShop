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

        protected override void Load()
        {
            Instance = this;
            if (Configuration.Instance.Backend == BackendType.MySQL)
                Database = new MySQLDatabaseManager();
            else
                Database = new XMLDatabaseManager();
        }

        protected override void Unload()
        {
            Database.Unload();
            Database = null;
        }

        public delegate void PlayerDShopBuy(UnturnedPlayer player, decimal amt, ushort numItems, ushort itemID, ItemType type = ItemType.Item);
        public event PlayerDShopBuy OnShopBuy;
        public delegate void PlayerDShopSell(UnturnedPlayer player, decimal amt, ushort numItems, ushort itemID, ItemType type = ItemType.Item);
        public event PlayerDShopSell OnShopSell;

    }
}
