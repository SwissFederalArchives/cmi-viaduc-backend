using System;

namespace CMI.Contract.Common
{
    public class PrimaerdatenAuftragLog
    {
        #region Constructors

        #endregion

        #region Properties

        public int PrimaerdatenAuftragLogId { get; set; }

        public int PrimaerdatenAuftragId { get; set; }

        public AufbereitungsStatusEnum Status { get; set; }

        public AufbereitungsServices Service { get; set; }

        public DateTime CreatedOn { get; set; }

        public string ErrorText { get; set; }

        #endregion
    }
}