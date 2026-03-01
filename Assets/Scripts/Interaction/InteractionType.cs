/// <summary>
/// Etkileşim türlerini tanımlar.
/// Pickup: F tuşuyla tetiklenir (parça alma/sökme).
/// Interact: E tuşuyla tetiklenir (parça yerleştirme, kapı açma vb.).
/// </summary>
public enum InteractionType
{
    /// <summary>
    /// F tuşuyla tetiklenen etkileşimler (parça alma, sökme).
    /// </summary>
    Pickup,

    /// <summary>
    /// E tuşuyla tetiklenen etkileşimler (parça yerleştirme, kapı açma).
    /// </summary>
    Interact
}
