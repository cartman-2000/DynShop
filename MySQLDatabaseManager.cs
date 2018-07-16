using I18N.West;
using MySql.Data.MySqlClient;
using Rocket.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynShop
{
    internal class MySQLDatabaseManager : DataManager
    {
        private MySqlConnection Connection = null;

        private string Prefix;
        private string TableConfig;
        private string TableItems;
        private string TableVehicles;

        public bool IsLoaded { get; set; }

        public BackendType Backend { get { return BackendType.MySQL; } }

        internal MySQLDatabaseManager()
        {
            CP1250 cP1250 = new CP1250();
            Prefix = DShop.Instance.Configuration.Instance.DatabaseTablePrefix;
            TableConfig = Prefix + "_config";
            TableItems = Prefix + "_items";
            TableVehicles = Prefix + "_vehicles";
            CheckSchema();
        }

        public void CheckSchema()
        {
            MySqlCommand command = null;
            try
            {
                if (!CreateConnection())
                    return;
                ushort version = 0;
                command = Connection.CreateCommand();
                command.CommandText = new QueryBuilder(QueryBuilderType.SHOW).Table(TableConfig).Build();
                object test = command.ExecuteScalar();

                if (test == null)
                {
                    command.CommandText = "CREATE TABLE `" + TableConfig + "` (" +
                            " `key` varchar(40) COLLATE utf8_unicode_ci NOT NULL," +
                            " `value` varchar(40) COLLATE utf8_unicode_ci NOT NULL," +
                            " PRIMARY KEY(`key`)" +
                            ") ENGINE = InnoDB DEFAULT CHARSET = utf8 COLLATE = utf8_unicode_ci;";
                    command.CommandText += "CREATE TABLE `" + TableItems + "` (" +
                        " `ItemID` SMALLINT UNSIGNED NOT NULL," +
                        " `BuyCost` DECIMAL(11, 4) NOT NULL," +
                        " `SellMultiplier` DECIMAL(11, 4) NOT NULL," +
                        " `MinBuyPrice` DECIMAL(11, 4) NOT NULL," +
                        " `ChangeRate` DECIMAL(11, 4) NOT NULL," +
                        " `ItemName` VARCHAR(255) CHARACTER SET utf8 COLLATE utf8_unicode_ci NOT NULL," +
                        " PRIMARY KEY (`ItemID`)" +
                        ") ENGINE = InnoDB DEFAULT CHARSET = utf8 COLLATE = utf8_unicode_ci;";
                    command.CommandText += "CREATE TABLE `" + TableVehicles + "` (" +
                        " `ItemID` SMALLINT UNSIGNED NOT NULL," +
                        " `BuyCost` DECIMAL(11, 4) NOT NULL," +
                        " `ItemName` VARCHAR(255) CHARACTER SET utf8 COLLATE utf8_unicode_ci NOT NULL," +
                        " PRIMARY KEY (`ItemID`)" +
                        ") ENGINE = InnoDB DEFAULT CHARSET = utf8 COLLATE = utf8_unicode_ci;";
                    command.ExecuteNonQuery();
                    CheckVersion(version, command);
                }
                else
                {
                    command.CommandText = new QueryBuilder(QueryBuilderType.SELECT).Column("value").Table(TableConfig).Where("key", "version").Build();
                    object result = command.ExecuteScalar();
                    if (result != null)
                    {
                        if (ushort.TryParse(result.ToString(), out version))
                            CheckVersion(version, command);
                        else
                        {
                            Logger.LogWarning("Error: Database version number not found.");
                            return;
                        }
                    }
                    else
                    {
                        Logger.LogWarning("Error: Database version number not found.");
                        return;
                    }
                }
                IsLoaded = true;
            }
            catch (MySqlException ex)
            {
                Logger.LogException(ex);
            }
            finally
            {
                if (command != null)
                    command.Dispose();
                Connection.Close();
            }
        }

        private void CheckVersion(ushort version, MySqlCommand command)
        {
            ushort updatingVersion = 0;
            try
            {
                if (version < 1)
                {
                    updatingVersion = 1;
                    command.CommandText = new QueryBuilder(QueryBuilderType.INSERT).Table(TableConfig).Column("key", "version").Column("value", "1").Build();
                    command.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                HandleException(ex, "Failed in updating Database schema to version " + updatingVersion + ", you may have to do a manual update to the database schema.");
            }
        }

        private void HandleException(MySqlException ex, string msg = null)
        {
            if (ex.Number == 0)
            {
                Logger.LogException(ex, "Error: Connection lost to database server.");
            }
            else
            {
                Logger.LogWarning(ex.Number.ToString() + ":" + ((MySqlErrorCode)ex.Number).ToString());
                Logger.LogException(ex, msg);
            }
        }

        public bool ConvertDB(BackendType toBackend)
        {
            bool result = false;
            if (toBackend == Backend)
                return result;
            else if (toBackend == BackendType.XML)
            {
                DataManager database = new XMLDatabaseManager();
                if (!database.IsLoaded)
                    return result;
                try
                {
                    Dictionary<ushort, ShopObject> items = GetAllItems(ItemType.Item);
                    Dictionary<ushort, ShopObject> vehicles = GetAllItems(ItemType.Vehicle);

                    foreach (ShopObject item in items.Values)
                    {
                        database.AddItem(ItemType.Item, item);
                    }
                    foreach (ShopObject vehicle in vehicles.Values)
                    {
                        database.AddItem(ItemType.Vehicle, vehicle);
                    }
                    result = true;
                }
                catch(MySqlException ex)
                {
                    HandleException(ex);
                }
                finally
                {
                    if (database.IsLoaded)
                        database.Unload();
                    database = null;
                }
            }
            return result;
        }

        public Dictionary<ushort, ShopObject> GetAllItems(ItemType type)
        {
            MySqlCommand command = null;
            MySqlDataReader reader = null;
            Dictionary<ushort, ShopObject> itemList = new Dictionary<ushort, ShopObject>();
            ShopObject item = null;
            try
            {
                if (!CreateConnection())
                    return itemList;
                command = Connection.CreateCommand();
                if (type == ItemType.Item)
                    command.CommandText = new QueryBuilder(QueryBuilderType.SELECT).Column("ItemID").Column("BuyCost").Column("SellMultiplier").Column("MinBuyPrice").Column("ChangeRate").Table(TableItems).Build();
                else
                    command.CommandText = new QueryBuilder(QueryBuilderType.SELECT).Column("ItemID").Column("BuyCost").Table(TableVehicles).Build();
                reader = command.ExecuteReader();
                if (!reader.HasRows)
                    return itemList;
                while (reader.Read())
                {
                    if (type == ItemType.Item)
                    {
                        item = ShopObjectBuild(ItemType.Item, reader);
                        itemList.Add(item.ItemID, item);
                    }
                    else
                    {
                        item = ShopObjectBuild(ItemType.Vehicle, reader);
                        itemList.Add(item.ItemID, item);

                    }
                }
                reader.Dispose();
            }
            catch (MySqlException ex)
            {
                HandleException(ex);
            }
            finally
            {
                if (command != null)
                    command.Dispose();
                if (reader != null)
                    reader.Dispose();
                Connection.Close();
            }
            return itemList;
        }


        private bool CreateConnection()
        {
            try
            {
                if (Connection == null)
                    Connection = new MySqlConnection(string.Format("SERVER={0};DATABASE={1};UID={2};PASSWORD={3};PORT={4};", DShop.Instance.Configuration.Instance.DatabaseAddress, DShop.Instance.Configuration.Instance.DatabaseName,
                        DShop.Instance.Configuration.Instance.DatabaseUsername, DShop.Instance.Configuration.Instance.DatabasePassword, DShop.Instance.Configuration.Instance.DatabasePort));
                Connection.Open();
                return true;
            }
            catch(MySqlException ex)
            {
                Logger.LogException(ex, "Failed to connect to the database server!");
                return false;
            }
        }

        public void Unload()
        {
            Connection.Dispose();
            Connection = null;
            IsLoaded = false;
        }

        private ShopObject ShopObjectBuild(ItemType type, MySqlDataReader reader)
        {
            if (type == ItemType.Item)
                return new ShopItem(reader.GetUInt16("ItemID"), reader.GetDecimal("BuyCost"), reader.GetDecimal("SellMultiplier"), reader.GetDecimal("MinBuyPrice"), reader.GetDecimal("ChangeRate"));
            else
                return new ShopVehicle(reader.GetUInt16("ItemID"), reader.GetDecimal("BuyCost"));
        }

        public bool AddItem(ItemType type, ShopObject shopObject)
        {
            MySqlCommand command = null;
            bool result = false;
            try
            {
                if (!CreateConnection())
                    return result;
                command = Connection.CreateCommand();
                command.Parameters.AddWithValue("@itemName", shopObject.ItemName);

                if (type == ItemType.Item)
                {
                    ShopItem item = shopObject as ShopItem;
                    command.CommandText = new QueryBuilder(QueryBuilderType.INSERT).Table(TableItems).Column("ItemID", item.ItemID).Column("BuyCost", item.BuyCost).Column("SellMultiplier", item.SellMultiplier).Column("MinBuyPrice", item.MinBuyPrice).
                        Column("ChangeRate", item.Change).Column("ItemName", "@itemName").DuplicateInsertUpdate().Build();
                }
                else
                {
                    ShopVehicle vehicle = shopObject as ShopVehicle;
                    command.CommandText = new QueryBuilder(QueryBuilderType.INSERT).Table(TableVehicles).Column("ItemID", vehicle.ItemID).Column("BuyCost", vehicle.BuyCost).Column("ItemName", "@itemName").DuplicateInsertUpdate().Build();
                }
                command.ExecuteNonQuery();
                result = true;
            }
            catch (MySqlException ex)
            {
                HandleException(ex);
            }
            finally
            {
                if (command != null)
                    command.Dispose();
                Connection.Close();
            }
            return result;
        }

        public ShopObject GetItem(ItemType type, ushort itemID)
        {
            ShopObject shopObject = new ShopObject();
            MySqlDataReader reader = null;
            MySqlCommand command = null;
            try
            {
                if (!CreateConnection())
                    return shopObject;
                command = Connection.CreateCommand();
                if (type == ItemType.Item)
                    command.CommandText = new QueryBuilder(QueryBuilderType.SELECT).Column("ItemID").Column("BuyCost").Column("SellMultiplier").Column("MinBuyPrice").Column("ChangeRate").Where("ItemID", itemID).Table(TableItems).Build();
                else
                    command.CommandText = new QueryBuilder(QueryBuilderType.SELECT).Column("itemID").Column("BuyCost").Table(TableVehicles).Where("ItemID", itemID).Build();
                reader = command.ExecuteReader();
                if (reader.Read())
                {
                    if (type == ItemType.Item)
                        shopObject = ShopObjectBuild(type, reader);
                    else
                        shopObject = ShopObjectBuild(type, reader);
                }
            }
            catch (MySqlException ex)
            {
                HandleException(ex);
            }
            finally
            {
                if (reader != null)
                    reader.Dispose();
                if (command != null)
                    command.Dispose();
                Connection.Close();
            }
            // Returns either ShopItem or ShopVehicle based type if id is found(with conversion), ShopObject, if it's not found.
            return shopObject;
        }


        public bool DeleteItem(ItemType type, ushort itemID)
        {
            bool result = false;
            MySqlCommand command = null;
            if (GetItem(type, itemID).ItemID != itemID)
                return result;
            else
            {
                try
                {
                    if (!CreateConnection())
                        return result;
                    command = Connection.CreateCommand();
                    command.CommandText = new QueryBuilder(QueryBuilderType.DELETE).Table(type == ItemType.Item ? TableItems : TableVehicles).Where("ItemID", itemID).Build();
                    command.ExecuteNonQuery();
                    result = true;
                }
                catch(MySqlException ex)
                {
                    HandleException(ex);
                }
                finally
                {
                    if (command == null)
                        command.Dispose();
                    Connection.Close();
                }
                return result;
            }
        }
    }
}
