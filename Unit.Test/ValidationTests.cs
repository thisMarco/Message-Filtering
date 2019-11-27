using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Unit.Test
{
    [TestClass]
    public class ValidationTests
    {
        private TestContext testContextInstance;

        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }
        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV",
            "|DataDirectory|\\HData.csv", "HData#csv", DataAccessMethod.Sequential),
            DeploymentItem("HData.csv")]
        public void HeaderValidation()
        {
            var mainWindow = new MessageFiltering_EustonLeisure.MainWindow();

            var headerString = TestContext.DataRow["header"].ToString();
            var expectedResult = Boolean.Parse(TestContext.DataRow["expected"].ToString());

            var actualResult = mainWindow.RegexCheck("((?:[S|E|T]+[0-9]{9}))", headerString);
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV",
            "|DataDirectory|\\TweetIDTestingData.csv", "TweetIDTestingData#csv", DataAccessMethod.Sequential),
            DeploymentItem("TweetIDTestingData.csv")]
        public void TweetIDTest()
        {
            var mainWindow = new MessageFiltering_EustonLeisure.MainWindow();

            var headerString = TestContext.DataRow["id"].ToString();
            var expectedResult = Boolean.Parse(TestContext.DataRow["expected"].ToString());

            var actualResult = mainWindow.RegexCheck(@"(@)+(\w){1,15}$", headerString);
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV",
            "|DataDirectory|\\HashtagTestingData.csv", "HashtagTestingData#csv", DataAccessMethod.Sequential),
            DeploymentItem("HashtagTestingData.csv")]
        public void HashtagTest()
        {
            var mainWindow = new MessageFiltering_EustonLeisure.MainWindow();

            var headerString = TestContext.DataRow["hashtag"].ToString();
            var expectedResult = Boolean.Parse(TestContext.DataRow["expected"].ToString());

            var actualResult = mainWindow.RegexCheck(@"(#)\w+", headerString);
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV",
            "|DataDirectory|\\HyperTest.csv", "HyperTest#csv", DataAccessMethod.Sequential),
            DeploymentItem("HyperTest.csv")]
        public void HyperlinkTest()
        {
            var mainWindow = new MessageFiltering_EustonLeisure.MainWindow();

            var urlString = TestContext.DataRow["link"].ToString();
            var expectedResult = Boolean.Parse(TestContext.DataRow["expected"].ToString());

            var actualResult = mainWindow.RegexCheck(@"(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&amp;%\$#_]*)", urlString);
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV",
            "|DataDirectory|\\SubjectTest.csv", "SubjectTest#csv", DataAccessMethod.Sequential),
            DeploymentItem("SubjectTest.csv")]
        public void SubjectTest()
        {
            var mainWindow = new MessageFiltering_EustonLeisure.MainWindow();

            var subjectString = TestContext.DataRow["subject"].ToString();
            var expectedResult = Boolean.Parse(TestContext.DataRow["expected"].ToString());

            var actualResult = false;

            if (subjectString.Length > 0 && subjectString.Length <= 20)
                actualResult = true;

            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV",
            "|DataDirectory|\\SIRTest.csv", "SIRTest#csv", DataAccessMethod.Sequential),
            DeploymentItem("SIRTest.csv")]
        public void SIRTest()
        {
            var mainWindow = new MessageFiltering_EustonLeisure.MainWindow();

            var SIRString = TestContext.DataRow["sir"].ToString();
            var expectedResult = Boolean.Parse(TestContext.DataRow["expected"].ToString());

            var actualResult = mainWindow.RegexCheck(@"(SIR (0[1-9]|1[0-9]|2[0-9]|3[0-1])[\/](0[1-9]|1[0-2])[\/](0[0-9]|1[0-9]|2[0-9]))", SIRString);
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        [DataSource("Microsoft.VisualStudio.TestTools.DataSource.CSV",
            "|DataDirectory|\\CentreCodeTest.csv", "CentreCodeTest#csv", DataAccessMethod.Sequential),
            DeploymentItem("CentreCodeTest.csv")]
        public void CentreCodeTest()
        {
            var mainWindow = new MessageFiltering_EustonLeisure.MainWindow();

            var SIRString = TestContext.DataRow["code"].ToString();
            var expectedResult = Boolean.Parse(TestContext.DataRow["expected"].ToString());

            var actualResult = mainWindow.RegexCheck(@"(\d\d)+(-)+(\d\d\d)+(-)+(\d\d)", SIRString);
            Assert.AreEqual(expectedResult, actualResult);
        }
    }
}
