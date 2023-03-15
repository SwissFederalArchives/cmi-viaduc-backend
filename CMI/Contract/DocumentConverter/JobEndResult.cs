namespace CMI.Contract.DocumentConverter;

public class JobEndResult: IErrorResult
{
    public bool IsInvalid { get; set; }
    public string ErrorMessage { get; set; }
}