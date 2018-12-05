using System;
using System.Linq;
using Microsoft.SharePoint;

namespace Thrive2018Demo.Utilities
{
    public static class SPHelper
    {
        public static SPList FindListByName(this SPWeb web, string name)
        {
            SPList instance = (from SPList list in web.Lists
                               where list.RootFolder.Name.Equals(name, StringComparison.InvariantCulture)
                               select list).FirstOrDefault();

            return instance;
        }
    }
}
