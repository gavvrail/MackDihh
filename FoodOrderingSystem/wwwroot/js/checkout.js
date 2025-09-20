// Checkout Page JavaScript
$(document).ready(function() {
    // Get the user ID passed from the view
    const userId = window.checkoutUserId || null;
    
    // Auto-save user information to localStorage (user-specific)
    function saveUserInfo() {
        if (!userId) {
            console.log('No userId available for saving');
            return;
        }
        
        const userInfo = {
            deliveryAddress: $('#DeliveryAddress').val(),
            customerPhone: $('#CustomerPhone').val(),
            deliveryInstructions: $('#DeliveryInstructions').val(),
            notes: $('#Notes').val(),
            paymentMethod: $('input[name="PaymentMethod"]:checked').val(),
            promoCode: $('#promoCode').val(),
            timestamp: new Date().getTime()
        };
        
        console.log(`Saving user info for user: ${userId}`, userInfo);
        localStorage.setItem(`checkoutUserInfo_${userId}`, JSON.stringify(userInfo));
        console.log('Info saved to localStorage');
        
        // Show subtle save indicator
        showSaveIndicator();
    }

    // Auto-save card information to localStorage (user-specific) - excluding sensitive data
    function saveCardInfo() {
        if (!userId) {
            console.log('No userId available for saving card info');
            return;
        }
        
        const cardInfo = {
            cardHolderName: $('#CardHolderName').val(),
            cardExpiry: $('#CardExpiry').val(),
            // Note: We don't save card number or CVV for security reasons
            timestamp: new Date().getTime()
        };
        
        console.log(`Saving card info for user: ${userId}`, cardInfo);
        localStorage.setItem(`checkoutCardInfo_${userId}`, JSON.stringify(cardInfo));
        console.log('Card info saved to localStorage');
        
        // Show subtle save indicator for card section
        showCardSaveIndicator();
    }

    // Show card save indicator
    function showCardSaveIndicator() {
        // Remove existing indicators
        $('.card-auto-save-indicator').remove();
        
        // Add save indicator to card section
        const indicator = $('<small class="card-auto-save-indicator text-muted ms-2"><i class="fas fa-check-circle text-success"></i> Auto-saved</small>');
        $('.card-info-title').append(indicator);
        
        // Remove after 2 seconds
        setTimeout(() => {
            indicator.fadeOut(500, function() {
                $(this).remove();
            });
        }, 2000);
    }

    // Show save indicator
    function showSaveIndicator() {
        // Remove existing indicators
        $('.auto-save-indicator').remove();
        
        // Add save indicator
        const indicator = $('<small class="auto-save-indicator text-muted ms-2"><i class="fas fa-check-circle text-success"></i> Auto-saved</small>');
        $('.form-section h5').first().append(indicator);
        
        // Remove after 2 seconds
        setTimeout(() => {
            indicator.fadeOut(500, function() {
                $(this).remove();
            });
        }, 2000);
    }

    // Load saved user information (user-specific)
    function loadUserInfo() {
        if (!userId) {
            console.log('No userId available for loading saved info');
            return;
        }
        
        console.log(`Loading saved info for user: ${userId}`);
        const savedInfo = localStorage.getItem(`checkoutUserInfo_${userId}`);
        console.log('Saved info from localStorage:', savedInfo);
        
        if (savedInfo) {
            try {
                const userInfo = JSON.parse(savedInfo);
                console.log('Parsed user info:', userInfo);
                let fieldsLoaded = false;
                
                // Always load saved information (latest takes precedence)
                if (userInfo.deliveryAddress && userInfo.deliveryAddress.trim()) {
                    console.log('Loading delivery address:', userInfo.deliveryAddress);
                    $('#DeliveryAddress').val(userInfo.deliveryAddress);
                    fieldsLoaded = true;
                }
                if (userInfo.customerPhone && userInfo.customerPhone.trim()) {
                    console.log('Loading customer phone:', userInfo.customerPhone);
                    $('#CustomerPhone').val(userInfo.customerPhone);
                    fieldsLoaded = true;
                }
                if (userInfo.deliveryInstructions && userInfo.deliveryInstructions.trim()) {
                    console.log('Loading delivery instructions:', userInfo.deliveryInstructions);
                    $('#DeliveryInstructions').val(userInfo.deliveryInstructions);
                }
                if (userInfo.notes && userInfo.notes.trim()) {
                    console.log('Loading notes:', userInfo.notes);
                    $('#Notes').val(userInfo.notes);
                }
                if (userInfo.paymentMethod) {
                    console.log('Loading payment method:', userInfo.paymentMethod);
                    $(`input[name="PaymentMethod"][value="${userInfo.paymentMethod}"]`).prop('checked', true);
                    toggleCardDetails();
                    
                    // If card payment method was selected, load card info
                    if (userInfo.paymentMethod === 'Card') {
                        setTimeout(() => {
                            loadCardInfo();
                        }, 100); // Small delay to ensure card section is visible
                    }
                }
                
                console.log('Fields loaded:', fieldsLoaded);
                // Show notification if we loaded saved info
                if (fieldsLoaded) {
                    showAutoFillNotification();
                }
            } catch (e) {
                console.log('Error loading saved user info:', e);
            }
        } else {
            console.log('No saved info found in localStorage');
        }
    }

    // Load saved card information (user-specific) - excluding sensitive data
    function loadCardInfo() {
        if (!userId) {
            console.log('No userId available for loading card info');
            return;
        }
        
        console.log(`Loading saved card info for user: ${userId}`);
        const savedCardInfo = localStorage.getItem(`checkoutCardInfo_${userId}`);
        console.log('Saved card info from localStorage:', savedCardInfo);
        
        if (savedCardInfo) {
            try {
                const cardInfo = JSON.parse(savedCardInfo);
                console.log('Parsed card info:', cardInfo);
                let cardFieldsLoaded = false;
                
                // Load saved card information (excluding sensitive data)
                if (cardInfo.cardHolderName && cardInfo.cardHolderName.trim()) {
                    console.log('Loading card holder name:', cardInfo.cardHolderName);
                    $('#CardHolderName').val(cardInfo.cardHolderName);
                    cardFieldsLoaded = true;
                }
                if (cardInfo.cardExpiry && cardInfo.cardExpiry.trim()) {
                    console.log('Loading card expiry:', cardInfo.cardExpiry);
                    $('#CardExpiry').val(cardInfo.cardExpiry);
                    cardFieldsLoaded = true;
                }
                
                console.log('Card fields loaded:', cardFieldsLoaded);
                // Show notification if we loaded saved card info
                if (cardFieldsLoaded) {
                    showCardAutoFillNotification();
                }
            } catch (e) {
                console.log('Error loading saved card info:', e);
            }
        } else {
            console.log('No saved card info found in localStorage');
        }
    }

    // Show auto-fill notification
    function showAutoFillNotification() {
        const notification = $('<div class="alert alert-info alert-dismissible fade show mt-3" role="alert">' +
            '<i class="fas fa-info-circle me-2"></i>' +
            'Your delivery information has been auto-filled from your previous order. You can modify it if needed.' +
            '<button type="button" class="btn-close" data-bs-dismiss="alert"></button>' +
            '</div>');
        
        $('.checkout-header').after(notification);
        
        // Auto-dismiss after 5 seconds
        setTimeout(() => {
            notification.alert('close');
        }, 5000);
    }

    // Show card auto-fill notification
    function showCardAutoFillNotification() {
        const notification = $('<div class="alert alert-success alert-dismissible fade show mt-2" role="alert">' +
            '<i class="fas fa-credit-card me-2"></i>' +
            'Your card information has been auto-filled (card number and CVV need to be entered for security).' +
            '<button type="button" class="btn-close" data-bs-dismiss="alert"></button>' +
            '</div>');
        
        $('#cardDetailsSection').prepend(notification);
        
        // Auto-dismiss after 6 seconds
        setTimeout(() => {
            notification.alert('close');
        }, 6000);
    }

    // Auto-save on input change (with debounce) - focus on delivery information
    let saveTimeout;
    $('#DeliveryAddress, #CustomerPhone, #DeliveryInstructions, #Notes, input[name="PaymentMethod"]').on('input change', function() {
        clearTimeout(saveTimeout);
        saveTimeout = setTimeout(saveUserInfo, 1500); // Save after 1.5 seconds of no typing
    });

    // Auto-save on card input change (with debounce) - focus on card information (excluding sensitive data)
    let cardSaveTimeout;
    $('#CardHolderName, #CardExpiry').on('input change', function() {
        clearTimeout(cardSaveTimeout);
        cardSaveTimeout = setTimeout(saveCardInfo, 1500); // Save after 1.5 seconds of no typing
    });
    
    // Immediate save on successful form submission
    $('#checkoutForm').on('submit', function() {
        saveUserInfo();
        saveCardInfo();
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

    // Initialize payment method on page load
    toggleCardDetails();

    // ========================================
    // CREDIT CARD VALIDATION SYSTEM
    // ========================================

    // Luhn Algorithm for card number validation
    function luhnCheck(cardNumber) {
        const digits = cardNumber.replace(/\D/g, '').split('').map(Number);
        let sum = 0;
        let isEven = false;
        
        for (let i = digits.length - 1; i >= 0; i--) {
            let digit = digits[i];
            
            if (isEven) {
                digit *= 2;
                if (digit > 9) {
                    digit -= 9;
                }
            }
            
            sum += digit;
            isEven = !isEven;
        }
        
        return sum % 10 === 0;
    }

    // Detect card type based on card number
    function detectCardType(cardNumber) {
        const cleanNumber = cardNumber.replace(/\D/g, '');
        
        // Visa: starts with 4
        if (/^4/.test(cleanNumber)) {
            return 'visa';
        }
        
        // Mastercard: starts with 5[1-5] or 2[2-7]
        if (/^5[1-5]/.test(cleanNumber) || /^2[2-7]/.test(cleanNumber)) {
            return 'mastercard';
        }
        
        // American Express: starts with 34 or 37
        if (/^3[47]/.test(cleanNumber)) {
            return 'amex';
        }
        
        return null;
    }

    // Show validation error
    function showValidationError(fieldId, message) {
        const errorElement = $(`#${fieldId}Error`);
        const inputElement = $(`#${fieldId}`);
        
        // Hide ASP.NET validation message to avoid duplicates
        inputElement.siblings('.text-danger').hide();
        
        errorElement.text(message).removeClass('hide').addClass('show');
        inputElement.removeClass('valid').addClass('invalid');
    }

    // Hide validation error
    function hideValidationError(fieldId) {
        const errorElement = $(`#${fieldId}Error`);
        const inputElement = $(`#${fieldId}`);
        
        // Show ASP.NET validation message again when field is valid
        inputElement.siblings('.text-danger').show();
        
        errorElement.removeClass('show').addClass('hide');
        inputElement.removeClass('invalid').addClass('valid');
    }

    // Clear validation state
    function clearValidationState(fieldId) {
        const errorElement = $(`#${fieldId}Error`);
        const inputElement = $(`#${fieldId}`);
        
        // Show ASP.NET validation message again when clearing state
        inputElement.siblings('.text-danger').show();
        
        errorElement.removeClass('show hide');
        inputElement.removeClass('valid invalid');
    }

    // Card Number Validation and Formatting
    $('#CardNumber').on('input', function() {
        let value = $(this).val().replace(/\D/g, '');
        const originalLength = value.length;
        
        // Limit to 19 digits (longest card number)
        if (value.length > 19) {
            value = value.substring(0, 19);
        }
        
        // Format with spaces every 4 digits
        const formattedValue = value.replace(/(\d{4})(?=\d)/g, '$1 ');
        $(this).val(formattedValue);
        
        // Detect and display card type
        const cardType = detectCardType(value);
        const iconElement = $('#cardTypeIcon');
        
        iconElement.removeClass('visa mastercard amex');
        if (cardType) {
            iconElement.addClass(cardType);
        }
        
        // Validate card number
        if (value.length === 0) {
            clearValidationState('cardNumber');
        } else if (value.length < 13) {
            showValidationError('cardNumber', 'Card number must be at least 13 digits');
        } else if (value.length > 19) {
            showValidationError('cardNumber', 'Card number cannot exceed 19 digits');
        } else if (!luhnCheck(value)) {
            showValidationError('cardNumber', 'Invalid card number');
        } else {
            hideValidationError('cardNumber');
        }
    });

    // Card Holder Name Validation
    $('#CardHolderName').on('input', function() {
        const value = $(this).val().trim();
        const namePattern = /^[a-zA-Z\s\-']+$/;
        
        if (value.length === 0) {
            clearValidationState('cardHolder');
        } else if (value.length < 2) {
            showValidationError('cardHolder', 'Name must be at least 2 characters long');
        } else if (!namePattern.test(value)) {
            showValidationError('cardHolder', 'Name can only contain letters, spaces, hyphens, and apostrophes');
        } else {
            hideValidationError('cardHolder');
        }
    });

    // Card Expiry Validation and Formatting
    $('#CardExpiry').on('input', function() {
        let value = $(this).val().replace(/\D/g, '');
        
        // Limit to 4 digits
        if (value.length > 4) {
            value = value.substring(0, 4);
        }
        
        // Auto-format with slash
        if (value.length >= 2) {
            value = value.substring(0, 2) + '/' + value.substring(2, 4);
        }
        
        $(this).val(value);
        
        // Validate expiry date
        if (value.length === 0) {
            clearValidationState('cardExpiry');
        } else if (value.length < 5) {
            showValidationError('cardExpiry', 'Please enter expiry date in MM/YY format');
        } else {
            const [month, year] = value.split('/');
            const monthNum = parseInt(month, 10);
            const yearNum = parseInt('20' + year, 10);
            
            // Validate month
            if (monthNum < 1 || monthNum > 12) {
                showValidationError('cardExpiry', 'Invalid month. Please enter 01-12');
                return;
            }
            
            // Check if expired (assuming current date is September 2025)
            const currentYear = 2025;
            const currentMonth = 9;
            
            if (yearNum < currentYear || (yearNum === currentYear && monthNum < currentMonth)) {
                showValidationError('cardExpiry', 'Card has expired');
            } else {
                hideValidationError('cardExpiry');
            }
        }
    });

    // CVV Validation
    $('#CardCvv').on('input', function() {
        let value = $(this).val().replace(/\D/g, '');
        const cardNumber = $('#CardNumber').val().replace(/\D/g, '');
        const cardType = detectCardType(cardNumber);
        const isAmex = cardType === 'amex';
        const expectedLength = isAmex ? 4 : 3;
        
        // Limit length based on card type
        if (value.length > expectedLength) {
            value = value.substring(0, expectedLength);
        }
        
        $(this).val(value);
        
        // Update maxlength attribute based on card type
        $(this).attr('maxlength', expectedLength);
        
        // Validate CVV
        if (value.length === 0) {
            clearValidationState('cardCvv');
        } else if (value.length < expectedLength) {
            const lengthText = isAmex ? '4 digits for American Express' : '3 digits';
            showValidationError('cardCvv', `CVV must be ${lengthText}`);
        } else {
            hideValidationError('cardCvv');
        }
    });

    // Prevent non-numeric input on card number and CVV
    $('#CardNumber, #CardCvv').on('keypress', function(e) {
        // Allow backspace, delete, tab, escape, enter
        if ([8, 9, 27, 13, 46].indexOf(e.keyCode) !== -1 ||
            // Allow Ctrl+A, Ctrl+C, Ctrl+V, Ctrl+X
            (e.keyCode === 65 && e.ctrlKey === true) ||
            (e.keyCode === 67 && e.ctrlKey === true) ||
            (e.keyCode === 86 && e.ctrlKey === true) ||
            (e.keyCode === 88 && e.ctrlKey === true)) {
            return;
        }
        // Ensure that it is a number
        if ((e.shiftKey || (e.keyCode < 48 || e.keyCode > 57)) && (e.keyCode < 96 || e.keyCode > 105)) {
            e.preventDefault();
        }
    });

    // Prevent non-numeric input on expiry date
    $('#CardExpiry').on('keypress', function(e) {
        // Allow backspace, delete, tab, escape, enter
        if ([8, 9, 27, 13, 46].indexOf(e.keyCode) !== -1 ||
            // Allow Ctrl+A, Ctrl+C, Ctrl+V, Ctrl+X
            (e.keyCode === 65 && e.ctrlKey === true) ||
            (e.keyCode === 67 && e.ctrlKey === true) ||
            (e.keyCode === 86 && e.ctrlKey === true) ||
            (e.keyCode === 88 && e.ctrlKey === true)) {
            return;
        }
        // Ensure that it is a number
        if ((e.shiftKey || (e.keyCode < 48 || e.keyCode > 57)) && (e.keyCode < 96 || e.keyCode > 105)) {
            e.preventDefault();
        }
    });

    // Enhanced form validation
    function validateAllCardFields() {
        let isValid = true;
        
        // Validate card number
        const cardNumber = $('#CardNumber').val().replace(/\D/g, '');
        if (cardNumber.length === 0) {
            showValidationError('cardNumber', 'Card number is required');
            isValid = false;
        } else if (cardNumber.length < 13 || cardNumber.length > 19) {
            showValidationError('cardNumber', 'Invalid card number length');
            isValid = false;
        } else if (!luhnCheck(cardNumber)) {
            showValidationError('cardNumber', 'Invalid card number');
            isValid = false;
        }
        
        // Validate card holder name
        const cardHolder = $('#CardHolderName').val().trim();
        if (cardHolder.length === 0) {
            showValidationError('cardHolder', 'Card holder name is required');
            isValid = false;
        } else if (cardHolder.length < 2) {
            showValidationError('cardHolder', 'Name must be at least 2 characters');
            isValid = false;
        }
        
        // Validate expiry
        const expiry = $('#CardExpiry').val();
        if (expiry.length === 0) {
            showValidationError('cardExpiry', 'Expiry date is required');
            isValid = false;
        } else if (expiry.length < 5) {
            showValidationError('cardExpiry', 'Invalid expiry date format');
            isValid = false;
        }
        
        // Validate CVV
        const cvv = $('#CardCvv').val();
        const cardType = detectCardType(cardNumber);
        const expectedCvvLength = cardType === 'amex' ? 4 : 3;
        
        if (cvv.length === 0) {
            showValidationError('cardCvv', 'CVV is required');
            isValid = false;
        } else if (cvv.length < expectedCvvLength) {
            showValidationError('cardCvv', `CVV must be ${expectedCvvLength} digits`);
            isValid = false;
        }
        
        return isValid;
    }

    // Add card validation to form submission
    const originalFormValidation = $('#checkoutForm').data('events')?.submit || [];
    
    $('#checkoutForm').off('submit').on('submit', function(e) {
        const paymentMethod = $('input[name="PaymentMethod"]:checked').val();
        
        if (paymentMethod === 'Card') {
            if (!validateAllCardFields()) {
                e.preventDefault();
                return false;
            }
        }
        
        // Call original validation functions
        originalFormValidation.forEach(handler => {
            if (typeof handler.handler === 'function') {
                handler.handler.call(this, e);
            }
        });
    });

    // Phone number validation and formatting (ID consistency fixed)
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
        const $btn = $(this).find('button[type="submit"]');
        $btn.prop('disabled', true)
            .html('<i class="fas fa-spinner fa-spin me-2"></i>Processing...');

        // Save user info before submission
        saveUserInfo();
        
        console.log('Form submission started...');
    });

    // Re-enable button if validation fails
    $(document).on('invalid', function(e) {
        const $btn = $('.btn-checkout');
        $btn.prop('disabled', false)
            .html('<i class="fas fa-check me-2"></i>Place Food Order');
    });

    // Auto-dismiss alerts after 5 seconds
    setTimeout(function() {
        $('.alert').fadeOut('slow');
    }, 5000);


    // Promo Code Functionality
    $('#applyPromoCode').click(function() {
        const promoCode = $('#promoCode').val().trim();
        if (!promoCode) {
            showPromoMessage('Please enter a promo code.', 'warning');
            return;
        }

        // Show loading state
        const $btn = $(this);
        const originalText = $btn.html();
        $btn.html('<i class="fas fa-spinner fa-spin me-1"></i>Applying...').prop('disabled', true);

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
                        showAppliedPromoCode(promoCode, response);
                    }
                } else {
                    showPromoMessage(response.message, 'danger');
                }
            },
            error: function() {
                showPromoMessage('Error applying promo code. Please try again.', 'danger');
            },
            complete: function() {
                // Reset button state
                $btn.html(originalText).prop('disabled', false);
            }
        });
    });

    // Remove Promo Code Functionality
    $('#removePromoCode').click(function() {
        // Show loading state
        const $btn = $(this);
        const originalText = $btn.html();
        $btn.html('<i class="fas fa-spinner fa-spin me-1"></i>Removing...').prop('disabled', true);

        // Remove promo code
        removePromoCode();
        
        // Reset button state
        setTimeout(() => {
            $btn.html(originalText).prop('disabled', false);
        }, 500);
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

    function showAppliedPromoCode(promoCode, response) {
        // Hide input row and show applied promo row
        $('#promoCodeInputRow').hide();
        $('#appliedPromoCodeRow').show();
        
        // Update applied promo display
        $('#appliedPromoCodeText').text(promoCode.toUpperCase());
        
        let details = '';
        if (response.discountPercentage > 0) {
            details = `${response.discountPercentage}% discount applied`;
        } else if (response.discountAmount > 0) {
            details = `RM${response.discountAmount.toFixed(2)} discount applied`;
        }
        if (response.bonusPoints > 0) {
            details += ` • +${response.bonusPoints} bonus points`;
        }
        $('#appliedPromoDetails').text(details);
        
        // Store applied promo data for removal
        window.appliedPromoData = {
            code: promoCode,
            discountAmount: response.discountAmount
        };
    }

    function removePromoCode() {
        // Hide applied promo row and show input row
        $('#appliedPromoCodeRow').hide();
        $('#promoCodeInputRow').show();
        
        // Clear input and reset totals
        $('#promoCode').val('');
        $('#promoCodeMessage').hide();
        
        // Reset order total
        if (window.appliedPromoData && window.appliedPromoData.discountAmount > 0) {
            resetOrderTotal();
        }
        
        // Clear stored promo data
        window.appliedPromoData = null;
        
        showPromoMessage('Promo code removed successfully.', 'info');
    }

    function updateOrderTotal(discountAmount) {
        // Update the discount line
        if (discountAmount > 0) {
            $('#discount-line').show();
            $('#discount-amount').text('-RM' + discountAmount.toFixed(2));
            
            // Calculate new total
            const subtotalText = $('#subtotal-amount').text().replace('RM', '').replace(',', '');
            const subtotal = parseFloat(subtotalText);
            const tax = subtotal * 0.06;
            const deliveryFee = subtotal >= 100 ? 0 : 5.00;
            const newTotal = subtotal + tax + deliveryFee - discountAmount;
            
            $('#total-amount').text('RM' + newTotal.toFixed(2));
        } else {
            $('#discount-line').hide();
        }
    }

    function resetOrderTotal() {
        // Hide discount line
        $('#discount-line').hide();
        
        // Recalculate original total
        const subtotalText = $('#subtotal-amount').text().replace('RM', '').replace(',', '');
        const subtotal = parseFloat(subtotalText);
        const tax = subtotal * 0.06;
        const deliveryFee = subtotal >= 100 ? 0 : 5.00;
        const originalTotal = subtotal + tax + deliveryFee;
        
        $('#total-amount').text('RM' + originalTotal.toFixed(2));
    }
});
