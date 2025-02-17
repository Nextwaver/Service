using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;

namespace WorkSpace.Models
{
    public class ReturnDataTable
    {
        #region Property

        public DataTable Data { get; set; }

        #endregion

        #region Constructor

        public ReturnDataTable()
        {
        }

        public ReturnDataTable(DataTable Data)
        {
            this.Data = Data;
        }

        #endregion
    }
}
