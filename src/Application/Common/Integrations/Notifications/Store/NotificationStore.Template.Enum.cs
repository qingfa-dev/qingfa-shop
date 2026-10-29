using Application.Common.Integrations.Notifications.Templates;

namespace Application.Common.Integrations.Notifications.Store;

/// <summary>Defines the complete notification template registry with use-case definitions, channel mappings, priority levels, and parameter constraints. This is the single source of truth for all notification templates.</summary>
public static partial class NotificationStore
{
    public static readonly Dictionary<NotificationUseCase, NotificationTemplate> Templates = new()
    {
        #region Identity
        [NotificationUseCase.UserRegistered] = new NotificationTemplate
        {
            Value = NotificationUseCase.UserRegistered,
            Name = "Account Registration",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.Critical,
            TemplateContent = "Hi {UserFirstName},\n\nWelcome to {ApplicationName}! Your account has been created.\n\nStart shopping at {ApplicationUrl}.\n\nQuestions? Contact {SupportEmail}.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Hi {UserFirstName},</h2><p>Welcome to {ApplicationName}! Your account has been created.</p><p><a href='{ApplicationUrl}' style='" + NotificationTemplateStyles.ButtonStyle + "'>Start Shopping</a></p><p>Questions? Contact <a href='mailto:{SupportEmail}'>{SupportEmail}</a>.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.UserFirstName,
                NotificationParameterType.ApplicationName
            ],
            Description = "Sent after account creation to welcome the user."
        },

