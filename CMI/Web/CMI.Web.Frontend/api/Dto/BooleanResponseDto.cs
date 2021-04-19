namespace CMI.Web.Frontend.api.Dto
{
    // Simple class to return a boolean value to Client.
    // The frontend client can't cope with simple value types.
    public class BooleanResponseDto
    {
        public bool Value { get; set; }
    }
}