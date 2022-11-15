using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Aspose.Cells;
using Aspose.Cells.Tables;
using Serilog;

namespace CMI.Web.Common.Helpers
{
    public class ExcelExportHelper
    {
        static ExcelExportHelper()
        {
            try
            {
                var licensePdf = new License();
                licensePdf.SetLicense("Aspose.Total.NET.lic");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error while setting Aspose license.");
                throw;
            }
        }

        /// <summary>Exports any data list to a XLSX Excel stream.</summary>
        /// <typeparam name="T">Type of the data in the list</typeparam>
        /// <param name="data">The data to export</param>
        public MemoryStream ExportToExcel<T>(List<T> data, ExcelColumnInfos infos)
        {
            try
            {
                var workbook = new Workbook();
                var worksheet = workbook.Worksheets[0];

                // Validation of data
                EnsureValidData(data);

                // Importing the array of names to 1st row and first column vertically
                worksheet.Cells.ImportCustomObjects(data, 0, 0, new ImportTableOptions());
                worksheet.ListObjects.Add(0, 0, data.Count, typeof(T).GetProperties().Length - 1, true);
                worksheet.ListObjects[0].TableStyleType = TableStyleType.TableStyleLight9;

                ApplyFormatting(infos, worksheet);

                var stream = new MemoryStream();
                workbook.Save(stream, new OoxmlSaveOptions(SaveFormat.Xlsx));
                return stream;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected problem exporting data to excel");
                throw;
            }
        }

        private void ApplyFormatting(ExcelColumnInfos infos, Worksheet worksheet)
        {
            try
            {
                if (infos != null && infos.Any())
                {
                    var table = worksheet.ListObjects.FirstOrDefault();
                    foreach (var columnInfo in infos)
                    {
                        // Find column
                        var col = table?.ListColumns.Find(c => c.Name.Equals(columnInfo.ColumnName, StringComparison.InvariantCultureIgnoreCase));
                        if (col != null)
                        {
                            FormatColumn(worksheet, columnInfo, col);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error while formatting Excel export");
            }
        }

        private static void FormatColumn(Worksheet worksheet, ExcelColumnInfo columnInfo, ListColumn col)
        {
            if (!string.IsNullOrEmpty(columnInfo.FormatSpecification))
            {
                var cell = GetCell(col, 1);
                if (cell != null)
                {
                    var style = cell.GetStyle();
                    style.Custom = columnInfo.FormatSpecification;
                    col.Range.SetStyle(style);
                }
            }

            if (columnInfo.Width > 0)
            {
                col.Range.ColumnWidth = columnInfo.Width;
            }

            if (columnInfo.MakeAutoWidth)
            {
                worksheet.AutoFitColumn(col.Range.FirstColumn);
            }

            if (!string.IsNullOrEmpty(columnInfo.ColumnHeader))
            {
                col.Name = columnInfo.ColumnHeader;
            }

            if (columnInfo.Hidden)
            {
                col.Range.ColumnWidth = 0;
            }
        }

        /// <summary>
        /// The Cell is only supplied if the cell has a value also
        /// </summary>
        /// <param name="col"></param>
        /// <param name="rowIndex"></param>
        /// <returns></returns>
        private static Cell GetCell(ListColumn col, int rowIndex)
        {
            var cell = col.Range.GetCellOrNull(rowIndex, 0);
            if (cell == null && col.Range.RowCount > rowIndex + 1)
            {
                cell = GetCell(col, ++rowIndex);
            }
            return cell;
        }

        // As Excel allows only 32K in a cell, we need to make sure, no text property is longer than 32K
        private void EnsureValidData<T>(List<T> data)
        {
            var lengthLimit = 32 * 1000;

            var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => p.PropertyType == typeof(string))
                .Where(p => p.GetGetMethod(true).IsPublic)
                .Where(p => p.GetSetMethod(true).IsPublic).ToList();

            foreach (var row in data)
            foreach (var property in properties)
            {
                var temp = (string) property.GetValue(row, null);
                if (!string.IsNullOrEmpty(temp) && temp.Length > lengthLimit)
                {
                    property.SetValue(row, temp.Substring(0, lengthLimit), null);
                }
            }
        }
    }

    public class ExcelColumnInfos : List<ExcelColumnInfo>
    {
    }

    public class ExcelColumnInfo
    {
        public string ColumnName { get; set; }
        public string ColumnHeader { get; set; }
        public string FormatSpecification { get; set; }
        public int Width { get; set; }
        public bool MakeAutoWidth { get; set; }
        public bool Hidden { get; set; }
    }
}