namespace Application.BuildingBlocks.Ports.Notifications.Templates;

/// <summary>
/// Defines the allowed placeholders/parameters that can be injected into notification templates.
/// </summary>
public enum NotificationParameterType
{
    // Region: Application
    /// <summary>The name of the application (e.g., "ReSys Shop").</summary>
    ApplicationName,
    /// <summary>Base URL of the application (e.g., "https://resys.shop").</summary>
    ApplicationUrl,
    /// <summary>The general support email address (e.g., "support@resys.shop").</summary>
    SupportEmail,
    /// <summary>The general support phone number (e.g., "+1-800-555-0199").</summary>
    SupportPhone,

    // Region: Customer
    /// <summary>The given (first) name of the user.</summary>
    UserFirstName,

    // Region: Authentication
    /// <summary>The verification code sent to the user (e.g., "ABC123").</summary>
    VerificationCode,
    /// <summary>URL to complete email or account verification.</summary>
    VerificationUrl,
    /// <summary>URL to reset a forgotten password.</summary>
    ResetPasswordUrl,
    /// <summary>URL to set up an initial password for a new account.</summary>
    PasswordSetupUrl,
    /// <summary>URL to confirm an email change or account action.</summary>
    ConfirmationUrl,
    /// <summary>The time window before a verification token expires (e.g., "30 minutes").</summary>
    ExpirationTime,

    // Region: Order
    /// <summary>The unique order reference number (e.g., "ORD-98765").</summary>
    OrderNumber,
    /// <summary>A formatted list of items in the order.</summary>
    OrderItems,
    /// <summary>The total monetary value of the order (e.g., "$129.99").</summary>
    OrderTotal,
    /// <summary>The currency code used for the order (e.g., "USD", "EUR").</summary>
    Currency,

    // Region: Shipping
    /// <summary>The shipping carrier name (e.g., "FedEx", "UPS").</summary>
    Carrier,
    /// <summary>The tracking number assigned by the carrier.</summary>
    TrackingNumber,
    /// <summary>Direct link to the carrier tracking page.</summary>
    TrackingUrl,
    /// <summary>The estimated or actual delivery date.</summary>
    EstimatedDeliveryDate,

    // Region: Payment
    /// <summary>The payment method used (e.g., "Visa", "PayPal").</summary>
    PaymentMethod,
    /// <summary>The amount charged in the transaction.</summary>
    PaymentAmount,
    /// <summary>The amount refunded to the customer.</summary>
    RefundAmount,
    /// <summary>URL to view the invoice for the transaction.</summary>
    InvoiceUrl,
    /// <summary>URL to download or view the payment receipt.</summary>
    ReceiptUrl,

    // Region: Product
    /// <summary>The name of the product (e.g., "Classic Leather Jacket").</summary>
    ProductName,
    /// <summary>The price of the product (e.g., "$89.99").</summary>
    ProductPrice,
    /// <summary>URL to the product's main image.</summary>
    ProductImageUrl,
    /// <summary>URL to the product detail page.</summary>
    ProductUrl,

    // Region: Collection
    /// <summary>The name of a collection or category (e.g., "Summer 2026").</summary>
    CollectionName,
    /// <summary>URL to the collection's landing page.</summary>
    CollectionUrl,

    // Region: Cart
    /// <summary>The number of items currently in the cart.</summary>
    CartItemCount,
    /// <summary>The total value of the cart (e.g., "$199.99").</summary>
    CartTotal,
    /// <summary>URL to the shopping cart page.</summary>
    CartUrl,

    // Region: Security
    /// <summary>The IP address from which the action was performed.</summary>
    IpAddress,
    /// <summary>The name of the device used (e.g., "iPhone 16", "Windows PC").</summary>
    DeviceName,
    /// <summary>The browser name used (e.g., "Chrome", "Safari").</summary>
    Browser,
    /// <summary>The geographic location derived from the IP address.</summary>
    Location,

    // Region: Links
    /// <summary>URL to write a product review.</summary>
    ReviewUrl,
    /// <summary>URL to unsubscribe from future communications.</summary>
    UnsubscribeUrl
}
