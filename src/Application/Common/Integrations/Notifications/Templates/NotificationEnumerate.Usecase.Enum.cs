using Application.Common.Integrations.Notifications.Store;

namespace Application.Common.Integrations.Notifications.Templates;

/// <summary>
/// Enumerates all supported notification scenarios in the ReSys.Shop ecosystem.
/// Each UseCase maps to a specific template in the <see cref="NotificationStore"/>.
/// </summary>
public enum NotificationUseCase
{
    /// <summary>Invalid use case placeholder.</summary>
    None = 0,

    // Region: Identity
    /// <summary>Sent when a new user account is created.
    /// <para>Uses: <see cref="NotificationParameterType.UserFirstName"/>, <see cref="NotificationParameterType.ApplicationName"/>.</para></summary>
    UserRegistered,
    /// <summary>Sent when a user requests email verification.
    /// <para>Uses: <see cref="NotificationParameterType.UserFirstName"/>, <see cref="NotificationParameterType.VerificationUrl"/>, <see cref="NotificationParameterType.ExpirationTime"/>.</para></summary>
    EmailVerificationRequested,
    /// <summary>Sent after email address is successfully verified.
    /// <para>Uses: <see cref="NotificationParameterType.UserFirstName"/>.</para></summary>
    EmailVerified,
    /// <summary>Sent when a user's email address is changed.
    /// <para>Uses: <see cref="NotificationParameterType.UserFirstName"/>.</para></summary>
    EmailChanged,
    /// <summary>Sent when a user needs to set up their password for the first time.
    /// <para>Uses: <see cref="NotificationParameterType.UserFirstName"/>, <see cref="NotificationParameterType.PasswordSetupUrl"/>, <see cref="NotificationParameterType.ExpirationTime"/>.</para></summary>
    PasswordSetupRequested,
    /// <summary>Sent when a password reset is requested.
    /// <para>Uses: <see cref="NotificationParameterType.UserFirstName"/>, <see cref="NotificationParameterType.ResetPasswordUrl"/>, <see cref="NotificationParameterType.ExpirationTime"/>.</para></summary>
    PasswordResetRequested,
    /// <summary>Sent as confirmation after a password is successfully changed.
    /// <para>Uses: <see cref="NotificationParameterType.UserFirstName"/>.</para></summary>
    PasswordChanged,
    /// <summary>Sent when a two-factor authentication code is requested.
    /// <para>Uses: <see cref="NotificationParameterType.VerificationCode"/>, <see cref="NotificationParameterType.ExpirationTime"/>.</para></summary>
    TwoFactorCodeRequested,
    /// <summary>Sent when a login attempt is blocked due to security concerns.
    /// <para>Uses: <see cref="NotificationParameterType.UserFirstName"/>, <see cref="NotificationParameterType.IpAddress"/>, <see cref="NotificationParameterType.DeviceName"/>, <see cref="NotificationParameterType.Browser"/>, <see cref="NotificationParameterType.Location"/>.</para></summary>
    LoginBlocked,

    // Region: Customer
    /// <summary>Sent to welcome a new customer to the platform.
    /// <para>Uses: <see cref="NotificationParameterType.UserFirstName"/>.</para></summary>
    WelcomeSent,
    /// <summary>Sent when a customer account has been locked.
    /// <para>Uses: <see cref="NotificationParameterType.UserFirstName"/>.</para></summary>
    AccountLocked,
    /// <summary>Sent when a customer account has been unlocked.
    /// <para>Uses: <see cref="NotificationParameterType.UserFirstName"/>.</para></summary>
    AccountUnlocked,

    // Region: Orders
    /// <summary>Sent when an order is confirmed and placed.
    /// <para>Uses: <see cref="NotificationParameterType.UserFirstName"/>, <see cref="NotificationParameterType.OrderNumber"/>, <see cref="NotificationParameterType.OrderItems"/>, <see cref="NotificationParameterType.OrderTotal"/>, <see cref="NotificationParameterType.Currency"/>.</para></summary>
    OrderConfirmed,
    /// <summary>Sent when an order is cancelled.
    /// <para>Uses: <see cref="NotificationParameterType.OrderNumber"/>.</para></summary>
    OrderCancelled,
    /// <summary>Sent when an order has been shipped.
    /// <para>Uses: <see cref="NotificationParameterType.OrderNumber"/>, <see cref="NotificationParameterType.Carrier"/>, <see cref="NotificationParameterType.TrackingNumber"/>, <see cref="NotificationParameterType.TrackingUrl"/>, <see cref="NotificationParameterType.EstimatedDeliveryDate"/>.</para></summary>
    OrderShipped,
    /// <summary>Sent when a shipment is delayed.
    /// <para>Uses: <see cref="NotificationParameterType.OrderNumber"/>, <see cref="NotificationParameterType.EstimatedDeliveryDate"/>.</para></summary>
    ShipmentDelayed,
    /// <summary>Sent when a shipment is ready for pickup.
    /// <para>Uses: <see cref="NotificationParameterType.OrderNumber"/>.</para></summary>
    ShipmentReadyForPickup,
    /// <summary>Sent when an order has been delivered.
    /// <para>Uses: <see cref="NotificationParameterType.OrderNumber"/>.</para></summary>
    OrderDelivered,
    /// <summary>Sent when a return request is approved.
    /// <para>Uses: <see cref="NotificationParameterType.OrderNumber"/>.</para></summary>
    ReturnApproved,
    /// <summary>Sent when a return request is rejected.
    /// <para>Uses: <see cref="NotificationParameterType.OrderNumber"/>.</para></summary>
    ReturnRejected,

