using I18N.West;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynShop
{
    internal class MySQLDatabaseManager : DataManager
    {
        private MySqlConnection Connection = null;

        public int SchemaVersion {get {return 1;} }

        internal MySQLDatabaseManager()
        {
            CP1250 cP1250 = new CP1250();
            CheckSchema();
        }

        public void CheckSchema()
        {

        }

        private MySqlConnection CreateConnection()
        {
            return new MySqlConnection();
        }

        public void Unload()
        {
            throw new NotImplementedException();
        }

        public bool AddItem(ItemType type, ShopObject item)
        {
            throw new NotImplementedException();
        }

        public ShopObject GetItem(ItemType type, ushort itemID)
        {
            throw new NotImplementedException();
        }

        public bool DeleteItem(ItemType type, ushort itemID)
        {
            throw new NotImplementedException();
        }
    }
}
