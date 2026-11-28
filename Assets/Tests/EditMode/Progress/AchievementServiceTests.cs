using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using WeatherForSale.Progress;
using WeatherForSale.Shop;

namespace WeatherForSale.Tests.Progress
{
    /// <summary>Story 005: achievement conditions, no repeat unlock, save round trip, damaged data.</summary>
    public sealed class AchievementServiceTests
    {
        private sealed class MemoryStore : IProgressStore
        {
            public string Json;
            public int Saves;
            public string Load() => Json;
            public void Save(string json) { Json = json; Saves++; }
        }

        private static AchievementRules Rules() => new AchievementRules { deathsNeeded = 10, starsNeeded = 12 };

        private ProgressService service;
        private MemoryStore store;
        private List<AchievementId> unlocked;

        [SetUp]
        public void SetUp()
        {
            service = ScriptableObject.CreateInstance<ProgressService>();
            store = new MemoryStore();
            service.UseStore(store);
            unlocked = new List<AchievementId>();
            service.AchievementUnlocked += unlocked.Add;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(service);
        }

        [Test]
        public void Evaluate_NineDeaths_DoesNotUnlockDie10()
        {
            var data = new ProgressData { totalDeaths = 9 };
            Assert.IsEmpty(new AchievementService(Rules()).Evaluate(data));
        }

        [Test]
        public void Evaluate_TenDeaths_UnlocksDie10()
        {
            var data = new ProgressData { totalDeaths = 10 };
            CollectionAssert.AreEqual(new[] { AchievementId.Die10Times }, new AchievementService(Rules()).Evaluate(data));
        }

        [Test]
        public void Evaluate_TwelveStars_UnlocksEarn12Stars()
        {
            var data = new ProgressData();
            foreach (string map in new[] { "a", "b", "c", "d" }) data.bestStars.Add(new MapStars { mapId = map, stars = 3 });
            CollectionAssert.AreEqual(new[] { AchievementId.Earn12Stars }, new AchievementService(Rules()).Evaluate(data));
        }

        [Test]
        public void Evaluate_EndingsSeen_UnlockBothEndingAchievements()
        {
            var data = new ProgressData { normalEndingSeen = true, trueEndingSeen = true };
            CollectionAssert.AreEqual(new[] { AchievementId.NormalEnding, AchievementId.TrueEnding },
                new AchievementService(Rules()).Evaluate(data));
        }

        [Test]
        public void Evaluate_SecondTime_DoesNotUnlockAgain()
        {
            var data = new ProgressData { totalDeaths = 12 };
            var achievements = new AchievementService(Rules());
            achievements.Evaluate(data);
            Assert.IsEmpty(achievements.Evaluate(data));
            Assert.AreEqual(1, data.achievements.Count);
        }

        [Test]
        public void RecordDeaths_ReachingThreshold_RaisesUnlockOnce()
        {
            service.RecordDeaths(9);
            Assert.IsEmpty(unlocked);
            service.RecordDeaths(1);
            service.RecordDeaths(5);
            CollectionAssert.AreEqual(new[] { AchievementId.Die10Times }, unlocked);
            Assert.IsTrue(service.IsUnlocked(AchievementId.Die10Times));
        }

        [Test]
        public void RecordMapCleared_KeepsBestStars()
        {
            service.RecordMapCleared("valley", 3);
            service.RecordMapCleared("valley", 1);
            Assert.AreEqual(3, service.Data.BestStars("valley"));
            CollectionAssert.Contains(service.Data.unlockedMaps, "valley");
        }

        [Test]
        public void RecordEnding_True_UnlocksTrueEndingOnly()
        {
            service.RecordEnding(EndingKind.True);
            CollectionAssert.AreEqual(new[] { AchievementId.TrueEnding }, unlocked);
        }

        [Test]
        public void Save_ThenReloadFromSameStore_KeepsProgress()
        {
            service.RecordDeaths(10);
            service.RecordShard("glacier");
            service.RecordMapCleared("desert", 2);
            service.SetSecretBossUnlocked(true);

            var reloaded = ScriptableObject.CreateInstance<ProgressService>();
            reloaded.UseStore(new MemoryStore { Json = store.Json });
            ProgressData data = reloaded.Data;
            Assert.AreEqual(10, data.totalDeaths);
            CollectionAssert.Contains(data.shardMaps, "glacier");
            Assert.AreEqual(2, data.BestStars("desert"));
            Assert.IsTrue(data.secretBossUnlocked);
            Assert.IsTrue(reloaded.IsUnlocked(AchievementId.Die10Times));
            Assert.AreEqual(ProgressData.CurrentVersion, data.version);
            Object.DestroyImmediate(reloaded);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("{not json")]
        [TestCase("{\"totalDeaths\":3}")]
        public void Parse_EmptyOrDamaged_GivesUsableDefaults(string json)
        {
            ProgressData data = ProgressService.Parse(json);
            Assert.IsNotNull(data.shardMaps);
            Assert.IsNotNull(data.bestStars);
            Assert.IsNotNull(data.achievements);
            Assert.IsNotNull(data.unlockedMaps);
        }

        [Test]
        public void Load_DamagedStore_StartsFresh()
        {
            service.UseStore(new MemoryStore { Json = "garbage" });
            Assert.AreEqual(0, service.Data.totalDeaths);
            Assert.IsFalse(service.IsUnlocked(AchievementId.TrueEnding));
        }
    }
}
