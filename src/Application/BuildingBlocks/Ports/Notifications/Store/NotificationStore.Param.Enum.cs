using Application.BuildingBlocks.Ports.Notifications.Templates;

namespace Application.BuildingBlocks.Ports.Notifications.Store
{
    /// <summary>Defines the parameter registry for notification template placeholders. Maps each use case to its required and optional parameter keys.</summary>
    public static partial class NotificationStore
    {
        public static readonly Dictionary<NotificationParameterType, NotificationDefinition<NotificationParameterType>> Parameters = new()
        {
            #region Application-related parameters
            [NotificationParameterType.ApplicationName] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.ApplicationName,
                Name = nameof(NotificationParameterType.ApplicationName),
                Presentation = "Application Name",
                Description = "The name of the application, used for branding in outgoing notifications.",
                Example = "ReSys Shop"
            },
            [NotificationParameterType.ApplicationUrl] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.ApplicationUrl,
                Name = nameof(NotificationParameterType.ApplicationUrl),
                Presentation = "Application URL",
                Description = "The base URL of the application, used for navigation links in notifications.",
                Example = "https://resys.shop"
            },
            [NotificationParameterType.SupportEmail] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.SupportEmail,
                Name = nameof(NotificationParameterType.SupportEmail),
                Presentation = "Support Email",
                Description = "The email address for customer support inquiries.",
                Example = "support@resys.shop"
            },
            [NotificationParameterType.SupportPhone] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.SupportPhone,
                Name = nameof(NotificationParameterType.SupportPhone),
                Presentation = "Support Phone",
                Description = "The phone number for customer support contact.",
                Example = "+1-800-555-0199"
            },
            #endregion

            #region Customer-related parameters
            [NotificationParameterType.UserFirstName] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.UserFirstName,
                Name = nameof(NotificationParameterType.UserFirstName),
                Presentation = "User First Name",
                Description = "The given name of the user, used for a personalized greeting.",
                Example = "Jane"
            },
            #endregion

            #region Authentication-related parameters
            [NotificationParameterType.VerificationCode] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.VerificationCode,
                Name = nameof(NotificationParameterType.VerificationCode),
                Presentation = "Verification Code",
                Description = "The verification code sent to the user for email or account verification.",
                Example = "ABC123"
            },
            [NotificationParameterType.VerificationUrl] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.VerificationUrl,
                Name = nameof(NotificationParameterType.VerificationUrl),
                Presentation = "Verification URL",
                Description = "URL to complete email or account verification.",
                Example = "https://resys.shop/verify?token=abc123"
            },
            [NotificationParameterType.ResetPasswordUrl] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.ResetPasswordUrl,
                Name = nameof(NotificationParameterType.ResetPasswordUrl),
                Presentation = "Reset Password URL",
                Description = "URL for resetting a forgotten password.",
                Example = "https://resys.shop/reset-password?token=abc123"
            },
            [NotificationParameterType.PasswordSetupUrl] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.PasswordSetupUrl,
                Name = nameof(NotificationParameterType.PasswordSetupUrl),
                Presentation = "Password Setup URL",
                Description = "URL for setting up an initial password for a new account.",
                Example = "https://resys.shop/setup-password?token=abc123"
            },
            [NotificationParameterType.ConfirmationUrl] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.ConfirmationUrl,
                Name = nameof(NotificationParameterType.ConfirmationUrl),
                Presentation = "Confirmation URL",
                Description = "URL to confirm an email change or account action.",
                Example = "https://resys.shop/confirm?token=abc123"
            },
            [NotificationParameterType.ExpirationTime] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.ExpirationTime,
                Name = nameof(NotificationParameterType.ExpirationTime),
                Presentation = "Expiration Time",
                Description = "The time window before a verification or recovery token expires.",
                Example = "30 minutes"
            },
            #endregion

            #region Order-related parameters
            [NotificationParameterType.OrderNumber] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.OrderNumber,
                Name = nameof(NotificationParameterType.OrderNumber),
                Presentation = "Order Number",
                Description = "The unique reference number identifying the order.",
                Example = "ORD-98765"
            },
            [NotificationParameterType.OrderItems] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.OrderItems,
                Name = nameof(NotificationParameterType.OrderItems),
                Presentation = "Order Items",
                Description = "A formatted list of items in the order.",
                Example = "Classic Leather Jacket, Cashmere Scarf"
            },
            [NotificationParameterType.OrderTotal] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.OrderTotal,
                Name = nameof(NotificationParameterType.OrderTotal),
                Presentation = "Order Total",
                Description = "The total monetary value of the order.",
                Example = "$129.99"
            },
            [NotificationParameterType.Currency] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.Currency,
                Name = nameof(NotificationParameterType.Currency),
                Presentation = "Currency",
                Description = "The currency code used for the order transaction.",
                Example = "USD"
            },
            #endregion

            #region Shipping-related parameters
            [NotificationParameterType.Carrier] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.Carrier,
                Name = nameof(NotificationParameterType.Carrier),
                Presentation = "Carrier",
                Description = "The shipping carrier name handling the delivery.",
                Example = "FedEx"
            },
            [NotificationParameterType.TrackingNumber] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.TrackingNumber,
                Name = nameof(NotificationParameterType.TrackingNumber),
                Presentation = "Tracking Number",
                Description = "The tracking number assigned by the carrier for shipment tracking.",
                Example = "1Z9999W999999999"
            },
            [NotificationParameterType.TrackingUrl] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.TrackingUrl,
                Name = nameof(NotificationParameterType.TrackingUrl),
                Presentation = "Tracking URL",
                Description = "Direct link to the carrier tracking page for the shipment.",
                Example = "https://fedex.com/track/1Z9999W999999999"
            },
            [NotificationParameterType.EstimatedDeliveryDate] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.EstimatedDeliveryDate,
                Name = nameof(NotificationParameterType.EstimatedDeliveryDate),
                Presentation = "Estimated Delivery Date",
                Description = "The estimated or actual delivery date of the shipment.",
                Example = "2026-07-15"
            },
            #endregion

            #region Payment-related parameters
            [NotificationParameterType.PaymentMethod] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.PaymentMethod,
                Name = nameof(NotificationParameterType.PaymentMethod),
                Presentation = "Payment Method",
                Description = "The payment method used for the transaction.",
                Example = "Visa"
            },
            [NotificationParameterType.PaymentAmount] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.PaymentAmount,
                Name = nameof(NotificationParameterType.PaymentAmount),
                Presentation = "Payment Amount",
                Description = "The amount charged in the transaction.",
                Example = "$129.99"
            },
            [NotificationParameterType.RefundAmount] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.RefundAmount,
                Name = nameof(NotificationParameterType.RefundAmount),
                Presentation = "Refund Amount",
                Description = "The amount refunded to the customer.",
                Example = "$129.99"
            },
            [NotificationParameterType.InvoiceUrl] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.InvoiceUrl,
                Name = nameof(NotificationParameterType.InvoiceUrl),
                Presentation = "Invoice URL",
                Description = "URL to view or download the invoice for the transaction.",
                Example = "https://resys.shop/invoice/INV-001"
            },
            [NotificationParameterType.ReceiptUrl] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.ReceiptUrl,
                Name = nameof(NotificationParameterType.ReceiptUrl),
                Presentation = "Receipt URL",
                Description = "URL to view or download the payment receipt.",
                Example = "https://resys.shop/receipt/RCP-001"
            },
            #endregion

            #region Product-related parameters
            [NotificationParameterType.ProductName] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.ProductName,
                Name = nameof(NotificationParameterType.ProductName),
                Presentation = "Product Name",
                Description = "The name of the product being referenced in the notification.",
                Example = "Classic Leather Jacket"
            },
            [NotificationParameterType.ProductPrice] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.ProductPrice,
                Name = nameof(NotificationParameterType.ProductPrice),
                Presentation = "Product Price",
                Description = "The price of the product being referenced.",
                Example = "$89.99"
            },
            [NotificationParameterType.ProductImageUrl] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.ProductImageUrl,
                Name = nameof(NotificationParameterType.ProductImageUrl),
                Presentation = "Product Image URL",
                Description = "URL to the product primary image.",
                Example = "https://resys.shop/images/product-001.jpg"
            },
            [NotificationParameterType.ProductUrl] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.ProductUrl,
                Name = nameof(NotificationParameterType.ProductUrl),
                Presentation = "Product URL",
                Description = "URL to the product detail page.",
                Example = "https://resys.shop/products/classic-leather-jacket"
            },
            #endregion

            #region Collection-related parameters
            [NotificationParameterType.CollectionName] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.CollectionName,
                Name = nameof(NotificationParameterType.CollectionName),
                Presentation = "Collection Name",
                Description = "The name of a product collection or category.",
                Example = "Summer 2026"
            },
            [NotificationParameterType.CollectionUrl] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.CollectionUrl,
                Name = nameof(NotificationParameterType.CollectionUrl),
                Presentation = "Collection URL",
                Description = "URL to the collection landing page.",
                Example = "https://resys.shop/collections/summer-2026"
            },
            #endregion

            #region Cart-related parameters
            [NotificationParameterType.CartItemCount] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.CartItemCount,
                Name = nameof(NotificationParameterType.CartItemCount),
                Presentation = "Cart Item Count",
                Description = "The number of items currently in the shopping cart.",
                Example = "3"
            },
            [NotificationParameterType.CartTotal] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.CartTotal,
                Name = nameof(NotificationParameterType.CartTotal),
                Presentation = "Cart Total",
                Description = "The total value of the shopping cart.",
                Example = "$199.99"
            },
            [NotificationParameterType.CartUrl] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.CartUrl,
                Name = nameof(NotificationParameterType.CartUrl),
                Presentation = "Cart URL",
                Description = "URL to the shopping cart page.",
                Example = "https://resys.shop/cart"
            },
            #endregion

            #region Security-related parameters
            [NotificationParameterType.IpAddress] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.IpAddress,
                Name = nameof(NotificationParameterType.IpAddress),
                Presentation = "IP Address",
                Description = "The IP address from which the action was performed.",
                Example = "192.168.1.1"
            },
            [NotificationParameterType.DeviceName] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.DeviceName,
                Name = nameof(NotificationParameterType.DeviceName),
                Presentation = "Device Name",
                Description = "The name of the device used for the action.",
                Example = "iPhone 16"
            },
            [NotificationParameterType.Browser] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.Browser,
                Name = nameof(NotificationParameterType.Browser),
                Presentation = "Browser",
                Description = "The browser used for the action.",
                Example = "Chrome"
            },
            [NotificationParameterType.Location] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.Location,
                Name = nameof(NotificationParameterType.Location),
                Presentation = "Location",
                Description = "The geographic location derived from the IP address.",
                Example = "New York, NY"
            },
            #endregion

            #region Link-related parameters
            [NotificationParameterType.ReviewUrl] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.ReviewUrl,
                Name = nameof(NotificationParameterType.ReviewUrl),
                Presentation = "Review URL",
                Description = "URL to write a product review.",
                Example = "https://resys.shop/products/classic-leather-jacket/review"
            },
            [NotificationParameterType.UnsubscribeUrl] = new NotificationDefinition<NotificationParameterType>
            {
                Value = NotificationParameterType.UnsubscribeUrl,
                Name = nameof(NotificationParameterType.UnsubscribeUrl),
                Presentation = "Unsubscribe URL",
                Description = "URL to unsubscribe from future communications.",
                Example = "https://resys.shop/unsubscribe"
            },
            #endregion
        };
    }
}
