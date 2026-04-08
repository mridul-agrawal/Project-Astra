using NUnit.Framework;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Tests
{
    [TestFixture]
    public class WeaponTriangleTests
    {
        [Test] public void Sword_BeatsAxe() => Assert.AreEqual(1, WeaponTriangle.ComputePhysicalAdvantage(WeaponType.Sword, WeaponType.Axe));
        [Test] public void Axe_BeatsLance() => Assert.AreEqual(1, WeaponTriangle.ComputePhysicalAdvantage(WeaponType.Axe, WeaponType.Lance));
        [Test] public void Lance_BeatsSword() => Assert.AreEqual(1, WeaponTriangle.ComputePhysicalAdvantage(WeaponType.Lance, WeaponType.Sword));

        [Test] public void Axe_LosesToSword() => Assert.AreEqual(-1, WeaponTriangle.ComputePhysicalAdvantage(WeaponType.Axe, WeaponType.Sword));
        [Test] public void Lance_LosesToAxe() => Assert.AreEqual(-1, WeaponTriangle.ComputePhysicalAdvantage(WeaponType.Lance, WeaponType.Axe));
        [Test] public void Sword_LosesToLance() => Assert.AreEqual(-1, WeaponTriangle.ComputePhysicalAdvantage(WeaponType.Sword, WeaponType.Lance));

        [Test] public void SameType_Neutral() => Assert.AreEqual(0, WeaponTriangle.ComputePhysicalAdvantage(WeaponType.Sword, WeaponType.Sword));
        [Test] public void Bow_AlwaysNeutral() => Assert.AreEqual(0, WeaponTriangle.ComputePhysicalAdvantage(WeaponType.Bow, WeaponType.Sword));
        [Test] public void Staff_AlwaysNeutral() => Assert.AreEqual(0, WeaponTriangle.ComputePhysicalAdvantage(WeaponType.Staff, WeaponType.Lance));

        [Test] public void Anima_BeatsDark() => Assert.AreEqual(1, WeaponTriangle.ComputeMagicAdvantage(MagicSchool.Anima, MagicSchool.Dark));
        [Test] public void Dark_BeatsLight() => Assert.AreEqual(1, WeaponTriangle.ComputeMagicAdvantage(MagicSchool.Dark, MagicSchool.Light));
        [Test] public void Light_BeatsAnima() => Assert.AreEqual(1, WeaponTriangle.ComputeMagicAdvantage(MagicSchool.Light, MagicSchool.Anima));

        [Test] public void Dark_LosesToAnima() => Assert.AreEqual(-1, WeaponTriangle.ComputeMagicAdvantage(MagicSchool.Dark, MagicSchool.Anima));
        [Test] public void MagicNone_Neutral() => Assert.AreEqual(0, WeaponTriangle.ComputeMagicAdvantage(MagicSchool.None, MagicSchool.Anima));

        [Test]
        public void CrossType_PhysicalVsMagic_NoTriangle()
        {
            var sword = WeaponData.IronSword;
            var fire = WeaponData.Fire;
            Assert.AreEqual(0, WeaponTriangle.ComputeAdvantage(sword, fire));
        }

        [Test]
        public void HitBonus_Advantage_Plus15() => Assert.AreEqual(15, WeaponTriangle.GetHitBonus(1));
        [Test]
        public void HitBonus_Disadvantage_Minus15() => Assert.AreEqual(-15, WeaponTriangle.GetHitBonus(-1));
        [Test]
        public void DamageBonus_Advantage_Plus1() => Assert.AreEqual(1, WeaponTriangle.GetDamageBonus(1));
        [Test]
        public void AvoidBonus_Advantage_Plus15() => Assert.AreEqual(15, WeaponTriangle.GetAvoidBonus(1));
    }
}