    // Region: Payments
    /// <summary>Sent when a payment transaction succeeds.
    /// <para>Uses: <see cref="NotificationParameterType.OrderNumber"/>, <see cref="NotificationParameterType.PaymentAmount"/>, <see cref="NotificationParameterType.PaymentMethod"/>, <see cref="NotificationParameterType.ReceiptUrl"/>.</para></summary>
    PaymentSucceeded,
    /// <summary>Sent when a payment transaction fails.
    /// <para>Uses: <see cref="NotificationParameterType.OrderNumber"/>, <see cref="NotificationParameterType.PaymentAmount"/>.</para></summary>
    PaymentFailed,
    /// <summary>Sent when a refund has been completed.
    /// <para>Uses: <see cref="NotificationParameterType.OrderNumber"/>, <see cref="NotificationParameterType.RefundAmount"/>.</para></summary>
    RefundCompleted,
    /// <summary>Sent when an invoice is issued.
    /// <para>Uses: <see cref="NotificationParameterType.InvoiceUrl"/>.</para></summary>
    InvoiceIssued,
    /// <summary>Sent when a receipt is issued.
    /// <para>Uses: <see cref="NotificationParameterType.ReceiptUrl"/>.</para></summary>
    ReceiptIssued,

    // Region: Products
    /// <summary>Sent when a previously out-of-stock product is back in stock.
    /// <para>Uses: <see cref="NotificationParameterType.ProductName"/>, <see cref="NotificationParameterType.ProductUrl"/>.</para></summary>
    ProductBackInStock,
    /// <summary>Sent when the price of a product drops.
    /// <para>Uses: <see cref="NotificationParameterType.ProductName"/>, <see cref="NotificationParameterType.ProductPrice"/>, <see cref="NotificationParameterType.ProductUrl"/>.</para></summary>
    PriceDropped,
    /// <summary>Sent to request a product review after purchase.
    /// <para>Uses: <see cref="NotificationParameterType.ProductName"/>, <see cref="NotificationParameterType.ReviewUrl"/>.</para></summary>
    ProductReviewRequested,
    /// <summary>Sent when a product is recommended to a user.
    /// <para>Uses: <see cref="NotificationParameterType.ProductName"/>, <see cref="NotificationParameterType.ProductImageUrl"/>, <see cref="NotificationParameterType.ProductUrl"/>.</para></summary>
    ProductRecommended,

    // Region: Cart
    /// <summary>Sent as a reminder when a shopping cart is abandoned.
    /// <para>Uses: <see cref="NotificationParameterType.UserFirstName"/>, <see cref="NotificationParameterType.CartItemCount"/>, <see cref="NotificationParameterType.CartTotal"/>, <see cref="NotificationParameterType.CartUrl"/>.</para></summary>
    CartAbandoned,

    /// <summary>Sent when a new collection is released.
    /// <para>Uses: <see cref="NotificationParameterType.CollectionName"/>, <see cref="NotificationParameterType.CollectionUrl"/>.</para></summary>
    NewCollectionReleased,

    // Region: System
    /// <summary>Sent for security-related alerts and warnings.
    /// <para>Uses: <see cref="NotificationParameterType.UserFirstName"/>, <see cref="NotificationParameterType.IpAddress"/>, <see cref="NotificationParameterType.DeviceName"/>, <see cref="NotificationParameterType.Browser"/>, <see cref="NotificationParameterType.Location"/>.</para></summary>
    SecurityAlert,
    /// <summary>Sent when the privacy policy is updated.
    /// <para>Uses: <see cref="NotificationParameterType.ApplicationUrl"/>.</para></summary>
    PrivacyPolicyUpdated,
    /// <summary>Sent when the terms of service are updated.
    /// <para>Uses: <see cref="NotificationParameterType.ApplicationUrl"/>.</para></summary>
    TermsOfServiceUpdated
}
