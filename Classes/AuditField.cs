using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CRMCleaner.Classes
{
  public  class AuditField
    {
        string _FieldName;

        string _FieldValue;
        [XmlElement("FieldName")]
        public string FieldName
        {
            get { return _FieldName; }
            set { _FieldName = value; }
        }

        [XmlElement("FieldValue")]
        public string FieldValue
        {
            get { return _FieldValue; }
            set { _FieldValue = value; }
        }
    }
}