        [NotificationUseCase.EmailVerificationRequested] = new NotificationTemplate
        {
            Value = NotificationUseCase.EmailVerificationRequested,
            Name = "Email Verification",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.High,
            TemplateContent = "Hi {UserFirstName},\n\nPlease verify your email address by clicking the link below:\n\n{VerificationUrl}\n\nThis link expires in {ExpirationTime}. If you didn't create an account, please ignore this email.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Hi {UserFirstName},</h2><p>Please verify your email address by clicking the button below:</p><p><a href='{VerificationUrl}' style='" + NotificationTemplateStyles.ButtonStyle + "'>Verify Email</a></p><p>This link expires in <b>{ExpirationTime}</b>.</p><p>If you didn't create an account, please ignore this email.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.UserFirstName,
                NotificationParameterType.VerificationUrl,
                NotificationParameterType.ExpirationTime
            ],
            Description = "Sent to verify a user's email address after registration or email change."
        },

        [NotificationUseCase.EmailVerified] = new NotificationTemplate
        {
            Value = NotificationUseCase.EmailVerified,
            Name = "Email Verified",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.High,
            TemplateContent = "Hi {UserFirstName},\n\nYour email address has been successfully verified. You now have full access to your {ApplicationName} account.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Hi {UserFirstName},</h2><p>Your email address has been successfully verified.</p><p>You now have full access to your {ApplicationName} account.</p><p><a href='{ApplicationUrl}' style='" + NotificationTemplateStyles.ButtonStyle + "'>Go to Account</a></p></body></html>",
            ParamValues =
            [
                NotificationParameterType.UserFirstName
            ],
            Description = "Sent after email verification is completed successfully."
        },

        [NotificationUseCase.EmailChanged] = new NotificationTemplate
        {
            Value = NotificationUseCase.EmailChanged,
            Name = "Email Changed",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.High,
            TemplateContent = "Hi {UserFirstName},\n\nYour email address has been changed. If you did not make this change, please contact {SupportEmail} immediately.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Hi {UserFirstName},</h2><p>Your email address has been changed.</p><p>If you did not make this change, please contact <a href='mailto:{SupportEmail}'>{SupportEmail}</a> immediately.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.UserFirstName
            ],
            Description = "Sent as confirmation when a user changes their email address."
        },

        [NotificationUseCase.PasswordSetupRequested] = new NotificationTemplate
        {
            Value = NotificationUseCase.PasswordSetupRequested,
            Name = "Password Setup",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.Critical,
            TemplateContent = "Hi {UserFirstName},\n\nAn account has been created for you at {ApplicationName}. Please set up your password by clicking the link below:\n\n{PasswordSetupUrl}\n\nThis link expires in {ExpirationTime}. If you didn't expect this, please contact {SupportEmail}.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Hi {UserFirstName},</h2><p>An account has been created for you at {ApplicationName}. Please set up your password by clicking the button below:</p><p><a href='{PasswordSetupUrl}' style='" + NotificationTemplateStyles.ButtonStyle + "'>Set Up Password</a></p><p>This link expires in <b>{ExpirationTime}</b>.</p><p>If you didn't expect this, please contact <a href='mailto:{SupportEmail}'>{SupportEmail}</a>.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.UserFirstName,
                NotificationParameterType.PasswordSetupUrl,
                NotificationParameterType.ExpirationTime
            ],
            Description = "Sent to new users when an account is created for them, providing a link to securely set their initial password."
        },

        [NotificationUseCase.PasswordResetRequested] = new NotificationTemplate
        {
            Value = NotificationUseCase.PasswordResetRequested,
            Name = "Password Reset",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.Critical,
            TemplateContent = "Hi {UserFirstName},\n\nWe received a request to reset your password. Click the link below to set a new one:\n\n{ResetPasswordUrl}\n\nThis link expires in {ExpirationTime}. If you didn't request this, please contact {SupportEmail}.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Hi {UserFirstName},</h2><p>We received a request to reset your password. Click the button below to set a new one:</p><p><a href='{ResetPasswordUrl}' style='" + NotificationTemplateStyles.ButtonStyle + "'>Reset Password</a></p><p>This link expires in <b>{ExpirationTime}</b>.</p><p>If you didn't request this, please contact <a href='mailto:{SupportEmail}'>{SupportEmail}</a>.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.UserFirstName,
                NotificationParameterType.ResetPasswordUrl,
                NotificationParameterType.ExpirationTime
            ],
            Description = "Sent when a user requests a password reset, providing a link to securely update their password."
        },

        [NotificationUseCase.PasswordChanged] = new NotificationTemplate
        {
            Value = NotificationUseCase.PasswordChanged,
            Name = "Password Changed",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.Critical,
            TemplateContent = "Hi {UserFirstName},\n\nYour password has been successfully changed. If you did not make this change, please contact {SupportEmail} immediately.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Hi {UserFirstName},</h2><p>Your password has been successfully changed.</p><p>If you did not make this change, please contact <a href='mailto:{SupportEmail}'>{SupportEmail}</a> immediately.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.UserFirstName
            ],
            Description = "Sent as confirmation after a password is successfully changed."
        },

        [NotificationUseCase.TwoFactorCodeRequested] = new NotificationTemplate
        {
            Value = NotificationUseCase.TwoFactorCodeRequested,
            Name = "Two-Factor Authentication Code",
            TemplateFormatType = NotificationFormat.Default,
            SendMethodType = NotificationChannel.SMS,
            Priority = NotificationPriorityLevel.Critical,
            TemplateContent = "Your verification code is {VerificationCode}. This code expires in {ExpirationTime}. Don't share this code with anyone.",
            HtmlTemplateContent = "Your verification code is <b>{VerificationCode}</b>. This code expires in {ExpirationTime}. Don't share this code with anyone.",
            ParamValues =
            [
                NotificationParameterType.VerificationCode,
                NotificationParameterType.ExpirationTime
            ],
            Description = "Sent via SMS to provide a verification code for two-factor authentication."
        },

        [NotificationUseCase.LoginBlocked] = new NotificationTemplate
        {
            Value = NotificationUseCase.LoginBlocked,
            Name = "Login Blocked",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.Critical,
            TemplateContent = "Hi {UserFirstName},\n\nWe detected a suspicious login attempt on your account and have blocked it.\n\nDetails:\nIP Address: {IpAddress}\nDevice: {DeviceName}\nBrowser: {Browser}\nLocation: {Location}\n\nIf this was you, please contact {SupportEmail} for assistance.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Hi {UserFirstName},</h2><p>We detected a suspicious login attempt on your account and have blocked it.</p><table><tr><td><b>IP Address:</b></td><td>{IpAddress}</td></tr><tr><td><b>Device:</b></td><td>{DeviceName}</td></tr><tr><td><b>Browser:</b></td><td>{Browser}</td></tr><tr><td><b>Location:</b></td><td>{Location}</td></tr></table><p>If this was you, please contact <a href='mailto:{SupportEmail}'>{SupportEmail}</a> for assistance.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.UserFirstName,
                NotificationParameterType.IpAddress,
                NotificationParameterType.DeviceName,
                NotificationParameterType.Browser,
                NotificationParameterType.Location
            ],
            Description = "Sent when a login attempt is blocked due to suspicious activity."
        },
        #endregion

        #region Customer
        [NotificationUseCase.WelcomeSent] = new NotificationTemplate
        {
            Value = NotificationUseCase.WelcomeSent,
            Name = "Welcome Message",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.Normal,
            TemplateContent = "Hi {UserFirstName},\n\nWelcome to {ApplicationName}! We're excited to have you on board. Start exploring and discover great products tailored just for you.\n\nVisit {ApplicationUrl} to get started.\n\nQuestions? Contact {SupportEmail}.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Hi {UserFirstName},</h2><p>Welcome to {ApplicationName}! We're excited to have you on board.</p><p>Start exploring and discover great products tailored just for you.</p><p><a href='{ApplicationUrl}' style='" + NotificationTemplateStyles.ButtonStyle + "'>Get Started</a></p><p>Questions? Contact <a href='mailto:{SupportEmail}'>{SupportEmail}</a>.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.UserFirstName
            ],
            Description = "Sent to welcome a new customer after registration is complete."
        },

        [NotificationUseCase.AccountLocked] = new NotificationTemplate
        {
            Value = NotificationUseCase.AccountLocked,
            Name = "Account Locked",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.Normal,
            TemplateContent = "Hi {UserFirstName},\n\nYour account has been locked. This may be due to multiple failed login attempts or a policy violation.\n\nPlease contact {SupportEmail} to regain access to your account.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Hi {UserFirstName},</h2><p>Your account has been locked. This may be due to multiple failed login attempts or a policy violation.</p><p>Please contact <a href='mailto:{SupportEmail}'>{SupportEmail}</a> to regain access to your account.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.UserFirstName
            ],
            Description = "Sent when a customer account has been locked."
        },

        [NotificationUseCase.AccountUnlocked] = new NotificationTemplate
        {
            Value = NotificationUseCase.AccountUnlocked,
            Name = "Account Unlocked",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.Normal,
            TemplateContent = "Hi {UserFirstName},\n\nYour account has been unlocked. You can now log in and use {ApplicationName} as usual.\n\nVisit {ApplicationUrl} to sign in.\n\nIf you have any questions, contact {SupportEmail}.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Hi {UserFirstName},</h2><p>Your account has been unlocked. You can now log in and use {ApplicationName} as usual.</p><p><a href='{ApplicationUrl}' style='" + NotificationTemplateStyles.ButtonStyle + "'>Sign In</a></p><p>If you have any questions, contact <a href='mailto:{SupportEmail}'>{SupportEmail}</a>.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.UserFirstName
            ],
            Description = "Sent when a customer account has been reinstated."
        },
        #endregion

        #region Orders
        [NotificationUseCase.OrderConfirmed] = new NotificationTemplate
        {
            Value = NotificationUseCase.OrderConfirmed,
            Name = "Order Confirmed",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.High,
            TemplateContent = "Hi {UserFirstName},\n\nYour order {OrderNumber} has been confirmed.\n\nItems: {OrderItems}\nTotal: {OrderTotal} {Currency}\n\nWe'll notify you when it ships. View your order details at {ApplicationUrl}.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Hi {UserFirstName},</h2><p>Your order <b>{OrderNumber}</b> has been confirmed.</p><p><b>Items:</b> {OrderItems}<br><b>Total:</b> {OrderTotal} {Currency}</p><p>We'll notify you when it ships.</p><p><a href='{ApplicationUrl}' style='" + NotificationTemplateStyles.ButtonStyle + "'>View Order</a></p></body></html>",
            ParamValues =
            [
                NotificationParameterType.UserFirstName,
                NotificationParameterType.OrderNumber,
                NotificationParameterType.OrderItems,
                NotificationParameterType.OrderTotal,
                NotificationParameterType.Currency
            ],
            Description = "Sent when an order is confirmed and placed successfully."
        },

        [NotificationUseCase.OrderCancelled] = new NotificationTemplate
        {
            Value = NotificationUseCase.OrderCancelled,
            Name = "Order Cancelled",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.High,
            TemplateContent = "Your order {OrderNumber} has been cancelled.\n\nIf you have any questions about this cancellation, please contact {SupportEmail}.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Order Cancelled</h2><p>Your order <b>{OrderNumber}</b> has been cancelled.</p><p>If you have any questions about this cancellation, please contact <a href='mailto:{SupportEmail}'>{SupportEmail}</a>.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.OrderNumber
            ],
            Description = "Sent when an order is cancelled."
        },

        [NotificationUseCase.OrderShipped] = new NotificationTemplate
        {
            Value = NotificationUseCase.OrderShipped,
            Name = "Order Shipped",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.High,
            TemplateContent = "Your order {OrderNumber} has been shipped!\n\nCarrier: {Carrier}\nTracking Number: {TrackingNumber}\nTrack your shipment: {TrackingUrl}\nEstimated Delivery: {EstimatedDeliveryDate}",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Order Shipped</h2><p>Your order <b>{OrderNumber}</b> has been shipped!</p><table><tr><td><b>Carrier:</b></td><td>{Carrier}</td></tr><tr><td><b>Tracking Number:</b></td><td>{TrackingNumber}</td></tr><tr><td><b>Estimated Delivery:</b></td><td>{EstimatedDeliveryDate}</td></tr></table><p><a href='{TrackingUrl}' style='" + NotificationTemplateStyles.ButtonStyle + "'>Track Your Shipment</a></p></body></html>",
            ParamValues =
            [
                NotificationParameterType.OrderNumber,
                NotificationParameterType.Carrier,
                NotificationParameterType.TrackingNumber,
                NotificationParameterType.TrackingUrl,
                NotificationParameterType.EstimatedDeliveryDate
            ],
            Description = "Sent when an order is shipped, providing tracking information."
        },

        [NotificationUseCase.ShipmentDelayed] = new NotificationTemplate
        {
            Value = NotificationUseCase.ShipmentDelayed,
            Name = "Shipment Delayed",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.High,
            TemplateContent = "Your order {OrderNumber} has been delayed.\n\nNew estimated delivery date: {EstimatedDeliveryDate}\n\nWe apologize for the inconvenience. Contact {SupportEmail} for more information.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Shipment Delayed</h2><p>Your order <b>{OrderNumber}</b> has been delayed.</p><p><b>New estimated delivery date:</b> {EstimatedDeliveryDate}</p><p>We apologize for the inconvenience. Contact <a href='mailto:{SupportEmail}'>{SupportEmail}</a> for more information.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.OrderNumber,
                NotificationParameterType.EstimatedDeliveryDate
            ],
            Description = "Sent when a shipment is delayed by the carrier."
        },

        [NotificationUseCase.ShipmentReadyForPickup] = new NotificationTemplate
        {
            Value = NotificationUseCase.ShipmentReadyForPickup,
            Name = "Ready for Pickup",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.High,
            TemplateContent = "Your order {OrderNumber} is ready for pickup!\n\nPlease visit the pickup location to collect your order. Bring your order confirmation and a valid ID.\n\nQuestions? Contact {SupportEmail}.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Ready for Pickup</h2><p>Your order <b>{OrderNumber}</b> is ready for pickup!</p><p>Please visit the pickup location to collect your order. Bring your order confirmation and a valid ID.</p><p>Questions? Contact <a href='mailto:{SupportEmail}'>{SupportEmail}</a>.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.OrderNumber
            ],
            Description = "Sent when an order is ready for customer pickup."
        },

        [NotificationUseCase.OrderDelivered] = new NotificationTemplate
        {
            Value = NotificationUseCase.OrderDelivered,
            Name = "Order Delivered",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.High,
            TemplateContent = "Your order {OrderNumber} has been delivered!\n\nWe hope you love your purchase. If you have any issues, please contact {SupportEmail}.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Order Delivered</h2><p>Your order <b>{OrderNumber}</b> has been delivered!</p><p>We hope you love your purchase. If you have any issues, please contact <a href='mailto:{SupportEmail}'>{SupportEmail}</a>.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.OrderNumber
            ],
            Description = "Sent when an order is confirmed as delivered."
        },

        [NotificationUseCase.ReturnApproved] = new NotificationTemplate
        {
            Value = NotificationUseCase.ReturnApproved,
            Name = "Return Approved",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.High,
            TemplateContent = "Your return for order {OrderNumber} has been approved.\n\nWe'll process your refund within the next few business days. Contact {SupportEmail} if you have any questions.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Return Approved</h2><p>Your return for order <b>{OrderNumber}</b> has been approved.</p><p>We'll process your refund within the next few business days.</p><p>Contact <a href='mailto:{SupportEmail}'>{SupportEmail}</a> if you have any questions.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.OrderNumber
            ],
            Description = "Sent when a return request is approved."
        },

        [NotificationUseCase.ReturnRejected] = new NotificationTemplate
        {
            Value = NotificationUseCase.ReturnRejected,
            Name = "Return Rejected",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.High,
            TemplateContent = "Your return for order {OrderNumber} has been rejected.\n\nIf you believe this is an error, please contact {SupportEmail} for further assistance.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Return Rejected</h2><p>Your return for order <b>{OrderNumber}</b> has been rejected.</p><p>If you believe this is an error, please contact <a href='mailto:{SupportEmail}'>{SupportEmail}</a> for further assistance.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.OrderNumber
            ],
            Description = "Sent when a return request is rejected."
        },
        #endregion

        #region Payments
        [NotificationUseCase.PaymentSucceeded] = new NotificationTemplate
        {
            Value = NotificationUseCase.PaymentSucceeded,
            Name = "Payment Successful",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.High,
            TemplateContent = "Your payment for order {OrderNumber} has been processed successfully.\n\nAmount: {PaymentAmount}\nPayment Method: {PaymentMethod}\n\nView your receipt: {ReceiptUrl}",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Payment Successful</h2><p>Your payment for order <b>{OrderNumber}</b> has been processed successfully.</p><table><tr><td><b>Amount:</b></td><td>{PaymentAmount}</td></tr><tr><td><b>Payment Method:</b></td><td>{PaymentMethod}</td></tr></table><p><a href='{ReceiptUrl}' style='" + NotificationTemplateStyles.ButtonStyle + "'>View Receipt</a></p></body></html>",
            ParamValues =
            [
                NotificationParameterType.OrderNumber,
                NotificationParameterType.PaymentAmount,
                NotificationParameterType.PaymentMethod,
                NotificationParameterType.ReceiptUrl
            ],
            Description = "Sent when a payment transaction completes successfully."
        },

        [NotificationUseCase.PaymentFailed] = new NotificationTemplate
        {
            Value = NotificationUseCase.PaymentFailed,
            Name = "Payment Failed",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.Critical,
            TemplateContent = "Your payment of {PaymentAmount} for order {OrderNumber} has failed.\n\nPlease try again or use a different payment method. Contact {SupportEmail} if you need assistance.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Payment Failed</h2><p>Your payment of <b>{PaymentAmount}</b> for order <b>{OrderNumber}</b> has failed.</p><p>Please try again or use a different payment method.</p><p>Contact <a href='mailto:{SupportEmail}'>{SupportEmail}</a> if you need assistance.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.OrderNumber,
                NotificationParameterType.PaymentAmount
            ],
            Description = "Sent when a payment transaction fails."
        },

        [NotificationUseCase.RefundCompleted] = new NotificationTemplate
        {
            Value = NotificationUseCase.RefundCompleted,
            Name = "Refund Completed",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.High,
            TemplateContent = "Your refund of {RefundAmount} for order {OrderNumber} has been completed.\n\nThe funds should appear in your account within a few business days. Contact {SupportEmail} if you have any questions.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Refund Completed</h2><p>Your refund of <b>{RefundAmount}</b> for order <b>{OrderNumber}</b> has been completed.</p><p>The funds should appear in your account within a few business days.</p><p>Contact <a href='mailto:{SupportEmail}'>{SupportEmail}</a> if you have any questions.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.OrderNumber,
                NotificationParameterType.RefundAmount
            ],
            Description = "Sent when a refund has been processed."
        },

        [NotificationUseCase.InvoiceIssued] = new NotificationTemplate
        {
            Value = NotificationUseCase.InvoiceIssued,
            Name = "Invoice Issued",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.Normal,
            TemplateContent = "Your invoice is now available.\n\nView and download your invoice at: {InvoiceUrl}\n\nThank you for your business. Contact {SupportEmail} if you have any questions.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Invoice Issued</h2><p>Your invoice is now available.</p><p><a href='{InvoiceUrl}' style='" + NotificationTemplateStyles.ButtonStyle + "'>View Invoice</a></p><p>Thank you for your business. Contact <a href='mailto:{SupportEmail}'>{SupportEmail}</a> if you have any questions.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.InvoiceUrl
            ],
            Description = "Sent when an invoice is generated for a transaction."
        },

        [NotificationUseCase.ReceiptIssued] = new NotificationTemplate
        {
            Value = NotificationUseCase.ReceiptIssued,
            Name = "Receipt Issued",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.Normal,
            TemplateContent = "Your receipt is now available.\n\nView and download your receipt at: {ReceiptUrl}\n\nThank you for your purchase. Contact {SupportEmail} if you have any questions.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Receipt Issued</h2><p>Your receipt is now available.</p><p><a href='{ReceiptUrl}' style='" + NotificationTemplateStyles.ButtonStyle + "'>View Receipt</a></p><p>Thank you for your purchase. Contact <a href='mailto:{SupportEmail}'>{SupportEmail}</a> if you have any questions.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.ReceiptUrl
            ],
            Description = "Sent when a receipt is generated for a purchase."
        },
        #endregion

        #region Products
        [NotificationUseCase.ProductBackInStock] = new NotificationTemplate
        {
            Value = NotificationUseCase.ProductBackInStock,
            Name = "Back in Stock",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.Normal,
            TemplateContent = "Good news! {ProductName} is back in stock.\n\nShop it now at {ProductUrl} before it sells out again.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Back in Stock</h2><p>Good news! <b>{ProductName}</b> is back in stock.</p><p><a href='{ProductUrl}' style='" + NotificationTemplateStyles.ButtonStyle + "'>Shop Now</a></p><p>Hurry, quantities are limited.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.ProductName,
                NotificationParameterType.ProductUrl
            ],
            Description = "Sent when a previously out-of-stock product is restocked."
        },

        [NotificationUseCase.PriceDropped] = new NotificationTemplate
        {
            Value = NotificationUseCase.PriceDropped,
            Name = "Price Dropped",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.Normal,
            TemplateContent = "The price of {ProductName} has dropped to {ProductPrice}!\n\nShop it now at {ProductUrl} and grab the deal while it lasts.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Price Dropped</h2><p>The price of <b>{ProductName}</b> has dropped to <b>{ProductPrice}</b>!</p><p><a href='{ProductUrl}' style='" + NotificationTemplateStyles.ButtonStyle + "'>Shop Now</a></p><p>Grab the deal while it lasts.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.ProductName,
                NotificationParameterType.ProductPrice,
                NotificationParameterType.ProductUrl
            ],
            Description = "Sent when the price of a watched product decreases."
        },

        [NotificationUseCase.ProductReviewRequested] = new NotificationTemplate
        {
            Value = NotificationUseCase.ProductReviewRequested,
            Name = "Review Requested",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.Normal,
            TemplateContent = "How was your experience with {ProductName}?\n\nShare your thoughts by leaving a review at {ReviewUrl}. Your feedback helps other customers!",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Review Requested</h2><p>How was your experience with <b>{ProductName}</b>?</p><p>Share your thoughts by leaving a review:</p><p><a href='{ReviewUrl}' style='" + NotificationTemplateStyles.ButtonStyle + "'>Write a Review</a></p><p>Your feedback helps other customers!</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.ProductName,
                NotificationParameterType.ReviewUrl
            ],
            Description = "Sent after purchase to request a product review."
        },

        [NotificationUseCase.ProductRecommended] = new NotificationTemplate
        {
            Value = NotificationUseCase.ProductRecommended,
            Name = "Product Recommendation",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.Normal,
            TemplateContent = "We thought you might like {ProductName} based on your interests.\n\nCheck it out at {ProductUrl}.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Product Recommendation</h2><p>We thought you might like <b>{ProductName}</b> based on your interests.</p><p><a href='{ProductUrl}' style='" + NotificationTemplateStyles.ButtonStyle + "'>View Product</a></p></body></html>",
            ParamValues =
            [
                NotificationParameterType.ProductName,
                NotificationParameterType.ProductImageUrl,
                NotificationParameterType.ProductUrl
            ],
            Description = "Sent as a personalized product recommendation."
        },
        #endregion

        #region Cart
        [NotificationUseCase.CartAbandoned] = new NotificationTemplate
        {
            Value = NotificationUseCase.CartAbandoned,
            Name = "Cart Abandoned Reminder",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.Normal,
            TemplateContent = "Hi {UserFirstName},\n\nYou have {CartItemCount} items totaling {CartTotal} in your cart.\n\nComplete your purchase at {CartUrl} before they sell out!",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Hi {UserFirstName},</h2><p>You have <b>{CartItemCount}</b> items totaling <b>{CartTotal}</b> in your cart.</p><p><a href='{CartUrl}' style='" + NotificationTemplateStyles.ButtonStyle + "'>Complete Your Purchase</a></p><p>Don't miss out on the items in your cart!</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.UserFirstName,
                NotificationParameterType.CartItemCount,
                NotificationParameterType.CartTotal,
                NotificationParameterType.CartUrl
            ],
            Description = "Sent as a reminder when items are left in a cart without checkout."
        },
        #endregion

        [NotificationUseCase.NewCollectionReleased] = new NotificationTemplate
        {
            Value = NotificationUseCase.NewCollectionReleased,
            Name = "New Collection Released",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.Normal,
            TemplateContent = "Explore our new collection: {CollectionName}!\n\nDiscover the latest products and refresh your style at {CollectionUrl}.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>New Collection Released</h2><p>Explore our new collection: <b>{CollectionName}</b>!</p><p>Discover the latest products and refresh your style.</p><p><a href='{CollectionUrl}' style='" + NotificationTemplateStyles.ButtonStyle + "'>Explore Collection</a></p></body></html>",
            ParamValues =
            [
                NotificationParameterType.CollectionName,
                NotificationParameterType.CollectionUrl
            ],
            Description = "Sent when a new product collection is released."
        },

        #region System
        [NotificationUseCase.SecurityAlert] = new NotificationTemplate
        {
            Value = NotificationUseCase.SecurityAlert,
            Name = "Security Alert",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.Critical,
            TemplateContent = "Hi {UserFirstName},\n\nA security-related event was detected on your account.\n\nDetails:\nIP Address: {IpAddress}\nDevice: {DeviceName}\nBrowser: {Browser}\nLocation: {Location}\n\nIf this was not you, please contact {SupportEmail} immediately.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Hi {UserFirstName},</h2><p>A security-related event was detected on your account.</p><table><tr><td><b>IP Address:</b></td><td>{IpAddress}</td></tr><tr><td><b>Device:</b></td><td>{DeviceName}</td></tr><tr><td><b>Browser:</b></td><td>{Browser}</td></tr><tr><td><b>Location:</b></td><td>{Location}</td></tr></table><p>If this was not you, please contact <a href='mailto:{SupportEmail}'>{SupportEmail}</a> immediately.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.UserFirstName,
                NotificationParameterType.IpAddress,
                NotificationParameterType.DeviceName,
                NotificationParameterType.Browser,
                NotificationParameterType.Location
            ],
            Description = "Sent for security-related account notifications."
        },

        [NotificationUseCase.PrivacyPolicyUpdated] = new NotificationTemplate
        {
            Value = NotificationUseCase.PrivacyPolicyUpdated,
            Name = "Privacy Policy Updated",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.Normal,
            TemplateContent = "Our privacy policy has been updated. Please review the changes at {ApplicationUrl}.\n\nIf you have any questions, contact {SupportEmail}.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Privacy Policy Updated</h2><p>Our privacy policy has been updated. Please review the changes:</p><p><a href='{ApplicationUrl}' style='" + NotificationTemplateStyles.ButtonStyle + "'>View Privacy Policy</a></p><p>If you have any questions, contact <a href='mailto:{SupportEmail}'>{SupportEmail}</a>.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.ApplicationUrl
            ],
            Description = "Sent when the privacy policy is updated."
        },

        [NotificationUseCase.TermsOfServiceUpdated] = new NotificationTemplate
        {
            Value = NotificationUseCase.TermsOfServiceUpdated,
            Name = "Terms of Service Updated",
            TemplateFormatType = NotificationFormat.Html,
            Priority = NotificationPriorityLevel.Normal,
            TemplateContent = "Our terms of service have been updated. Please review the changes at {ApplicationUrl}.\n\nIf you have any questions, contact {SupportEmail}.",
            HtmlTemplateContent = "<html><body style='" + NotificationTemplateStyles.BaseBodyStyle + "'><h2 style='" + NotificationTemplateStyles.HeaderStyle + "'>Terms of Service Updated</h2><p>Our terms of service have been updated. Please review the changes:</p><p><a href='{ApplicationUrl}' style='" + NotificationTemplateStyles.ButtonStyle + "'>View Terms of Service</a></p><p>If you have any questions, contact <a href='mailto:{SupportEmail}'>{SupportEmail}</a>.</p></body></html>",
            ParamValues =
            [
                NotificationParameterType.ApplicationUrl
            ],
            Description = "Sent when the terms of service are updated."
        }
        #endregion
    };
}
