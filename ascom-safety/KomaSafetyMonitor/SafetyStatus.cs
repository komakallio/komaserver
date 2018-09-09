using System.Collections.Generic;
using System.Linq;

namespace ASCOM.Komakallio
{
    public struct SafetyStatus
    {
        public SafetyStatus(bool isSafe, Dictionary<string, bool> details)
        {
            IsSafe = isSafe;
            Details = details;
        }

        public Dictionary<string, bool> Details { get; private set; }

        public bool IsSafe { get; private set; }

        public bool IsSafeWithFilters(List<Filter> filters)
        {
            var noFilters = filters.Count == 0;
            var noActiveFilters = !filters.Where(x => x.Active).Any();
            if (noFilters || noActiveFilters) return IsSafe;

            var activeFilters = filters
                .Where(x => x.Active)
                .Select(x => x.Name);

            return Details
                .Where(x => activeFilters.Contains(x.Key))
                .All(x => x.Value);
        }
    }
}
