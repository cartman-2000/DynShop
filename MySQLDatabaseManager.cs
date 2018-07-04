using fr34kyn01535.Uconomy;
using I18N.West;
using MySql.Data.MySqlClient;
using Rocket.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynShop
{
    internal class MySQLDatabaseManager : DataManagerFields, DataManager
    {
        private MySqlConnection Connection = null;

        private string Prefix;
        private string TableConfig;
        private string TableItems;
        private string TableVehicles;

        internal MySQLDatabaseManager()
        {
            CP1250 cP1250 = new CP1250();
            Backend = BackendType.MySQL;
            Prefix = DShop.Instance.Configuration.Instance.DatabaseTablePrefix;
            TableConfig = Prefix + "_config";
            TableItems = Prefix + "_items";
            TableVehicles = Prefix + "_vehicles";
            CheckSchema();
        }

        public void CheckSchema()
        {
            try
            {
                if (!CreateConnection())
                    return;
                ushort version = 0;
                MySqlCommand command = Connection.CreateCommand();
                command.CommandText = "show tables like `" + TableConfig + "`;";
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
                    command.CommandText += "CREATE TABLE `" + TableItems + "` (" +
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
                    command.CommandText = "SELECT `value` FROM `" + TableConfig + "` WHERE `key` = 'version';";
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
                command.Dispose();
            }
            catch (MySqlException ex)
            {
                Logger.LogException(ex);
            }
            finally
            {
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
                    command.CommandText = "INSERT INTO `" + TableConfig + "` (`key`, `value`) VALUES ('version', '1');";
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
            if (toBackend == Backend)
                return false;
            else if (toBackend == BackendType.XML)
            {
                DataManager database = new XMLDatabaseManager();
                MySqlCommand command = null;
                MySqlDataReader reader = null;
                try
                {
                    if (!CreateConnection())
                        return false;
                    command.CommandText = "SELECT `ItemID`, `BuyCost`, `SellMultiplier`, `MinBuyPrice`, `ChangeRate`, `ItemName` FROM `" + TableItems + "`;";
                    reader = command.ExecuteReader();
                    if (!reader.HasRows)
                        return false;
                    while (reader.Read())
                    {
                        database.AddItem(ItemType.Item, ShopObjectBuild(ItemType.Item, reader));
                    }
                    reader.Dispose();
                    command.CommandText = "SELECT `ItemID`, `BuyCost`, `ItemName` FROM `" + TableVehicles + "`;";
                    reader = command.ExecuteReader();
                    if (!reader.HasRows)
                        return false;
                    while (reader.Read())
                    {
                        database.AddItem(ItemType.Vehicle, ShopObjectBuild(ItemType.Vehicle, reader));
                    }
                    reader.Dispose();
                }
                catch(MySqlException ex)
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

                return true;
            }
            return false;
        }

        private bool CreateConnection()
        {
            try
            {
                if (Connection == null)
                    Connection = new MySqlConnection(string.Format("SERVER={0};DATABASE={1};UID={2};PASSWORD={3};PORT={4};", Uconomy.Instance.Configuration.Instance.DatabaseAddress, Uconomy.Instance.Configuration.Instance.DatabaseName,
                        Uconomy.Instance.Configuration.Instance.DatabaseUsername, Uconomy.Instance.Configuration.Instance.DatabasePassword, Uconomy.Instance.Configuration.Instance.DatabasePort));
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
        }

        private ShopObject ShopObjectBuild(ItemType type, MySqlDataReader reader)
        {
            if (type == ItemType.Item)
                return new ShopItem(reader.GetUInt16("ItemID"), reader.GetDecimal("BuyCost"), reader.GetDecimal("SellMultiplier"), reader.GetDecimal("MinBuyPrice"), reader.GetDecimal("ChangeRate"));
            else
                return new ShopVehicle(reader.GetUInt16("ItemID"), reader.GetDecimal("BuyCost"));
        }

        public bool AddItem(ItemType type, ShopObject item)
        {
            throw new NotImplementedException();
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
                    command.CommandText = "SELECT `ItemID`, `BuyCost`, `SellMultiplier`, `MinBuyPrice`, `ChangeRate`, `ItemName` FROM `"+ TableItems +"` WHERE `ItemID` = "+ itemID +";";
                else
                    command.CommandText = "SELECT `ItemID`, `BuyCost`, `ItemName` FROM `" + TableVehicles + "` WHERE `ItemID` = " + itemID + ";";
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
            if (GetItem(type, itemID) is ShopObject)
                return result;
            else
            {
                try
                {
                    if (!CreateConnection())
                        return result;
                    command = Connection.CreateCommand();
                    if (type == ItemType.Item)
                        command.CommandText = "DELETE FROM `"+ TableItems +"` WHERE `ItemID` = "+ itemID +";";
                    else
                        command.CommandText = "DELETE FROM `" + TableVehicles + "` WHERE `ItemID` = " + itemID + ";";
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
