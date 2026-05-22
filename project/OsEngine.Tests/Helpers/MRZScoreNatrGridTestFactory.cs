using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using OsEngine.Common;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Indicators.TrigonumCustom;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Robots.TrigonumCustom.Base;

namespace OsEngine.Tests.Helpers
{
    internal static class MRZScoreNatrGridTestFactory
    {
        public static MRZScoreNatrGrid CreateUninitializedRobot()
        {
            return (MRZScoreNatrGrid)FormatterServices.GetUninitializedObject(typeof(MRZScoreNatrGrid));
        }

        public static MRZScoreNatrGrid CreateRecoveryConfiguredRobot(int threshold, int seriesCount, decimal multiplier)
        {
            MRZScoreNatrGrid robot = CreateUninitializedRobot();
            Type levelType = GetGridLevelStateType();

            SetField(robot, "_debugLogging", new StrategyParameterBool("Debug Logging", false));
            SetField(robot, "_gridLevels", CreateGridLevelsList(levelType));
            SetField(robot, "_levelsAwaitingOpeningSuccess", CreateGridLevelsList(levelType));
            SetField(robot, "_levelBindingsByReference", Activator.CreateInstance(GetFieldType("_levelBindingsByReference")));
            SetField(robot, "_levelBindingsByNumber", Activator.CreateInstance(GetFieldType("_levelBindingsByNumber")));
            SetField(robot, "_levelBindingsByOrderUserNumber", Activator.CreateInstance(GetFieldType("_levelBindingsByOrderUserNumber")));
            SetField(robot, "_levelBindingsByOrderMarketNumber", Activator.CreateInstance(GetFieldType("_levelBindingsByOrderMarketNumber")));
            SetProtectedField(
                robot,
                typeof(OsEngine.Robots.TrigonumCustom.BotPanelSimple),
                "_plannedEntryAnchors",
                Activator.CreateInstance(GetFieldType(typeof(OsEngine.Robots.TrigonumCustom.BotPanelSimple), "_plannedEntryAnchors")));
            SetField(robot, "_recoveryAfterLossEnable", new StrategyParameterBool("Recovery After Loss Enable", true));
            SetField(robot, "_recoveryAfterLossLevelThreshold", new StrategyParameterInt("Recovery Loss Level Threshold", threshold, 0, 20, 1));
            SetField(robot, "_recoveryAfterLossSeriesCount", new StrategyParameterInt("Recovery Series Count", seriesCount, 1, 20, 1));
            SetField(robot, "_recoveryAfterLossVolumeMultiplier", new StrategyParameterDecimal("Recovery Volume Multiplier", multiplier, 1m, 10m, 0.1m));
            SetField(robot, "_recoverySeriesRemaining", 0);
            SetField(robot, "_volumeManager", new MeanReverseVolumeManager());

            return robot;
        }

