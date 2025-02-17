using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPACE_GATE.Class
{
    public class BookDetails
    {
        #region BookClass
        public Int32 _BookClassDocID { get; set; }
        public String _BookClassName { get; set; }
        public Int32 _BookCount { get; set; }
        public Int32 _BookMax { get; set; }
        #endregion

        #region Book
        public Int32 _BookDocID { get; set; }
        public Int32 _PageCount { get; set; }
        public Int32 _PageMax { get; set; }
        #endregion

        #region Page
        public Int32 _SubPageMax { get; set; }
        public Int32 _RowMax { get; set; }
        #endregion

        #region SubPage

        #endregion
    }
}
