using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCOM.Komakallio
{
    [TestClass]
    public class SafetyStatusTests
    {
        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void IsSafe_ReturnGlobalSafety(bool globalSafety)
        {
            var status = new SafetyStatus(globalSafety, new Dictionary<string, bool>());

            Assert.AreEqual(globalSafety, status.IsSafe);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void IsSafeWithFilters_NoFilters_ReturnGlobalSafety(bool globalSafety)
        {
            var status = new SafetyStatus(globalSafety, new Dictionary<string, bool>());
            var filters = new List<Filter>();
            Assert.AreEqual(globalSafety, status.IsSafeWithFilters(filters));
        }

        [TestMethod]
        public void IsSafeWithFilters_NoActiveFilters_GloballySafe_ReturnTrue()
        {
            var status = new SafetyStatus(true, new Dictionary<string, bool>());
            var filters = new List<Filter>
            {
                new Filter
                {
                    Name = "rain",
                    Active = false,
                },
                new Filter
                {
                    Name = "clouds",
                    Active = false,
                }
            };

            Assert.IsTrue(status.IsSafeWithFilters(filters));
        }

        [TestMethod]
        public void IsSafeWithFilters_NoActiveFilters_GloballyUnsafe_ReturnFalse()
        {
            var status = new SafetyStatus(false, new Dictionary<string, bool>());
            var filters = new List<Filter>
            {
                new Filter
                {
                    Name = "rain",
                    Active = false,
                },
                new Filter
                {
                    Name = "clouds",
                    Active = false,
                }
            };

            Assert.IsFalse(status.IsSafeWithFilters(filters));
        }

        [TestMethod]
        public void IsSafeWithFilters_AllFiltersActive_UnsafeDetails_ReturnFalse()
        {
            var details = new Dictionary<string, bool>
            {
                { "rain", false },
                { "clouds", true }
            };
            var status = new SafetyStatus(false, details);

            var filters = new List<Filter>
            {
                new Filter
                {
                    Name = "rain",
                    Active = true
                },
                new Filter
                {
                    Name = "clouds",
                    Active = true,
                }
            };

            Assert.IsFalse(status.IsSafeWithFilters(filters));
        }

        [TestMethod]
        public void IsSafeWithFilters_FiltersActiveForUnsafeDetails_ReturnFalse()
        {
            var details = new Dictionary<string, bool>
            {
                { "rain", false },
                { "clouds", true }
            };
            var status = new SafetyStatus(false, details);

            var filters = new List<Filter>
            {
                new Filter
                {
                    Name = "rain",
                    Active = true
                },
                new Filter
                {
                    Name = "clouds",
                    Active = false,
                }
            };

            Assert.IsFalse(status.IsSafeWithFilters(filters));
        }

        [TestMethod]
        public void IsSafeWithFilters_FiltersActiveForSafeDetails_ReturnTrue()
        {
            var details = new Dictionary<string, bool>
            {
                { "rain", false },
                { "clouds", true }
            };
            var status = new SafetyStatus(false, details);

            var filters = new List<Filter>
            {
                new Filter
                {
                    Name = "rain",
                    Active = false
                },
                new Filter
                {
                    Name = "clouds",
                    Active = true,
                }
            };

            Assert.IsTrue(status.IsSafeWithFilters(filters));
        }
    }
}
