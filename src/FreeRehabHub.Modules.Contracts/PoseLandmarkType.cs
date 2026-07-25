namespace FreeRehabHub.Modules.Contracts;

// MediaPipe PoseLandmarker'ın ürettiği 33 sabit BlazePose landmark'ıyla birebir — sıra/isimler
// https://ai.google.dev/edge/mediapipe/solutions/vision/pose_landmarker ile eşleşiyor.
public enum PoseLandmarkType
{
    Nose,
    LeftEyeInner,
    LeftEye,
    LeftEyeOuter,
    RightEyeInner,
    RightEye,
    RightEyeOuter,
    LeftEar,
    RightEar,
    MouthLeft,
    MouthRight,
    LeftShoulder,
    RightShoulder,
    LeftElbow,
    RightElbow,
    LeftWrist,
    RightWrist,
    LeftPinky,
    RightPinky,
    LeftIndex,
    RightIndex,
    LeftThumb,
    RightThumb,
    LeftHip,
    RightHip,
    LeftKnee,
    RightKnee,
    LeftAnkle,
    RightAnkle,
    LeftHeel,
    RightHeel,
    LeftFootIndex,
    RightFootIndex
}
