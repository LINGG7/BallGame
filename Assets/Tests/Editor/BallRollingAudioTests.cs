using System;
using System.Reflection;
using NUnit.Framework;

namespace BallGame.Tests
{
    public sealed class BallRollingAudioTests
    {
        private static Type BallType => Type.GetType("BallGame.Core.BallController, Assembly-CSharp");

        [Test]
        public void RollingReportCondition_RequiresDynamicGroundedMovingBallOutsideGoal()
        {
            Assert.That(ShouldReport(true, false, true, 0.5f), Is.True);
            Assert.That(ShouldReport(false, false, true, 0.5f), Is.False, "Build mode must be silent.");
            Assert.That(ShouldReport(true, true, true, 0.5f), Is.False, "A ball at the goal must be silent.");
            Assert.That(ShouldReport(true, false, false, 0.5f), Is.False, "An airborne ball must be silent.");
            Assert.That(ShouldReport(true, false, true, 0f), Is.False, "A stationary ball must be silent.");
        }

        private static bool ShouldReport(
            bool isDynamic,
            bool isStoppedAtGoal,
            bool hasRollingContact,
            float normalizedSpeed)
        {
            Assert.That(BallType, Is.Not.Null);
            MethodInfo method = BallType.GetMethod(
                "ShouldReportRollingAudio",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (bool)method.Invoke(null, new object[]
            {
                isDynamic,
                isStoppedAtGoal,
                hasRollingContact,
                normalizedSpeed
            });
        }
    }
}
