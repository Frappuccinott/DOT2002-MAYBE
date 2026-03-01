/// <summary>
/// Arabaya monte edilebilecek tüm parça tiplerini tanımlar.
/// Her parçanın arabada kendine ait bir yerleştirme noktası (slot) vardır.
/// </summary>
public enum CarPartType
{
    // Çamurluklar
    FrontFenderLeft,
    FrontFenderRight,

    // Ön kapılar
    FrontDoorLeft,
    FrontDoorRight,

    // Arka kapılar
    RearDoorLeft,
    RearDoorRight,

    // İç bileşenler
    SteeringWheel,
    Seat,
    Engine,
    Battery,

    // Dış parçalar
    Trunk,
    RearBumper,
    Hood,
    FuelTank,

    // Tekerlekler
    WheelFrontLeft,
    WheelFrontRight,
    WheelRearLeft,
    WheelRearRight
}
