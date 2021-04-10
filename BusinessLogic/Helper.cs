using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic
{
    public static class Helper
    {
        public static DataTable ToDataTable<T>(this List<T> data)
        {
            DataTable table = new DataTable();

            if (data != null)
            {
                PropertyDescriptorCollection props =
                    TypeDescriptor.GetProperties(typeof(T));

                foreach (PropertyDescriptor prop in props)
                    table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                foreach (T item in data)
                {
                    DataRow row = table.NewRow();
                    foreach (PropertyDescriptor prop in props)
                        row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                    table.Rows.Add(row);
                }

            }
            return table;
        }
    }
}
