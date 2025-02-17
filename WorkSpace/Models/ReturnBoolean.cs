using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkSpace.Models
{
    public class ReturnBoolean
    {
        #region Property

        public Boolean Data { get; set; }

        #endregion

        #region Constructor

        public ReturnBoolean()
        {
        }

        public ReturnBoolean(Boolean Data)
        {
            this.Data = Data;
        }

        #endregion
    }
}
