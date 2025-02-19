using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using Newtonsoft.Json;
namespace WorkSpace.Models
{
    public class ReturnDataTable
    {
        #region Property

        public string Data { get; set; }

        #endregion

        #region Constructor

        public ReturnDataTable()
        {
        }

        public ReturnDataTable(DataTable dt)
        {
            this.Data = JsonConvert.SerializeObject(dt);
        }

        #endregion
    }
}
