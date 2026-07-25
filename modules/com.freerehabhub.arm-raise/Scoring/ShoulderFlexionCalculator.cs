using System;
using FreeRehabHub.Modules.Contracts;

namespace FreeRehabHub.Modules.ArmRaise.Scoring;

// Omuz fleksiyon açısını hesaplar: kalça->omuz vektörüne göre omuz->dirsek vektörünün açısı.
// 0° kol gövdenin yanında sarkarken, ~90° kol yatayken, ~180° kol tam kaldırılmışken.
public static class ShoulderFlexionCalculator
{
    public static double CalculateFlexionAngleDegrees(PosePoint hip, PosePoint shoulder, PosePoint elbow)
    {
        var torsoDownX = hip.X - shoulder.X;
        var torsoDownY = hip.Y - shoulder.Y;
        var torsoDownZ = hip.Z - shoulder.Z;

        var armX = elbow.X - shoulder.X;
        var armY = elbow.Y - shoulder.Y;
        var armZ = elbow.Z - shoulder.Z;

        var dot = (torsoDownX * armX) + (torsoDownY * armY) + (torsoDownZ * armZ);
        var torsoMagnitude = Math.Sqrt((torsoDownX * torsoDownX) + (torsoDownY * torsoDownY) + (torsoDownZ * torsoDownZ));
        var armMagnitude = Math.Sqrt((armX * armX) + (armY * armY) + (armZ * armZ));

        if (torsoMagnitude == 0.0 || armMagnitude == 0.0)
        {
            return 0.0;
        }

        var cosAngle = Math.Clamp(dot / (torsoMagnitude * armMagnitude), -1.0, 1.0);
        return Math.Acos(cosAngle) * (180.0 / Math.PI);
    }
}
