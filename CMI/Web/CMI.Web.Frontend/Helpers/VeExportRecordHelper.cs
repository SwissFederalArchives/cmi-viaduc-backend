using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Configuration;

namespace CMI.Web.Frontend.Helpers
{
    public class VeExportRecordHelper
    {
        private readonly ExcelExportHelper exportHelper;

        public VeExportRecordHelper(ExcelExportHelper exportHelper)
        {
            this.exportHelper = exportHelper;
        }

        /// <summary>
        ///  string = undefined
        /// </summary>
        /// <param name="items">The row values</param>
        /// <param name="language">Selected language</param>
        /// <param name="fileName">The desired file name</param>
        /// <param name="defaultName">A simple file name without special characters</param>
        /// <returns></returns>
        public HttpResponseMessage CreateExcelFile(List<VeExportRecord> items, string language, string fileName, string defaultName)
        {
            MemoryStream stream = exportHelper.ExportToExcel(items, CreateColumnInfo(language));
            var retVal = new HttpResponseMessage(HttpStatusCode.OK);
            var contentType = MimeMapping.GetMimeMapping("xlsx");
            retVal.Content = new ByteArrayContent(stream.ToArray());
            retVal.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            retVal.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                // The FileName is safe without special characters, FileNameStar is UTF-8 encoded
                FileName = defaultName,
                FileNameStar = fileName
            };
            return retVal;
        }

        #region private Methods
        
        private ExcelColumnInfos CreateColumnInfo(string language)
        {
            return new ExcelColumnInfos
            {
                new ExcelColumnInfo
                {
                    ColumnName = nameof(VeExportRecord.ReferenceCode), MakeAutoWidth = true,
                    ColumnHeader = FrontendSettingsViaduc.Instance.GetTranslation(language, "veExportRecord.referenceCode")
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(VeExportRecord.FileReference), MakeAutoWidth = true,
                    ColumnHeader = FrontendSettingsViaduc.Instance.GetTranslation(language, "veExportRecord.fileReference")
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(VeExportRecord.Title), MakeAutoWidth = true,
                    ColumnHeader = FrontendSettingsViaduc.Instance.GetTranslation(language, "veExportRecord.title")
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(VeExportRecord.CreationPeriod), MakeAutoWidth = true,
                    ColumnHeader = FrontendSettingsViaduc.Instance.GetTranslation(language, "veExportRecord.creationPeriod")
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(VeExportRecord.WithinInfo), MakeAutoWidth = true,
                    ColumnHeader = FrontendSettingsViaduc.Instance.GetTranslation(language, "veExportRecord.withinInfo")
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(VeExportRecord.Level), MakeAutoWidth = true,
                    ColumnHeader = FrontendSettingsViaduc.Instance.GetTranslation(language, "veExportRecord.level")
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(VeExportRecord.Accessibility), MakeAutoWidth = true,
                    ColumnHeader = FrontendSettingsViaduc.Instance.GetTranslation(language, "veExportRecord.accessibility")
                }
            };
        }
        
        public static string GetCustomField(dynamic customFields, string fieldName)
        {
            if (customFields != null)
            {
                var field =
                    ((IDictionary<string, object>)customFields).FirstOrDefault(k =>
                       k.Key.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase));

                if (field.Value != null)
                {
                    if (field.Value is string)
                        return field.Value.ToString();
                    if (field.Value is List<string> list && list.Count > 0)
                        return string.Join(", ", list.ToArray());
                    if (field.Value is List<object> listO && listO.Count > 0)
                        return string.Join(", ", listO.ToArray());
                }
            }

            return null;
        }

        #endregion
    }
}
