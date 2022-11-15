using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace CMI.Tools.CacheFilesAnalysis
{
    public class CreateExcelFile 
    {
        private WorkbookPart workbookPart1;

        private List<string> extenstion;

        private List<CacheFile> cacheFiles;

        public string FileFullname { get; private set; }
        

        public void CreateExcel(List<CacheFile> data, string outPutFileDirectory)
        {
            this.cacheFiles = data;
            this.FileFullname = Path.Combine(outPutFileDirectory, "Output.xlsx");
            if (File.Exists(FileFullname))
            {
                var datetime = DateTime.Now.ToString(CultureInfo.InvariantCulture).Replace("/", "_").Replace(":", "_");
                this.FileFullname = Path.Combine(outPutFileDirectory, "Output_" + datetime + ".xlsx");
            }

            using (SpreadsheetDocument package = SpreadsheetDocument.Create(FileFullname, SpreadsheetDocumentType.Workbook))
            {
                CreatePartsForExcel(package);
                CreateSheetData(new Sheet { Name = "Übersicht", SheetId = 1, Id = "rId1" }, GenerateSheetData1(), "A1:V1");
                CreateSheetData(new Sheet { Name = "Detail", SheetId = 2, Id = "rId2" }, GenerateSheetData2(), "A1:C1");
            }
        }

        private void CreatePartsForExcel(SpreadsheetDocument package)
        {
            workbookPart1 = package.AddWorkbookPart();
            var sheets1 = new Sheets();
            var workbook1 = new Workbook();
            workbook1.AppendChild(sheets1);
            workbookPart1.Workbook = workbook1;
        }

        private void CreateSheetData(Sheet sheet, SheetData partSheetData, string reference)
        {
            workbookPart1?.Workbook.Sheets?.AppendChild(sheet);
            WorksheetPart worksheetPart1 = workbookPart1?.AddNewPart<WorksheetPart>(sheet.Id);
            GenerateWorksheetPartContent(worksheetPart1, partSheetData, reference);
        }

        private void GenerateWorksheetPartContent(WorksheetPart worksheetPart1, SheetData sheetData1, string reference)
        {
            var worksheet1 = new Worksheet();
            SheetDataSet(sheetData1, reference, worksheet1);
            worksheetPart1.Worksheet = worksheet1;
        }

        private Row GenerateRow(CacheFile cacheFile)
        {
            Row tRow = new Row();
            tRow.AppendChild(CreateCell(cacheFile.Name, CellValues.String));
            tRow.AppendChild(CreateCell(cacheFile.DirectoryName, CellValues.String));
            tRow.AppendChild(CreateCell(cacheFile.Depth.ToString()));
            tRow.AppendChild(CreateCell(cacheFile.NumberOfFiles.ToString()));
            tRow.AppendChild(CreateCell(cacheFile.SizeOfZipOutput.ToString("0.000")));
            tRow.AppendChild(CreateCell(cacheFile.SmallestFileOutput.ToString("0.000")));
            tRow.AppendChild(CreateCell(cacheFile.BiggestFileOutput.ToString("0.000")));
            tRow.AppendChild(CreateCell(cacheFile.AverageSizeFileOutput.ToString("0.000")));

            foreach (var key in extenstion)
            {
                tRow.AppendChild(cacheFile.Extension.ContainsKey(key)
                    ? CreateCell(cacheFile.Extension[key].ToString())
                    : CreateCell("0"));
            }

            return tRow;
        }

        private Row GenerateRow(CacheFileEntry cacheFileEntry)
        {
            Row tRow = new Row();
            tRow.AppendChild(CreateCell(cacheFileEntry.Name, CellValues.String));
            tRow.AppendChild(CreateCell(cacheFileEntry.Extension, CellValues.String));
            tRow.AppendChild(CreateCell(cacheFileEntry.Size.ToString("0.000")));

            return tRow;
        }

        private SheetData GenerateSheetData1()
        {
            SheetData sheetData1 = new SheetData();
            sheetData1.AppendChild(CreateHeaderRowForExcelsheet1());

            foreach (var cacheFile in cacheFiles)
            {
                Row partsRows = GenerateRow(cacheFile);
                sheetData1.AppendChild(partsRows);
            }

            return sheetData1;
        }

        private SheetData GenerateSheetData2()
        {
            SheetData sheetData1 = new SheetData();
            sheetData1.AppendChild(CreateHeaderRowForExcelsheet2());

            foreach (var cacheFile in cacheFiles)
            {
                foreach (var cacheFileEntry in cacheFile.FilesInZip)
                {
                    Row partsRows = GenerateRow(cacheFileEntry);
                    sheetData1.AppendChild(partsRows);
                }
            }

            return sheetData1;
        }

        private Row CreateHeaderRowForExcelsheet1()
        {
            Row workRow = new Row();

            workRow.AppendChild(CreateCell("Name", CellValues.String,3));
            workRow.AppendChild(CreateCell("DirectoryName", CellValues.String, 3));
            workRow.AppendChild(CreateCell("Tiefe", CellValues.String, 3));
            workRow.AppendChild(CreateCell("Dateien", CellValues.String, 3));
            workRow.AppendChild(CreateCell("Grösse des Zips (MB)", CellValues.String, 3));
            workRow.AppendChild(CreateCell("Kleinste Datei (MB)", CellValues.String, 3));
            workRow.AppendChild(CreateCell("Grösste Datei (MB)", CellValues.String, 3));
            workRow.AppendChild(CreateCell("Durchschnittsgrösse Datei (MB)", CellValues.String, 3));

            extenstion = new List<string>();
            foreach (var key in from keys in cacheFiles.Select(e => e.Extension).GroupBy(e => e.Keys).GroupBy(g => g.Key, g => g).ToDictionary(e => e.Key) from key in keys.Key where !extenstion.Contains(key) select key)
            {
                extenstion.Add(key);
                workRow.AppendChild(CreateCell("Dateityp: " + key.ToUpper(), CellValues.String, 3));
            }

            return workRow;
        }

        private Row CreateHeaderRowForExcelsheet2()
        {
            Row workRow = new Row();

            workRow.AppendChild(CreateCell("Name", CellValues.String, 3));
            workRow.AppendChild(CreateCell("Dateityp", CellValues.String, 3));
            workRow.AppendChild(CreateCell("Grösse in B", CellValues.String, 3));

            return workRow;
        }
        
        private Cell CreateCell(string text, CellValues cellValues = CellValues.Number, uint styleIndex = 1U)
        {
            return new Cell()
            {
                StyleIndex = styleIndex,
                DataType = cellValues,
                CellValue = new CellValue(text)
            };
        }

        private void SheetDataSet(SheetData sheetData1, string reference, Worksheet worksheet1)
        {
            var sheetDimension1 = new SheetDimension() { Reference = reference };
            var sheetViews1 = new SheetViews();
            var sheetView1 = new SheetView() { TabSelected = true, WorkbookViewId = 0 };
            var sheetFormatProperties1 = new SheetFormatProperties { DefaultRowHeight = 15D, DyDescent = 0.25D };
            var filter = new AutoFilter() { Reference = reference };
            var pageMargins1 = new PageMargins() { Left = 0.7D, Right = 0.7D, Top = 0.75D, Bottom = 0.75D, Header = 0.3D, Footer = 0.3D };

            sheetViews1.AppendChild(sheetView1);
            worksheet1.AppendChild(sheetDimension1);
            worksheet1.AppendChild(sheetViews1);
            worksheet1.AppendChild(sheetFormatProperties1);
            worksheet1.AppendChild(sheetData1);
            worksheet1.AppendChild(filter);
            worksheet1.AppendChild(pageMargins1);
        }
    }
}