        public static MRZScoreNatrGrid CreateConfiguredGridRobot(
            decimal seriesVolumeMultiplier,
            decimal sma = 100m,
            decimal atr = 1m,
            decimal lowChannel = 98m,
            decimal highChannel = 102m,
            decimal? ema = null,
            bool emaEnabled = false,
            bool emaReverse = false)
        {
            MRZScoreNatrGrid robot = CreateUninitializedRobot();

            SetField(robot, "StartProgram", StartProgram.IsTester);
            SetField(robot, "_tab", CreateTestTab());
            SetField(robot, "_debugLogging", new StrategyParameterBool("Debug Logging", false));
            SetField(robot, "_gridLevels", CreateGridLevelsList(GetGridLevelStateType()));
            SetField(robot, "_levelsAwaitingOpeningSuccess", CreateGridLevelsList(GetGridLevelStateType()));
            SetField(robot, "_levelBindingsByReference", Activator.CreateInstance(GetFieldType("_levelBindingsByReference")));
            SetField(robot, "_levelBindingsByNumber", Activator.CreateInstance(GetFieldType("_levelBindingsByNumber")));
            SetField(robot, "_levelBindingsByOrderUserNumber", Activator.CreateInstance(GetFieldType("_levelBindingsByOrderUserNumber")));
            SetField(robot, "_levelBindingsByOrderMarketNumber", Activator.CreateInstance(GetFieldType("_levelBindingsByOrderMarketNumber")));
            SetProtectedField(
                robot,
                typeof(OsEngine.Robots.TrigonumCustom.BotPanelSimple),
                "_plannedEntryAnchors",
                Activator.CreateInstance(GetFieldType(typeof(OsEngine.Robots.TrigonumCustom.BotPanelSimple), "_plannedEntryAnchors")));
            SetField(robot, "_gridSize", new StrategyParameterInt("Grid Size", 3, 1, 20, 1));
            SetField(robot, "_fixPercent", new StrategyParameterDecimal("Fix Percent", 2m, 0.1m, 10m, 0.1m));
            SetField(robot, "_natrMult", new StrategyParameterDecimal("NATR Multiplier", 1.25m, 0m, 10m, 0.1m));
            SetField(robot, "_orderType", new StrategyParameterString("OrderType", OrderType.MarketNextOpen.ToString()));
            SetField(robot, "_recoveryAfterLossEnable", new StrategyParameterBool("Recovery After Loss Enable", seriesVolumeMultiplier > 1m));
            SetField(robot, "_recoveryAfterLossLevelThreshold", new StrategyParameterInt("Recovery Loss Level Threshold", 0, 0, 20, 1));
            SetField(robot, "_recoveryAfterLossSeriesCount", new StrategyParameterInt("Recovery Series Count", 1, 1, 20, 1));
            SetField(robot, "_recoveryAfterLossVolumeMultiplier", new StrategyParameterDecimal("Recovery Volume Multiplier", seriesVolumeMultiplier, 1m, 10m, 0.1m));
            SetField(robot, "_recoverySeriesRemaining", seriesVolumeMultiplier > 1m ? 1 : 0);
            SetField(robot, "_volumeManager", new MeanReverseVolumeManager
            {
                R = 50m,
                GetVolumeFunc = _ => 10m,
                Rounding = value => decimal.Round(value, 2)
            });

            FakeIndicator smaIndicator = new FakeIndicator(sma);
            FakeIndicator natrIndicator = new FakeIndicator(atr);
            SetField(robot, "_sma", smaIndicator);
            SetField(robot, "_natrAtr", natrIndicator);
            SetField(robot, "_channel", CreateChannel(lowChannel, highChannel));

            if (ema.HasValue)
            {
                FakeIndicator emaIndicator = new FakeIndicator(ema.Value);
                SetField(robot, "_ema", emaIndicator);
                SetField(robot, "_canEnterByEma", CreateEmaFilter(emaIndicator, emaEnabled, emaReverse));
            }
            else
            {
                SetField(robot, "_canEnterByEma", null);
            }

            SetField(robot, "_change24", null);
            SetProtectedField(robot, typeof(OsEngine.Robots.TrigonumCustom.BotPanelSimple), "_regime", ParseNestedEnum(typeof(OsEngine.Robots.TrigonumCustom.BotPanelSimple), "BotRegime", "On"));

            return robot;
        }

        public static CanEnterByEmaDecoration CreateEmaFilter(Aindicator emaIndicator, bool enabled, bool reverse)
        {
            CanEnterByEmaDecoration filter = (CanEnterByEmaDecoration)FormatterServices.GetUninitializedObject(typeof(CanEnterByEmaDecoration));
            SetField(filter, "_enabled", new StrategyParameterBool("Ema Filter Enabled", enabled));
            SetField(filter, "_reverse", new StrategyParameterBool("Ema Filter Reverse", reverse));
            SetField(filter, "_ema", emaIndicator);
            SetField(filter, "_emaPeriod", 1m);
            return filter;
        }

