namespace CMI.Contract.Parameter.SaveParameter
{
    public class SaveParameterEvent
    {
        public SaveParameterEvent()
        {
        }

        public SaveParameterEvent(Parameter parameter)
        {
            Parameter = parameter;
        }

        public Parameter Parameter { get; set; }
    }
}