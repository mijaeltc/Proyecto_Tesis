/// <summary>
/// Contrato que cumplen PilaController, ColaController y ArbolController para que
/// MarcadorDemo pueda lanzar una demostración automática cuando se detecta el QR.
/// </summary>
public interface IDemoEstructura
{
    /// <summary>true si la estructura todavía no tiene elementos.</summary>
    bool EstaVacia { get; }

    /// <summary>Inserta automáticamente los valores de demostración, uno tras otro.</summary>
    void IniciarDemo();

    /// <summary>Cancela los valores de demostración que aún no se han insertado.</summary>
    void DetenerDemo();
}
