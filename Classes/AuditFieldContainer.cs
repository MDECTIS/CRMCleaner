using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CRMCleaner.Classes
{
    public class AuditFieldContainer
    {
        [XmlArray("AuditFields")]
        [XmlArrayItem("AuditField")]

        AuditField[] _AuditFields;
        [XmlArray("FullAuditFields")]
        [XmlArrayItem("AuditField")]

        AuditField[] _FullAuditFields;
        internal AuditField[] AuditFields
        {
            get { return _AuditFields; }
            set { _AuditFields = value.ToArray(); }
        }

        internal AuditField[] FullAuditFields
        {
            get { return _FullAuditFields; }
            set { _FullAuditFields = value.ToArray(); }
        }
    }
}
