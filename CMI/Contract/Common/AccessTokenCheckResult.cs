namespace CMI.Contract.Common
{  
    public class AccessTokenCheckResult
    {
        public string VeId { get; set; }
        public AccessTokens Calculated { get; set; }
        public AccessTokens Elastic { get; set; }
        public bool CheckError { get; set; }
    }
       
}