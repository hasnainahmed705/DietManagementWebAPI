namespace DietManagementWebAPI.Controllers.Generic
{
    public class GenericRequest
    {
        public int page { get; set; } = 0;
        public int ttlRecords { get; set; } = 5;           // Default 5 records
        public string apiPathValue { get; set; } = string.Empty;

        // Optional Fields
        public string sortField { get; set; } = string.Empty;
        public string sortOrder { get; set; } = "asc";     // Default ascending

        public List<FilterItem> filters { get; set; } = new List<FilterItem>();
    }

    public class FilterItem
    {
        public string filterName { get; set; } = string.Empty;
        public string filterType { get; set; } = "eq";
        public string filterValue { get; set; } = string.Empty;
    }
}
