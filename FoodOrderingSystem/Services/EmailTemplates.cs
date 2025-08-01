using System.Text;

namespace FoodOrderingSystem.Services
{
    public static class EmailTemplates
    {
        public static string GetEmailConfirmationTemplate(string username, string confirmationLink)
        {
            return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Welcome to MackDihh!</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            margin: 0;
            padding: 0;
            background-color: #f4f4f4;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
            border-radius: 10px;
            overflow: hidden;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #d32f2f 0%, #b71c1c 100%);
            color: white;
            padding: 30px 20px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
            font-weight: 700;
        }}
        .header p {{
            margin: 10px 0 0 0;
            font-size: 16px;
            opacity: 0.9;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .welcome-message {{
            font-size: 18px;
            color: #2c3e50;
            margin-bottom: 25px;
            text-align: center;
        }}
        .confirmation-section {{
            background-color: #f8f9fa;
            border-radius: 8px;
            padding: 25px;
            margin: 25px 0;
            text-align: center;
        }}
        .confirmation-button {{
            display: inline-block;
            background: linear-gradient(135deg, #d32f2f 0%, #b71c1c 100%);
            color: white;
            text-decoration: none;
            padding: 15px 30px;
            border-radius: 50px;
            font-weight: 600;
            font-size: 16px;
            margin: 20px 0;
            transition: transform 0.3s ease;
        }}
        .confirmation-button:hover {{
            transform: translateY(-2px);
            box-shadow: 0 6px 20px rgba(211, 47, 47, 0.3);
        }}
        .features {{
            margin: 30px 0;
        }}
        .feature-item {{
            display: flex;
            align-items: center;
            margin: 15px 0;
            padding: 10px 0;
        }}
        .feature-icon {{
            width: 40px;
            height: 40px;
            background: linear-gradient(135deg, #ffbc0d 0%, #ff9800 100%);
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            margin-right: 15px;
            color: white;
            font-weight: bold;
        }}
        .feature-text {{
            flex: 1;
            font-size: 14px;
            color: #555;
        }}
        .footer {{
            background-color: #2c3e50;
            color: white;
            padding: 25px 30px;
            text-align: center;
        }}
        .footer p {{
            margin: 5px 0;
            font-size: 14px;
        }}
        .social-links {{
            margin: 15px 0;
        }}
        .social-links a {{
            color: #ffbc0d;
            text-decoration: none;
            margin: 0 10px;
        }}
        .security-note {{
            background-color: #e8f5e8;
            border-left: 4px solid #4caf50;
            padding: 15px;
            margin: 20px 0;
            border-radius: 4px;
        }}
        .security-note p {{
            margin: 0;
            font-size: 14px;
            color: #2e7d32;
        }}
        @media only screen and (max-width: 600px) {{
            .container {{
                margin: 10px;
                border-radius: 5px;
            }}
            .content {{
                padding: 20px 15px;
            }}
            .header {{
                padding: 20px 15px;
            }}
            .header h1 {{
                font-size: 24px;
            }}
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🍔 Welcome to MackDihh!</h1>
            <p>Your journey to delicious food starts here</p>
        </div>
        
        <div class=""content"">
            <div class=""welcome-message"">
                <strong>Hi {username}!</strong><br>
                Thank you for joining the MackDihh family! 🎉
            </div>
            
            <p>We're excited to have you on board! To complete your registration and start enjoying our delicious meals, please confirm your email address by clicking the button below.</p>
            
            <div class=""confirmation-section"">
                <h3 style=""margin-top: 0; color: #d32f2f;"">📧 Confirm Your Email Address</h3>
                <p>This step helps us keep your account secure and ensures you receive important updates about your orders.</p>
                
                <a href=""{confirmationLink}"" class=""confirmation-button"">
                    ✅ Confirm Email Address
                </a>
                
                <p style=""font-size: 12px; color: #666; margin-top: 15px;"">
                    If the button doesn't work, copy and paste this link into your browser:<br>
                    <a href=""{confirmationLink}"" style=""color: #d32f2f; word-break: break-all;"">{confirmationLink}</a>
                </p>
            </div>
            
            <div class=""features"">
                <h3 style=""color: #2c3e50; margin-bottom: 20px;"">🚀 What you can do now:</h3>
                
                <div class=""feature-item"">
                    <div class=""feature-icon"">🍕</div>
                    <div class=""feature-text"">Browse our delicious menu with fresh, high-quality ingredients</div>
                </div>
                
                <div class=""feature-item"">
                    <div class=""feature-icon"">⚡</div>
                    <div class=""feature-text"">Fast delivery to your doorstep with real-time tracking</div>
                </div>
                
                <div class=""feature-item"">
                    <div class=""feature-icon"">🎁</div>
                    <div class=""feature-text"">Exclusive deals and promotions for registered members</div>
                </div>
                
                <div class=""feature-item"">
                    <div class=""feature-icon"">📱</div>
                    <div class=""feature-text"">Easy order management and order history tracking</div>
                </div>
            </div>
            
            <div class=""security-note"">
                <p><strong>🔒 Security Note:</strong> This confirmation link will expire in 24 hours for your security. If you didn't create this account, please ignore this email.</p>
            </div>
            
            <p style=""text-align: center; margin-top: 30px; color: #666;"">
                <strong>Ready to start your culinary journey?</strong><br>
                Once you confirm your email, you'll be able to place your first order!
            </p>
        </div>
        
        <div class=""footer"">
            <p><strong>MackDihh</strong> - Your favorite meals, delivered fast</p>
            <p>📍 No.1, Jalan Alamesra, Alamesra, 88450 Kota Kinabalu, Sabah</p>
            <p>📞 +60 88 123 4567 | 📧 support@mackdihh.com</p>
            
            <div class=""social-links"">
                <a href=""#"">Facebook</a> |
                <a href=""#"">Instagram</a> |
                <a href=""#"">Twitter</a>
            </div>
            
            <p style=""font-size: 12px; opacity: 0.7; margin-top: 20px;"">
                © 2025 MackDihh. All rights reserved.<br>
                This email was sent to you because you registered for a MackDihh account.
            </p>
        </div>
    </div>
</body>
</html>";
        }

        public static string GetOrderConfirmationTemplate(string orderNumber, string customerName, decimal total, string deliveryAddress, DateTime orderDate)
        {
            return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Order Confirmation - MackDihh</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            margin: 0;
            padding: 0;
            background-color: #f4f4f4;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
            border-radius: 10px;
            overflow: hidden;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #4caf50 0%, #45a049 100%);
            color: white;
            padding: 30px 20px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
            font-weight: 700;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .order-details {{
            background-color: #f8f9fa;
            border-radius: 8px;
            padding: 25px;
            margin: 25px 0;
        }}
        .order-number {{
            font-size: 24px;
            font-weight: bold;
            color: #4caf50;
            text-align: center;
            margin-bottom: 20px;
        }}
        .detail-row {{
            display: flex;
            justify-content: space-between;
            margin: 10px 0;
            padding: 8px 0;
            border-bottom: 1px solid #eee;
        }}
        .detail-row:last-child {{
            border-bottom: none;
            font-weight: bold;
            font-size: 18px;
            color: #4caf50;
        }}
        .footer {{
            background-color: #2c3e50;
            color: white;
            padding: 25px 30px;
            text-align: center;
        }}
        .status-badge {{
            background: linear-gradient(135deg, #ff9800 0%, #f57c00 100%);
            color: white;
            padding: 8px 16px;
            border-radius: 20px;
            font-size: 14px;
            font-weight: bold;
            display: inline-block;
            margin: 10px 0;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>✅ Order Confirmed!</h1>
            <p>Thank you for your order, {customerName}!</p>
        </div>
        
        <div class=""content"">
            <div class=""order-details"">
                <div class=""order-number"">Order #{orderNumber}</div>
                
                <div class=""detail-row"">
                    <span>Order Date:</span>
                    <span>{orderDate:MMMM dd, yyyy 'at' h:mm tt}</span>
                </div>
                
                <div class=""detail-row"">
                    <span>Delivery Address:</span>
                    <span>{deliveryAddress}</span>
                </div>
                
                <div class=""detail-row"">
                    <span>Estimated Delivery:</span>
                    <span>{orderDate.AddMinutes(45):h:mm tt}</span>
                </div>
                
                <div class=""detail-row"">
                    <span>Total Amount:</span>
                    <span>RM {total:F2}</span>
                </div>
            </div>
            
            <div style=""text-align: center; margin: 30px 0;"">
                <div class=""status-badge"">🕐 Preparing Your Order</div>
                <p>We're working hard to prepare your delicious meal!</p>
            </div>
            
            <p style=""text-align: center; color: #666;"">
                You'll receive updates about your order status via email and SMS.<br>
                Thank you for choosing MackDihh!
            </p>
        </div>
        
        <div class=""footer"">
            <p><strong>MackDihh</strong> - Your favorite meals, delivered fast</p>
            <p>📞 +60 88 123 4567 | 📧 support@mackdihh.com</p>
            <p style=""font-size: 12px; opacity: 0.7;"">© 2025 MackDihh. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
} 