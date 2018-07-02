using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace DynShop
{
    public class ShopObject
    {
        [XmlAttribute]
        public ushort ItemID = 0;
        [XmlAttribute]
        public decimal BuyCost = 10;
        [XmlAttribute]
        public string ItemName = "";

    }
}
