using FoodOrderingSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace FoodOrderingSystem.ViewModels
{
    public class CheckoutViewModel : IValidatableObject
    {
        public Cart Cart { get; set; } = null!;

        [Required(ErrorMessage = "Delivery address is required")]
        [StringLength(200, ErrorMessage = "Address cannot be longer than 200 characters")]
        public string DeliveryAddress { get; set; } = "";

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(11, MinimumLength = 10, ErrorMessage = "Phone number must be between 10 and 11 digits")]
        [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Phone number must contain only digits and be 10-11 characters long")]
        public string CustomerPhone { get; set; } = "";

        [Display(Name = "Delivery Instructions")]
        public string? DeliveryInstructions { get; set; }

        [Display(Name = "Order Notes")]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Please select a payment method")]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "";

        [Display(Name = "Card Number")]
        public string? CardNumber { get; set; }

        [Display(Name = "Card Holder Name")]
        public string? CardHolderName { get; set; }

        [Display(Name = "Expiry Date")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/([0-9]{2})$", ErrorMessage = "Please enter a valid expiry date in MM/YY format.")]
        public string? CardExpiry { get; set; }

        [Display(Name = "CVV")]
        public string? CardCvv { get; set; }

        [Display(Name = "Promo Code")]
        public string? PromoCode { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (PaymentMethod == "Card")
            {
                if (string.IsNullOrWhiteSpace(CardNumber))
                    yield return new ValidationResult("Card number is required.", new[] { nameof(CardNumber) });
                if (string.IsNullOrWhiteSpace(CardHolderName))
                    yield return new ValidationResult("Card holder name is required.", new[] { nameof(CardHolderName) });
                if (string.IsNullOrWhiteSpace(CardExpiry))
                    yield return new ValidationResult("Expiry date is required.", new[] { nameof(CardExpiry) });
                if (string.IsNullOrWhiteSpace(CardCvv))
                    yield return new ValidationResult("CVV is required.", new[] { nameof(CardCvv) });
            }

            if (!string.IsNullOrEmpty(CardExpiry))
            {
                if (IsCardExpired(CardExpiry))
                {
                    yield return new ValidationResult("This card has expired.", new[] { nameof(CardExpiry) });
                }
            }
        }

        private bool IsCardExpired(string expiryDate)
        {
            try
            {
                var parts = expiryDate.Split('/');
                if (parts.Length == 2 && int.TryParse(parts[0], out int month) && int.TryParse(parts[1], out int year))
                {
                    var expiry = new DateTime(2000 + year, month, 1).AddMonths(1).AddDays(-1);
                    return expiry < DateTime.Now;
                }
            }
            catch { /* Parsing failed, let other validators handle it */ }
            return true;
        }
    }
}
