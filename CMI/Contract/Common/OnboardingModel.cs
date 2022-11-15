using System;

namespace CMI.Contract.Common
{
    public class OnboardingModel
    {
        public string UserId { get; set; }
        public string SelectedId { get; set; }
        public string SelectedCountry { get;  set; }
        public string Language { get; set; }
        public string Title { get; set; }
        public string Name { get; set; }
        public string Firstname { get; set; }
        public string Middlename { get; set; }
        public string Nationality { get; set; }
        public string Permit { get; set; }
        public string DateOfBirth { get; set; }
        public string Mobile { get; set; }    
        public string Email { get; set; }
        public string IdType { get; set; }
        public string IdIssuingCountry { get; set; }
        public string CountryOfResidence { get; set; }
        public string StreetName { get; set; }
        public string TownName { get; set; }
        public string ZipCode { get; set; }

        public string ProcessId => $"{DateTime.UtcNow.Ticks}-{this.UserId}";
    }
}