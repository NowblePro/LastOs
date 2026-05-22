using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using OsEngine.Market;
using OsEngine.OsTrader;

namespace OsEngine.Tests.ConfigRegression
{
    [TestFixture]
    [Category("ConfigRegression")]
    public class OsTraderMasterConfigRegressionTests
    {
        [Test]
        public void NormalizeTesterCloneConnectorSettings_UsesPreviousValidConnector_WhenCurrentFileIsMalformed()
        {
            string testRoot = Path.Combine(Path.GetTempPath(), "OsEngineTests", Guid.NewGuid().ToString("N"));
            string engineDir = Path.Combine(testRoot, "Engine");
            string testerSetDir = Path.Combine(testRoot, "TesterSet");
            string connectorFileName = "TLClone Natr Dot Ltab0ConnectorPrime.txt";
            string connectorPath = Path.Combine(engineDir, connectorFileName);

            Directory.CreateDirectory(engineDir);
            Directory.CreateDirectory(testerSetDir);

            try
            {
                File.WriteAllText(Path.Combine(testerSetDir, "DOTUSDT.P.txt"), string.Empty);
                File.WriteAllText(Path.Combine(engineDir, "TestServer.txt"), testerSetDir);
                File.WriteAllLines(connectorPath, new[] { "broken", "state" });

                Dictionary<string, string[]> previousSettings = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    [connectorFileName] = new[]
                    {
                        "LiveMode",
                        "True",
                        "DOTUSDT.P",
                        "Bybit",
                        "PrimeClass",
                        "True"
                    }
                };

                InvokePrivate(
                    CreateUninitializedMaster(),
                    "NormalizeTesterCloneConnectorSettings",
                    engineDir,
                    previousSettings);

                string[] normalizedLines = File.ReadAllLines(connectorPath);

                Assert.That(normalizedLines.Length, Is.EqualTo(6));
                Assert.That(normalizedLines[0], Is.EqualTo("GodMode"));
                Assert.That(normalizedLines[1], Is.EqualTo("False"));
                Assert.That(normalizedLines[2], Is.EqualTo("DOTUSDT.P.txt"));
                Assert.That(normalizedLines[3], Is.EqualTo(ServerType.Tester.ToString()));
                Assert.That(normalizedLines[4], Is.EqualTo("TestClass"));
                Assert.That(normalizedLines[5], Is.EqualTo("True"));
            }
            finally
            {
                if (Directory.Exists(testRoot))
                {
                    Directory.Delete(testRoot, true);
                }
            }
        }

        [Test]
        public void NormalizeTesterCloneConnectorSettings_ResolvesTesterSecurityAndPreservesCurrentEventsFlag()
        {
            string testRoot = Path.Combine(Path.GetTempPath(), "OsEngineTests", Guid.NewGuid().ToString("N"));
            string engineDir = Path.Combine(testRoot, "Engine");
            string testerSetDir = Path.Combine(testRoot, "TesterSet");
            string nestedDataDir = Path.Combine(testerSetDir, "BybitServerCandles");
            string connectorPath = Path.Combine(engineDir, "TLClone Natr AAVE Ltab0ConnectorPrime.txt");

            Directory.CreateDirectory(engineDir);
            Directory.CreateDirectory(nestedDataDir);

            try
            {
                File.WriteAllText(Path.Combine(nestedDataDir, "AAVEUSDT.P.txt"), string.Empty);
                File.WriteAllText(Path.Combine(engineDir, "TestServer.txt"), testerSetDir);
                File.WriteAllLines(
                    connectorPath,
                    new[]
                    {
                        "LiveMode",
                        "True",
                        "AAVEUSDT.P",
                        "Bybit",
                        "PrimeClass",
                        "False"
                    });

                InvokePrivate(
                    CreateUninitializedMaster(),
                    "NormalizeTesterCloneConnectorSettings",
                    engineDir,
                    new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase));

                string[] normalizedLines = File.ReadAllLines(connectorPath);

                Assert.That(normalizedLines.Length, Is.EqualTo(6));
                Assert.That(normalizedLines[0], Is.EqualTo("GodMode"));
                Assert.That(normalizedLines[1], Is.EqualTo("False"));
                Assert.That(normalizedLines[2], Is.EqualTo("AAVEUSDT.P.txt"));
                Assert.That(normalizedLines[3], Is.EqualTo(ServerType.Tester.ToString()));
                Assert.That(normalizedLines[4], Is.EqualTo("TestClass"));
                Assert.That(normalizedLines[5], Is.EqualTo("False"));
            }
            finally
            {
                if (Directory.Exists(testRoot))
                {
                    Directory.Delete(testRoot, true);
                }
            }
        }

        private static OsTraderMaster CreateUninitializedMaster()
        {
            return (OsTraderMaster)FormatterServices.GetUninitializedObject(typeof(OsTraderMaster));
        }

        private static object InvokePrivate(object instance, string methodName, params object[] args)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, $"Private method '{methodName}' was not found.");

            return method.Invoke(instance, args);
        }
    }
}
