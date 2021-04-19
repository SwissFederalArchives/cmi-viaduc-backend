namespace CMI.Contract.Parameter.SaveParameter
{
    public class SaveParameterRequest
    {
        public SaveParameterRequest(Parameter parameter)
        {
            Parameter = parameter;
        }

        public Parameter Parameter { get; set; }
    }
}