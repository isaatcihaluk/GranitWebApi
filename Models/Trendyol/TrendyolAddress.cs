namespace GranitWebApi.Models.Trendyol
{
    public class TrendyolAddress
    {
        public long Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Company { get; set; }

        public string Address1 { get; set; }

        public string Address2 { get; set; }

        public string City { get; set; }

        public int CityCode { get; set; }

        public string District { get; set; }

        public int DistrictId { get; set; }

        public string PostalCode { get; set; }

        public string CountryCode { get; set; }

        public string Neighborhood { get; set; }

        public string Phone { get; set; }

        public string TaxOffice { get; set; }

        public string TaxNumber { get; set; }

        public string FullAddress { get; set; }

        public string FullName { get; set; }

        public string Latitude { get; set; }

        public string Longitude { get; set; }
    }

    public class TrendyolPackageHistory
    {
        public long CreatedDate { get; set; }

        public string Status { get; set; }
    }
}
