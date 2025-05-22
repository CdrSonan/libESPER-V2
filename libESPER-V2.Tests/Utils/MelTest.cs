using System;
using System.Linq;
using libESPER_V2.Utils;
using MathNet.Numerics.LinearAlgebra;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace libESPER_V2.Tests.Utils;

[TestFixture]
[TestOf(typeof(Mel))]
public class MelTest
{
    [Test]
    [TestCase(0f, 0f)]
    [TestCase(700f, 781.17f)]
    [TestCase(1000f, 999.98f)]
    [TestCase(-100f, -173.73f)]
    public void HzToMel_ValidInputs_ReturnsExpected(float hz, float result)
    {
        var mel = Mel.HzToMel(hz);
        ClassicAssert.AreEqual(result, mel, 0.01f);
    }

    [Test]
    [TestCase(0f, 0f)]
    [TestCase(700f, 602.71f)]
    [TestCase(1000f, 1000.02f)]
    [TestCase(-100f, -59.44f)]
    public void MelToHz_ValidInputs_ReturnsExpected(float mel, float result)
    {
        var hz = Mel.MelToHz(mel);
        ClassicAssert.AreEqual(result, hz, 0.01f);
    }


    [Test]
    [TestCase(0f, 10, 100f)]
    [TestCase(0.4f, 20, 200f)]
    public void MelFwd_ConstantInputs_ReturnsExpected(float xValue, int numMelBands, float maxFreq)
    {
        // Arrange
        const int inputVectorLength = 100; // Define the desired input vector length
        var x = Vector<float>.Build.Dense(inputVectorLength, xValue);

        // Act
        var mel = Mel.MelFwd(x, numMelBands, maxFreq);

        // Assert
        ClassicAssert.AreEqual(numMelBands, mel.Count,
            "The output vector does not have the expected number of mel bands.");
        ClassicAssert.IsTrue(mel.All(value => Math.Abs(value - xValue * inputVectorLength / numMelBands) < 0.01f),
            "The output does not distribute constant values evenly across mel bands.");
    }

    [Test]
    [TestCase(0f, 10, 100f)]
    [TestCase(0.4f, 20, 200f)]
    public void MelInv_ConstantInputs_ReturnsExpected(float xValue, int numMelBands, float maxFreq)
    {
        // Arrange
        const int inputVectorLength = 100; // Define the desired input vector length
        var mel = Vector<float>.Build.Dense(numMelBands, xValue);

        // Act
        var x = Mel.MelInv(mel, inputVectorLength, maxFreq);

        // Assert
        ClassicAssert.AreEqual(inputVectorLength, x.Count,
            "The output vector does not have the expected length.");
        ClassicAssert.IsTrue(x.All(value => Math.Abs(value - xValue * numMelBands / inputVectorLength) < 0.01f),
            "The output does not distribute constant values evenly across the input vector.");
    }
}