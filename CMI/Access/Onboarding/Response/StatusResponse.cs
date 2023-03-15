using System;
using Newtonsoft.Json;

namespace CMI.Access.Onboarding.Response
{
    public class StatusResponse
    {
        public Status[] Data { get; set; }
    }

    public class Status
    {
        [JsonProperty("customer")]
        public Customer Customer { get; set; }
        [JsonProperty("extId")]
        public string ExtId { get; set; }
        [JsonProperty("documentUris")]
        public string[] DocumentUris { get; set; }
        [JsonProperty("mediaUris")]
        public MediaUri[] MediaUris { get; set; }
        [JsonProperty("verification")]
        public Verification Verification { get; set; }
        [JsonProperty("processSteps")]
        public ProcessSteps[] ProcessSteps { get; set; }
        [JsonProperty("reviews")]
        public object[] Reviews { get; set; }
    }

    public class ProcessSteps
    {
        [JsonProperty("key")]
        public string Key { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("subtype")]
        public string Subtype { get; set; }
        [JsonProperty("state")]
        public string State { get; set; }
    }

    public class Customer
    {
        [JsonProperty("userId")]
        public Userid UserId { get; set; }
        [JsonProperty("name")]
        public Name Name { get; set; }
        [JsonProperty("firstname")]
        public Firstname Firstname { get; set; }
        [JsonProperty("nationality")]
        public Nationality Nationality { get; set; }
        [JsonProperty("dateOfBirth")]
        public Dateofbirth DateOfBirth { get; set; }
        [JsonProperty("idType")]
        public IdType IdType { get; set; }
        [JsonProperty("idIssuingCountry")]
        public IdIssuingCountry IdIssuingCountry { get; set; }
        [JsonProperty("countryOfResidence")]
        public CountryOfResidence CountryOfResidence { get; set; }
        [JsonProperty("email")]
        public string Email { get; set; }
        [JsonProperty("address")]
        public Address Address { get; set; }
    }

    public class Userid
    {
        [JsonProperty("inputValue")]
        public string InputValue { get; set; }
        [JsonProperty("quality")]
        public string Quality { get; set; }
    }

    public class Name
    {
        [JsonProperty("inputValue")]
        public string InputValue { get; set; }
        [JsonProperty("quality")]
        public string Quality { get; set; }
        [JsonProperty("idValue")]
        public string IdValue { get; set; }
    }

    public class Firstname
    {
        [JsonProperty("inputValue")]
        public string InputValue { get; set; }
        [JsonProperty("quality")]
        public string Quality { get; set; }
        [JsonProperty("idValue")]
        public string IdValue { get; set; }
    }

    public class Nationality
    {
        [JsonProperty("inputValue")]
        public string InputValue { get; set; }
        [JsonProperty("quality")]
        public string Quality { get; set; }
        [JsonProperty("idValue")]
        public string IdValue { get; set; }
    }

    public class Dateofbirth
    {
        [JsonProperty("inputValue")]
        public string InputValue { get; set; }
        [JsonProperty("quality")]
        public string Quality { get; set; }
        [JsonProperty("idValue")]
        public string IdValue { get; set; }
    }

    public class IdType
    {
        [JsonProperty("inputValue")]
        public string InputValue { get; set; }
        [JsonProperty("quality")]
        public string Quality { get; set; }
    }

    public class IdIssuingCountry
    {
        [JsonProperty("inputValue")]
        public string InputValue { get; set; }
        [JsonProperty("quality")]
        public string Quality { get; set; }
    }

    public class CountryOfResidence
    {
        [JsonProperty("inputValue")]
        public Inputvalue InputValue { get; set; }
        [JsonProperty("quality")]
        public string Quality { get; set; }
    }

    public class Address
    {
        [JsonProperty("inputValue")]
        public Inputvalue InputValue { get; set; }
        [JsonProperty("quality")]
        public string Quality { get; set; }
    }

    public class Inputvalue
    {
        [JsonProperty("checkAddress")]
        public bool CheckAddress { get; set; }
        [JsonProperty("streetName")]
        public string StreetName { get; set; }
        [JsonProperty("townName")]
        public string TownName { get; set; }
        [JsonProperty("zipCode")]
        public string ZipCode { get; set; }
    }

    public class Verification
    {
        [JsonProperty("state")]
        public string State { get; set; }
        [JsonProperty("decision")]
        public Decision Decision { get; set; }
        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }
        [JsonProperty("quality")]
        public string Quality { get; set; }
        [JsonProperty("score")]
        public string Score { get; set; }
        [JsonProperty("log")]
        public object[] Log { get; set; }
    }

    public class Decision
    {
        [JsonProperty("process")]
        public string Process { get; set; }
        [JsonProperty("system")]
        public string System { get; set; }
        [JsonProperty("review")]
        public string Review { get; set; }
    }

    public class MediaUri
    {
        [JsonProperty("mediatype")]
        public string MediaType { get; set; }
        [JsonProperty("uri")]
        public string Uri { get; set; }
        [JsonProperty("tags")]
        public string[] Tags { get; set; }
    }
}
