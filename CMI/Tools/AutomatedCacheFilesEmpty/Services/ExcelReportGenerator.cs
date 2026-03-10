using System.Collections.Generic;
using System.IO;
using CMI.Tools.AutomatedCacheFilesEmpty.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace CMI.Tools.AutomatedCacheFilesEmpty.Services
{
    public class ExcelReportGenerator
    {
        public void GenerateReport(List<CacheCheckResult> checkResults, string outputFile)
        {
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }

            using (var spreadsheetDocument = SpreadsheetDocument.Create(outputFile, SpreadsheetDocumentType.Workbook))
            {
                var workbookPart = spreadsheetDocument.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                var sheetData = new SheetData();
                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(sheetData);

                var sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild(new Sheets());
                var sheet = new Sheet
                {
                    Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "Cache Files Report"
                };
                sheets.Append(sheet);

                // Add header row
                var headerRow = new Row();
                headerRow.Append(
                    CreateCell("File Path"),
                    CreateCell("Archive Record ID"),
                    CreateCell("Reference Code"),
                    CreateCell("File Size (MB)"),
                    CreateCell("Last Downloaded Date"),
                    CreateCell("File Created Date"),
                    CreateCell("To Be Deleted"),
                    CreateCell("Has no Viewer manifest")
                );
                sheetData.AppendChild(headerRow);

                // Add data rows
                foreach (var checkResult in checkResults)
                {
                    var dataRow = new Row();
                    dataRow.Append(
                        CreateCell(checkResult.FilePath),
                        CreateCell(checkResult.ArchiveRecordId),
                        CreateCell(checkResult.ReferenceCode),
                        CreateCell(checkResult.FileSizeInMb.ToString("F2")),
                        CreateCell(checkResult.DatumErstellungToken.ToString("yyyy-MM-dd HH:mm:ss")),
                        CreateCell(checkResult.FileCreatedDate.ToString("yyyy-MM-dd HH:mm:ss")),
                        CreateCell(checkResult.ToBeDeleted.ToString()),
                        CreateCell(checkResult.HasNoViewerManifest.ToString())
                    );
                    sheetData.AppendChild(dataRow);
                }

                workbookPart.Workbook.Save();
            }
        }

        private Cell CreateCell(string text)
        {
            return new Cell
            {
                DataType = CellValues.String,
                CellValue = new CellValue(text)
            };
        }
    }
}
