namespace CMI.Contract.DocumentConverter
{
    public class AbbyyProgressEvent
    {
        /// <summary>
        /// The complete name of the file including the path 
        /// </summary>
        public string File { get; set; }

        /// <summary>
        /// Percentage is only reported in the <see cref="AbbyyEventType.AbbyyOnProgressEvent"/>
        /// </summary>
        public int Percentage { get; set; }

        /// <summary>
        /// Page is only reported in the <see cref="AbbyyEventType.AbbyyOnPageEvent"/>
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// TotalPages is only reported in the <see cref="AbbyyEventType.AbbyyOnPageEvent"/>
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Indicates if the event is reported for text extraction or for transformation
        /// </summary>
        public ProcessType Process { get; set; }

        /// <summary>
        /// Indicates if the event was raised by the Abbyy OnProgress event or by the Abbyy OnPageProcessed event.
        /// </summary>
        public AbbyyEventType EventType { get; set; }
        
        /// <summary>
        /// Indicates that the recognition of the document is completed
        /// </summary>
        public bool IsComplete { get; set; }

        /// <summary>
        /// Indicates that an error occurred
        /// </summary>
        public bool HasFailed { get; set; }

        /// <summary>
        /// Some additional information about the job
        /// </summary>
        public JobContext Context { get; set; }
    }
}
