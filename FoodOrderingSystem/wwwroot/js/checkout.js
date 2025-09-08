// Checkout Page JavaScript
$(document).ready(function() {
    // Get the user ID passed from the view
    const userId = window.checkoutUserId || null;
    
    // Auto-save user information to localStorage (user-specific)
    function saveUserInfo() {
        if (!userId) return;
        
        const userInfo = {
            deliveryAddress: $('#DeliveryAddress').val(),
            customerPhone: $('#CustomerPhone').val(),
            deliveryInstructions: $('#DeliveryInstructions').val(),
            notes: $('#Notes').val(),
            paymentMethod: $('input[name="PaymentMethod"]:checked').val(),
            promoCode: $('#promoCode').val(),
            timestamp: new Date().getTime()
        };
        localStorage.setItem(`checkoutUserInfo_${userId}`, JSON.stringify(userInfo));
    }

    // Load saved user information (user-specific)
    function loadUserInfo() {
        if (!userId) return;
        
        const savedInfo = localStorage.getItem(`checkoutUserInfo_${userId}`);
        if (savedInfo) {
            try {
                const userInfo = JSON.parse(savedInfo);
                // Only auto-fill if fields are empty
                if (!$('#DeliveryAddress').val()) {
                    $('#DeliveryAddress').val(userInfo.deliveryAddress || '');
                }
                if (!$('#CustomerPhone').val()) {
                    $('#CustomerPhone').val(userInfo.customerPhone || '');
                }
                if (!$('#DeliveryInstructions').val()) {
                    $('#DeliveryInstructions').val(userInfo.deliveryInstructions || '');
                }
                if (!$('#Notes').val()) {
                    $('#Notes').val(userInfo.notes || '');
                }
                if (userInfo.paymentMethod) {
                    $(`input[name="PaymentMethod"][value="${userInfo.paymentMethod}"]`).prop('checked', true);
                    toggleCardDetails();
                }
            } catch (e) {
                console.log('Error loading saved user info:', e);
            }
        }
    }

    // Auto-save on input change (with debounce)
    let saveTimeout;
    $('input, textarea').on('input', function() {
        clearTimeout(saveTimeout);
        saveTimeout = setTimeout(saveUserInfo, 1000); // Save after 1 second of no typing
    });

    // Load saved info when page loads
    loadUserInfo();

    // Payment method handling
    function toggleCardDetails() {
        const selectedMethod = $('input[name="PaymentMethod"]:checked').val();
        if (selectedMethod === 'Card') {
            $('#cardDetailsSection').slideDown();
            // Make card fields required
            $('#CardNumber, #CardHolderName, #CardExpiry, #CardCvv').prop('required', true);
        } else {
            $('#cardDetailsSection').slideUp();
            // Remove required from card fields
            $('#CardNumber, #CardHolderName, #CardExpiry, #CardCvv').prop('required', false);
        }
    }

    // Handle payment method selection
    $('input[name="PaymentMethod"]').on('change', function() {
        toggleCardDetails();
        saveUserInfo();
    });

    // Card number formatting
    $('#CardNumber').on('input', function() {
        let value = $(this).val().replace(/\D/g, '');
        value = value.replace(/(\d{4})(?=\d)/g, '$1 ');
        $(this).val(value);
    });

    // Phone number validation and formatting
    $('#CustomerPhone').on('input', function() {
        // Remove any non-numeric characters
        this.value = this.value.replace(/[^0-9]/g, '');
        
        // Limit to 11 digits
        if (this.value.length > 11) {
            this.value = this.value.substring(0, 11);
        }
    });
    
    // Prevent non-numeric input on phone number field
    $('#CustomerPhone').on('keypress', function(e) {
        // Allow backspace, delete, tab, escape, enter
        if ([8, 9, 27, 13, 46].indexOf(e.keyCode) !== -1 ||
            // Allow Ctrl+A, Ctrl+C, Ctrl+V, Ctrl+X
            (e.keyCode === 65 && e.ctrlKey === true) ||
            (e.keyCode === 67 && e.ctrlKey === true) ||
            (e.keyCode === 86 && e.ctrlKey === true) ||
            (e.keyCode === 88 && e.ctrlKey === true)) {
            return;
        }
        // Ensure that it is a number and stop the keypress
        if ((e.shiftKey || (e.keyCode < 48 || e.keyCode > 57)) && (e.keyCode < 96 || e.keyCode > 105)) {
            e.preventDefault();
        }
    });

    // Handle saved promo code selection
    $('#savedPromoCodeSelect').on('change', function() {
        const selectedOption = $(this).find('option:selected');
        const promoCode = selectedOption.val();
        
        if (promoCode) {
            // Auto-fill the manual promo code input
            $('#promoCode').val(promoCode);
            
            // Show promo code details
            const discountPercentage = selectedOption.data('discount-percentage');
            const discountedPrice = selectedOption.data('discounted-price');
            const minimumOrder = selectedOption.data('minimum-order');
            
            let details = 'Promo Code Details:\n';
            if (discountPercentage > 0) {
                details += `• ${discountPercentage}% discount\n`;
            }
            if (discountedPrice > 0) {
                details += `• RM${discountedPrice} off\n`;
            }
            if (minimumOrder > 0) {
                details += `• Minimum order: RM${minimumOrder}\n`;
            }
            
            // Show details in a temporary alert
            const alertDiv = $('<div class="alert alert-info alert-dismissible fade show" role="alert">' +
                '<i class="fas fa-info-circle me-2"></i>' + details.replace(/\n/g, '<br>') +
                '<button type="button" class="btn-close" data-bs-dismiss="alert"></button></div>');
            
            $('#promoCodeMessage').html(alertDiv);
            $('#promoCodeMessage').show();
            
            // Auto-remove after 5 seconds
            setTimeout(() => {
                alertDiv.alert('close');
            }, 5000);
        } else {
            // Clear the manual input if no promo code selected
            $('#promoCode').val('');
            $('#promoCodeMessage').hide();
        }
    });

    // Expiry date formatting with month validation and expiry check
    $('#CardExpiry').on('input', function() {
        let value = $(this).val().replace(/\D/g, '');
        
        // Check if the first two digits (month) are greater than 12
        if (value.length >= 2) {
            const month = parseInt(value.substring(0, 2));
            if (month > 12) {
                // If month is greater than 12, limit it to 12
                value = '12' + value.substring(2);
            }
            value = value.substring(0, 2) + '/' + value.substring(2, 4);
        }
        
        $(this).val(value);
        
        // Validate expiry date if we have a complete date
        if (value.length === 5) {
            validateExpiryDate(value);
        }
    });

    // Prevent invalid month input in real-time
    $('#CardExpiry').on('keypress', function(e) {
        const char = String.fromCharCode(e.which);
        const currentValue = $(this).val().replace(/\D/g, '');
        
        // If we're entering the first digit of the month
        if (currentValue.length === 0) {
            // Only allow 0 or 1 for the first digit
            if (char !== '0' && char !== '1') {
                e.preventDefault();
                return false;
            }
        }
        // If we're entering the second digit of the month
        else if (currentValue.length === 1) {
            const firstDigit = currentValue[0];
            // If first digit is 0, second digit can be 1-9
            // If first digit is 1, second digit can be 0-2
            if (firstDigit === '0' && char === '0') {
                e.preventDefault();
                return false;
            } else if (firstDigit === '1' && (char < '0' || char > '2')) {
                e.preventDefault();
                return false;
            }
        }
    });

    // Handle backspace/delete to allow erasing the slash
    $('#CardExpiry').on('keydown', function(e) {
        // Handle backspace
        if (e.keyCode === 8) { // Backspace key
            const currentValue = $(this).val();
            const cursorPos = $(this).get(0).selectionStart;
            
            // If cursor is right after the slash, move it before the slash
            if (cursorPos === 3 && currentValue.charAt(2) === '/') {
                e.preventDefault();
                $(this).val(currentValue.substring(0, 2));
                $(this).get(0).setSelectionRange(2, 2);
                return false;
            }
        }
        // Handle delete
        else if (e.keyCode === 46) { // Delete key
            const currentValue = $(this).val();
            const cursorPos = $(this).get(0).selectionStart;
            
            // If cursor is right before the slash, delete the slash and move cursor
            if (cursorPos === 2 && currentValue.charAt(2) === '/') {
                e.preventDefault();
                $(this).val(currentValue.substring(0, 2) + currentValue.substring(3));
                $(this).get(0).setSelectionRange(2, 2);
                return false;
            }
        }
    });

    // Function to validate expiry date
    function validateExpiryDate(expiryDate) {
        const parts = expiryDate.split('/');
        if (parts.length === 2) {
            const month = parseInt(parts[0]);
            const year = parseInt('20' + parts[1]); // Convert YY to YYYY
            
            const currentDate = new Date();
            const currentYear = currentDate.getFullYear();
            const currentMonth = currentDate.getMonth() + 1; // getMonth() returns 0-11
            
            const expiryDateObj = new Date(year, month - 1); // month - 1 because Date constructor expects 0-11
            const currentDateObj = new Date(currentYear, currentMonth - 1);
            
            if (expiryDateObj < currentDateObj) {
                // Card is expired
                const $field = $('#CardExpiry');
                $field.addClass('is-invalid');
                
                // Remove any existing error message
                $field.siblings('.invalid-feedback').remove();
                
                // Add error message
                $field.after('<div class="invalid-feedback">This card has expired. Please use a valid expiry date.</div>');
                
                return false;
            } else {
                // Card is valid
                const $field = $('#CardExpiry');
                $field.removeClass('is-invalid');
                $field.siblings('.invalid-feedback').remove();
                return true;
            }
        }
        return true;
    }

    // CVV validation
    $('#CardCvv').on('input', function() {
        let value = $(this).val().replace(/\D/g, '');
        $(this).val(value);
    });

    // Form submission handling
    $('#checkoutForm').on('submit', function(e) {
        // Only check required fields
        const deliveryAddress = $('#DeliveryAddress').val().trim();
        const customerPhone = $('#CustomerPhone').val().trim();
        const paymentMethod = $('input[name="PaymentMethod"]:checked').val();
        
        if (!deliveryAddress) {
            alert('Please enter a delivery address');
            $('#DeliveryAddress').focus();
            return false;
        }
        
        if (!customerPhone) {
            alert('Please enter a phone number');
            $('#CustomerPhone').focus();
            return false;
        }
        
        if (!paymentMethod) {
            alert('Please select a payment method');
            return false;
        }
        
        // Validate card details if card payment is selected
        if (paymentMethod === 'Card') {
            const cardNumber = $('#CardNumber').val().trim();
            const cardHolderName = $('#CardHolderName').val().trim();
            const cardExpiry = $('#CardExpiry').val().trim();
            const cardCvv = $('#CardCvv').val().trim();
            
            if (!cardNumber || cardNumber.replace(/\s/g, '').length < 16) {
                alert('Please enter a valid card number');
                $('#CardNumber').focus();
                return false;
            }
            
            if (!cardHolderName) {
                alert('Please enter the card holder name');
                $('#CardHolderName').focus();
                return false;
            }
            
            if (!cardExpiry || cardExpiry.length < 5) {
                alert('Please enter a valid expiry date (MM/YY)');
                $('#CardExpiry').focus();
                return false;
            }
            
            // Check if card is expired
            if (!validateExpiryDate(cardExpiry)) {
                alert('This card has expired. Please use a valid expiry date.');
                $('#CardExpiry').focus();
                return false;
            }
            
            if (!cardCvv || cardCvv.length < 3) {
                alert('Please enter a valid CVV');
                $('#CardCvv').focus();
                return false;
            }
        }

        // Disable button and show processing
        const $btn = $('#placeOrderBtn');
        $btn.prop('disabled', true)
            .html('<i class="fas fa-spinner fa-spin me-2"></i>Processing...');

        // Save user info before submission
        saveUserInfo();
        
        console.log('Form submission started...');
    });

    // Re-enable button if validation fails
    $(document).on('invalid', function(e) {
        const $btn = $('#placeOrderBtn');
        $btn.prop('disabled', false)
            .html('<i class="fas fa-credit-card me-2"></i>Place Order');
    });

    // Auto-dismiss alerts after 5 seconds
    setTimeout(function() {
        $('.alert').fadeOut('slow');
    }, 5000);

    // Show saved info notification (user-specific)
    if (userId && localStorage.getItem(`checkoutUserInfo_${userId}`)) {
        const savedInfo = JSON.parse(localStorage.getItem(`checkoutUserInfo_${userId}`));
        const savedDate = new Date(savedInfo.timestamp);
        const now = new Date();
        const hoursDiff = (now - savedDate) / (1000 * 60 * 60);
        
        if (hoursDiff < 24) { // Show notification if saved within 24 hours
            const notification = $('<div class="alert alert-info alert-dismissible fade show" role="alert">' +
                '<i class="fas fa-info-circle me-2"></i>' +
                'Your previous delivery information has been auto-filled. ' +
                '<button type="button" class="btn-close" data-bs-dismiss="alert"></button>' +
                '</div>');
            $('.checkout-container .container').prepend(notification);
            
            // Auto-dismiss after 3 seconds
            setTimeout(function() {
                notification.fadeOut('slow');
            }, 3000);
        }
    }

    // Promo Code Functionality
    $('#applyPromoCode').click(function() {
        const promoCode = $('#promoCode').val().trim();
        if (!promoCode) {
            showPromoMessage('Please enter a promo code.', 'warning');
            return;
        }

        // Apply promo code via AJAX
        $.ajax({
            url: '/Checkout/ApplyPromoCode',
            type: 'POST',
            data: {
                promoCode: promoCode,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function(response) {
                if (response.success) {
                    showPromoMessage(response.message, 'success');
                    // Update order total if discount applied
                    if (response.discountAmount > 0) {
                        updateOrderTotal(response.discountAmount);
                    }
                } else {
                    showPromoMessage(response.message, 'danger');
                }
            },
            error: function() {
                showPromoMessage('Error applying promo code. Please try again.', 'danger');
            }
        });
    });

    function showPromoMessage(message, type) {
        const messageDiv = $('#promoCodeMessage');
        messageDiv.removeClass('alert-success alert-danger alert-warning')
                  .addClass('alert-' + type)
                  .text(message)
                  .show();
        
        setTimeout(function() {
            messageDiv.fadeOut('slow');
        }, 5000);
    }

    function updateOrderTotal(discountAmount) {
        // Update the discount line
        if (discountAmount > 0) {
            $('#discountLine').show();
            $('#discountAmount').text('-RM ' + discountAmount.toFixed(2));
            
            // Calculate new total
            const subtotal = parseFloat($('#orderTotal').text().replace('RM ', '').replace(',', ''));
            const tax = subtotal * 0.06;
            const deliveryFee = subtotal >= 100 ? 0 : 5.00;
            const newTotal = subtotal + tax + deliveryFee - discountAmount;
            
            $('#orderTotal').text('RM ' + newTotal.toFixed(2));
        } else {
            $('#discountLine').hide();
        }
    }
});