        public static BotTabSimple CreateTestTab()
        {
            BotTabSimple tab = (BotTabSimple)FormatterServices.GetUninitializedObject(typeof(BotTabSimple));
            tab.StartProgram = StartProgram.IsTester;
            Security security = new Security
            {
                Name = "TESTUSDT.P",
                NameClass = "TestClass",
                PriceStep = 0.01m,
                PriceStepCost = 0.01m,
                Lot = 1m,
                DecimalsVolume = 2,
                MinTradeAmount = 0.01m
            };
            SetProtectedField(tab, typeof(BotTabSimple), "_security", security);
            return tab;
        }

        public static ZScoreChannel CreateChannel(decimal low, decimal high)
        {
            ZScoreChannel channel = (ZScoreChannel)FormatterServices.GetUninitializedObject(typeof(ZScoreChannel));

            SetProtectedField(channel, typeof(Aindicator), "DataSeries", new List<IndicatorDataSeries>());
            SetProtectedField(
                channel,
                typeof(ZScoreChannel),
                "_channelDataLow",
                new IndicatorDataSeries(Color.Yellow, "Low", IndicatorChartPaintType.Line, false)
                {
                    Values = new List<decimal> { low }
                });
            SetProtectedField(
                channel,
                typeof(ZScoreChannel),
                "_channelDataHigh",
                new IndicatorDataSeries(Color.Green, "High", IndicatorChartPaintType.Line, false)
                {
                    Values = new List<decimal> { high }
                });

            return channel;
        }

        public static Type GetGridLevelStateType()
        {
            Type levelType = typeof(MRZScoreNatrGrid).GetNestedType("GridLevelState", BindingFlags.NonPublic);
            Assert.That(levelType, Is.Not.Null, "GridLevelState nested type was not found.");
            return levelType;
        }

        public static IList CreateGridLevelsList(Type levelType)
        {
            Type listType = typeof(List<>).MakeGenericType(levelType);
            return (IList)Activator.CreateInstance(listType);
        }

        public static object CreateGridLevel(Type levelType, int index, bool consumed)
        {
            object level = FormatterServices.GetUninitializedObject(levelType);
            SetField(level, "Index", index);
            SetField(level, "Consumed", consumed);
            return level;
        }

        public static object InvokePrivate(object instance, string methodName, params object[] args)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Private method '{methodName}' was not found.");
            return method.Invoke(instance, args);
        }

        public static object GetField(object instance, string fieldName)
        {
            FieldInfo field = FindField(instance.GetType(), fieldName);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            return field.GetValue(instance);
        }

        public static void SetField(object instance, string fieldName, object value)
        {
            FieldInfo field = FindField(instance.GetType(), fieldName);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            field.SetValue(instance, value);
        }

        public static void SetProtectedField(object instance, Type declaringType, string fieldName, object value)
        {
            FieldInfo field = declaringType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found on '{declaringType.Name}'.");
            field.SetValue(instance, value);
        }

        public static object ParseNestedEnum(Type ownerType, string enumName, string value)
        {
            Type enumType = ownerType.GetNestedType(enumName, BindingFlags.NonPublic);
            Assert.That(enumType, Is.Not.Null, $"Nested enum '{enumName}' was not found on '{ownerType.Name}'.");
            return Enum.Parse(enumType, value);
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (field != null)
                {
                    return field;
                }

                type = type.BaseType;
            }

            return null;
        }

        private static Type GetFieldType(string fieldName)
        {
            FieldInfo field = FindField(typeof(MRZScoreNatrGrid), fieldName);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            return field.FieldType;
        }

        private static Type GetFieldType(Type ownerType, string fieldName)
        {
            FieldInfo field = FindField(ownerType, fieldName);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found on '{ownerType.Name}'.");
            return field.FieldType;
        }
    }
}
