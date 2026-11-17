using System;
using System.Diagnostics;
using NUnit.Framework;
using SMPro.Inspector.EditorTools;
using UnityEditor;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace SMPro.Inspector.Tests
{
    public class BenchmarkTests : InspectorTestBase
    {
        static double AverageMilliseconds(int runs, Action action)
        {
            action();
            Stopwatch watch = Stopwatch.StartNew();
            for (int i = 0; i < runs; i++) action();
            watch.Stop();
            return watch.Elapsed.TotalMilliseconds / runs;
        }

        [Test]
        public void TypeWithoutAttributes_CostsOneCachedLookup()
        {
            double milliseconds = AverageMilliseconds(100000, () => TypeModel.For(typeof(PlainTestComponent)));

            Debug.Log($"Benchmark: unattributed type check {milliseconds * 1000.0:0.000} µs");
            Assert.Less(milliseconds, 0.01);
        }

        [Test]
        public void TypeModel_For50Members_BuildsQuickly()
        {
            double milliseconds = AverageMilliseconds(50, () => TypeModel.Uncached(typeof(WideTestComponent)));

            Debug.Log($"Benchmark: type model, 50 members, uncached {milliseconds:0.000} ms");
            Assert.Less(milliseconds, 2.0);
        }

        [Test]
        public void StateRefresh_Of50Conditions_IsFast_AndAllocatesNothing()
        {
            InspectorRefresher refresher = InspectorBuilder.RefresherOf(Inspect(NewComponent<ConditionStressTestComponent>()));

            double milliseconds = AverageMilliseconds(1000, refresher.RefreshStates);

            Debug.Log($"Benchmark: state refresh, 50 conditions {milliseconds:0.0000} ms");
            Assert.Less(milliseconds, 0.1);
            Assert.That(() => refresher.RefreshStates(), UnityEngine.TestTools.Constraints.Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void StressInspector_200Fields_10Levels_1000Elements()
        {
            var component = NewComponent<StressTestComponent>();
            for (int i = 0; i < 1000; i++) component.items.Add(new StressTestComponent.Item { active = i % 2 == 0 });
            var serializedObject = new SerializedObject(component);
            VisualElement root = null;

            double build = AverageMilliseconds(3, () => root = InspectorBuilder.Build(serializedObject));
            InspectorRefresher refresher = InspectorBuilder.RefresherOf(root);
            double states = AverageMilliseconds(20, refresher.RefreshStates);
            double full = AverageMilliseconds(5, refresher.Refresh);

            Debug.Log($"Benchmark: stress inspector ({refresher.Properties.Count} properties) build {build:0.0} ms, state refresh {states:0.000} ms, full refresh {full:0.00} ms");
            Assert.Less(build, 3000.0);
            Assert.Less(states, 5.0);
        }
    }
}
