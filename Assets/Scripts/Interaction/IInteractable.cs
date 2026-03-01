/// <summary>
/// Oyuncunun etkileşime geçebileceği tüm objelerin uygulaması gereken interface.
/// Araba parçaları, kapılar, el freni gibi etkileşimli objeler bu interface'i uygular.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Ekranda gösterilecek etkileşim metni. Örn: "Parçayı Al", "Kapıyı Aç"
    /// </summary>
    string InteractionPrompt { get; }

    /// <summary>
    /// Bu etkileşimin türü. Pickup = F tuşu, Interact = E tuşu.
    /// </summary>
    InteractionType Type { get; }

    /// <summary>
    /// Şu an etkileşim yapılabilir mi?
    /// Örn: Oyuncunun elinde doğru parça varsa slot'a yerleştirilebilir.
    /// </summary>
    bool CanInteract { get; }

    /// <summary>
    /// Etkileşimi gerçekleştirir. Uygun tuşa basılınca çağrılır.
    /// </summary>
    void Interact();
}
