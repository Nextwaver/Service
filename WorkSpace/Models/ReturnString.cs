using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkSpace.Models
{
    public class ReturnString
    {
        #region Property

        public String Data { get; set; }

        #endregion

        #region Constructor

        public ReturnString()
        {
        }

        public ReturnString(String Data)
        {
            this.Data = Data;
        }

        #endregion
    }
}
