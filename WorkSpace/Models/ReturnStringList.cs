using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkSpace.Models
{
    public class ReturnStringList
    {
        #region Property

        public List<ReturnString> DataList { get; set; }

        #endregion

        #region Constructor

        public ReturnStringList()
        {
            DataList = new List<ReturnString>();
        }

        public ReturnStringList(List<ReturnString> DataList)
        {
            this.DataList = DataList;
        }

        #endregion
    }
}
