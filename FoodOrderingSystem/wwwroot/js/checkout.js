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

    // Form submission handling
    $('#checkoutForm').on('submit', function(e) {
        // Only check required fields
        const deliveryAddress = $('#DeliveryAddress').val().trim();
        const customerPhone = $('#CustomerPhone').val().trim();
        
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
});
