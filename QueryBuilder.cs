using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynShop
{
    public class QueryBuilder
    {
        private string _table = string.Empty;
        private Dictionary<string, string> _columns = new Dictionary<string, string>();
        private Dictionary<string, string> _where = new Dictionary<string, string>();
        private bool _whereAnd = false;
        private bool _duplicateInsertUpdate = false;
        private QueryBuilderType _type;

        public QueryBuilder (QueryBuilderType queryType)
        {
            _type = queryType;
        }

        public QueryBuilder Column(string columnName, object value = null)
        {
            _columns.Add(columnName, value.ToString());
            return this;
        }
        public QueryBuilder DuplicateInsertUpdate()
        {
            _duplicateInsertUpdate = true;
            return this;
        }
        public QueryBuilder Table(string tableName)
        {
            _table = tableName;
            return this;
        }
        public QueryBuilder Where(string column, object value)
        {
            _where.Add(column, value.ToString());
            return this;
        }
        public QueryBuilder WhereAnd(bool isAnd)
        {
            _whereAnd = isAnd;
            return this;
        }

        public string Build()
        {
            string query = string.Empty;
            int i = 0;
            switch (_type)
            {
                case QueryBuilderType.INSERT:
                    {
                        query = "INSERT INTO `" + _table + "` ";
                        query += "(`" + string.Join("`, `", _columns.Keys.ToArray()) + "`) VALUES (" + string.Join(", ", _columns.Values.ToArray()) + ")";
                        if (_duplicateInsertUpdate)
                        {
                            query += " ON DUPLICATE KEY UPDATE ";
                            foreach (string key in _columns.Keys)
                            {
                                query += "`" + key + "` = VALUES(`" + key + "`)";
                                i++;
                                if (i < _columns.Count)
                                    query += ", ";
                            }
                        }
                        break;
                    }
                case QueryBuilderType.SELECT:
                    {
                        query = "SELECT `";
                        query += string.Join("`, `", _columns.Keys.ToArray()) + "` ";
                        query += "FROM `" + _table + "`";
                        if (_where.Count > 0)
                        {
                            query += " WHERE ";
                            foreach (KeyValuePair<string,string> value in _where)
                            {
                                query += "`" + value.Key + "` = " + value.Value;
                                i++;
                                if (i < _where.Count)
                                    query += _whereAnd ? " AND " : " OR ";
                            }
                        }
                        break;
                    }
                case QueryBuilderType.UPDATE:
                    {
                        query = "UPDATE `" + _table + "` SET ";
                        foreach (KeyValuePair<string, string> value in _columns)
                        {
                            query += "`" + value.Key + "` = " + value.Value;
                            i++;
                            if (i < _columns.Count)
                                query += ", ";
                        }
                        break;
                    }
                case QueryBuilderType.DELETE:
                    {
                        query = "DELETE FROM `" + _table + "` WHERE ";
                        foreach (KeyValuePair<string, string> value in _where)
                        {
                            query += "`" + value.Key + "` = " + value.Value;
                            i++;
                            if (i < _where.Count)
                                query += _whereAnd ? " AND " : " OR ";
                        }
                        break;
                    }
                case QueryBuilderType.SHOW:
                    {
                        query = "SHOW TABLES LIKE `" + _table + "`";
                        break;
                    }
            }
            query += ";";
            return query;
        }
    }
    public enum QueryBuilderType
    {
        INSERT,
        SELECT,
        UPDATE,
        DELETE,
        SHOW,
    }
}
