using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CMI.Contract.Common;

namespace CMI.Contract.Messaging
{
    public class StartOnboardingProcessRequest
    {
        public OnboardingModel OnboardingModel { get; set; }
    }

    public class StartOnboardingProcessResponse
    {
        public StartProcessResult Result { get; set; }
    }

    public class HandleOnboardingCallbackRequest
    {
        public CallbackType CallbackType { get; set; }
        public PostbackParameters Parameters { get; set; }
    }

    public class HandleOnboardingCallbackResponse
    {
        public bool Success { get; set; }
    }

    public enum CallbackType
    {
        Success,
        Warn,
        Error,
        Review
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class PostbackParameters
    {
        public string extId { get; set; }
        public string userId => Regex.Match(extId, "[A-Za-z0-9]{1,}$").Value;
    }

    public class StartProcessResult
    {
        public bool Success { get; set; }
        public string ProcessUrl { get; set; }
    }
}
