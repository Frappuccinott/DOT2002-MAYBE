public enum FluidType
{
    Gasoline,
    MotorOil
}

public static class FluidTypeExtensions
{
    public static string GetDisplayName(this FluidType type)
    {
        return type switch
        {
            FluidType.Gasoline => "Gasoline",
            FluidType.MotorOil => "Motor Oil",
            _ => type.ToString()
        };
    }
}
