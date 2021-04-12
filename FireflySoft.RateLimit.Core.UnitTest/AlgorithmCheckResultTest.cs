using System;
using System.Collections.Generic;
using System.Linq;
using FireflySoft.RateLimit.Core.Rule;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FireflySoft.RateLimit.Core.UnitTest
{
    [TestClass]
    public class AlgorithmCheckResultTest
    {
        [DataTestMethod]
        public void TestIsLimit()
        {
            Assert.AreEqual(false, result.IsLimit);
        }

        [DataTestMethod]
        public void TestRuleCheckResults()
        {
            for (int k = 0; k < 30; k++)
            {
                int i = 0;
                foreach (var r in result.RuleCheckResults)
                {
                    if (i == 0) Assert.AreEqual(false, r.IsLimit);
                    if (i == 1) Assert.AreEqual(false, r.IsLimit);
                    i++;
                }
            }

            Assert.AreEqual(2, result.RuleCheckResults.Count());
        }

        [TestInitialize()]
        public void TestInitialize()
        {
            List<RuleCheckResult> results = new List<RuleCheckResult>();
            var checks = GetRuleCheckResults();
            foreach (var c in checks)
            {
                results.Add(c);
            }

            result = new AlgorithmCheckResult(results);
            _countValue1 = 0;
            _countValue2 = 0;
        }

        private IEnumerable<RuleCheckResult> GetRuleCheckResults()
        {
            yield return Check("1", 10);
            yield return Check("2", 20);
        }

        private AlgorithmCheckResult result;
        private int _countValue1;
        private int _countValue2;

        private RuleCheckResult Check(string ruleId, int limit)
        {
            var _countValue = 0;
            if (ruleId == "1")
                _countValue = ++_countValue1;
            else
                _countValue = ++_countValue2;

            return new RuleCheckResult()
            {
                IsLimit = _countValue > limit,
                Count = _countValue,
                Target = "/home",
                Wait = -1,
                Rule = new FixedWindowRule()
                {
                    Id = ruleId,
                    Name = ruleId,
                    StatWindow = TimeSpan.FromSeconds(1),
                    LimitNumber = limit
                }
            };
        }
    }
}