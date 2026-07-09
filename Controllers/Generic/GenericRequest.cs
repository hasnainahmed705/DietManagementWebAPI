namespace DietManagementWebAPI.Controllers.Generic
{
    public class GenericRequest
    {
        public int page { get; set; } = 0;                    // Page number (0-based)
        public int ttlRecords { get; set; } = 5;              // Records per page (default 5)
        public string apiPathValue { get; set; } = string.Empty; // Collection Name
        public string sortField { get; set; } = string.Empty;
        public string sortOrder { get; set; } = "asc";        // asc or desc
        public List<FilterItem> filters { get; set; } = new();
    }

    public class FilterItem
    {
        public string filterName { get; set; } = string.Empty;
        public string filterType { get; set; } = "eq";        // eq, contains, gt, lt, etc.
        public string filterValue { get; set; } = string.Empty;
    }
}
