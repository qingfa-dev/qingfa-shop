namespace Application.Modules.Payments.Ports;

/// <summary>Declares that a gateway can refund part of a capture.</summary>
public interface ISupportsPartialRefund
{
    /// <summary>Whether partial refunds are supported.</summary>
    bool CanPartiallyRefund() => true;
}

/// <summary>Declares that a gateway can capture a manual authorization.</summary>
public interface ISupportsManualCapture
{
    /// <summary>Whether manual capture is supported.</summary>
    bool CanCaptureManually() => true;
}
