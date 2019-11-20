using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MessageFiltering_EustonLeisure;

namespace UnitTests
{
    [TestClass]
    public class HeaderValidationTest
    {
        private TestContext testContextInstance;
        
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV",
                    "|DataDirectory|\\HeaderTestingData.csv", "HeaderTestingData#csv", DataAccessMethod.Sequential),
                    DeploymentItem("HeaderTestingData.csv")]
        public void HeaderValidation()
        {
            string pattern = "((?:[S|E|T]+[0-9]{9}))";
            var headerValidation = new MessageFiltering_EustonLeisure.MainWindow();

            var headerValue = TestContext.DataRow["header"].ToString();
            var expectedResult = Convert.ToBoolean(TestContext.DataRow["expected"].ToString());

            var actualResult = headerValidation.RegexCheck(pattern, headerValue);

            Assert.AreEqual(actualResult, expectedResult);
        }
    }
}
