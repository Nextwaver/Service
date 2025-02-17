using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPACE_GATE.Class
{
    public class Row
    {
        public String DocClassID { get; set; }
        public Int32 DocID { get; set; }
        public String Token { get; set; }
        public String Hashing { get; set; }
        public String Dsig { get; set; }
        public String SpaceGateID_Sender { get; set; }
        public String SpaceGateID_Update { get; set; }
    }
    public class TicketList
    {
        public String DocClassID { get; set; }
        public Int32 DocID { get; set; }
        public String Token { get; set; }
        public String Hashing { get; set; }
        public String Dsig { get; set; }
    }
}
