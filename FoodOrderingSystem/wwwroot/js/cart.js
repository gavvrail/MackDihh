// Using jQuery's document ready function for safety
$(function () {
    console.log('cart.js loaded.');

    // Use event delegation for quantity buttons and remove buttons
    // This ensures they work even if the cart content is updated via AJAX
    $(document).on('click', '.btn-update-quantity', function () {
        var button = $(this);
        var cartItemId = button.data('itemid');
        var quantityInput = button.siblings('.quantity-input');
        var currentQuantity = parseInt(quantityInput.val());
        var change = parseInt(button.data('change'));
        var newQuantity = currentQuantity + change;

        if (newQuantity > 0) {
            updateQuantity(cartItemId, newQuantity);
        } else {
            // If quantity becomes 0, treat it as a remove action
            removeItem(cartItemId);
        }
    });

    $(document).on('click', '.btn-remove-item', function (e) {
        e.preventDefault();
        var button = $(this);
        var cartItemId = button.data('itemid');
        removeItem(cartItemId);
    });

    // Checkout button is now a direct link, no JavaScript needed
    // The button will navigate directly to /Checkout via the href attribute


    // --- Helper Functions Below ---

    function updateQuantity(cartItemId, quantity) {
        var token = $('input[name="__RequestVerificationToken"]').val();
        $.post('/Cart/UpdateQuantity', { cartItemId: cartItemId, quantity: quantity, __RequestVerificationToken: token })
            .done(function (response) {
                if (response.success) {
                    // Reload the page to ensure all totals and item states are correct.
                    // This is simpler and more reliable than updating parts of the page manually.
                    location.reload();
                } else {
                    alert("There was an error updating your cart. Please try again.");
                }
            })
            .fail(function () {
                alert("A server error occurred. Please try again.");
            });
    }

    function removeItem(cartItemId) {
        var token = $('input[name="__RequestVerificationToken"]').val();
        $.post('/Cart/RemoveItem', { cartItemId: cartItemId, __RequestVerificationToken: token })
            .done(function (response) {
                if (response.success) {
                    // Show success message if points were refunded
                    if (response.message && response.message.includes('points refunded')) {
                        // Create a temporary success alert
                        var alertHtml = '<div class="alert alert-success alert-dismissible fade show" role="alert">' +
                                       '<i class="fas fa-check-circle me-2"></i>' + response.message +
                                       '<button type="button" class="btn-close" data-bs-dismiss="alert"></button>' +
                                       '</div>';
                        
                        // Insert at the top of the page
                        $('.cart-full-width .container-fluid').prepend(alertHtml);
                        
                        // Auto-dismiss after 5 seconds
                        setTimeout(function() {
                            $('.alert').fadeOut();
                        }, 5000);
                    }
                    
                    // Find the card for the removed item and fade it out
                    var itemCard = $('[data-itemid="' + cartItemId + '"]').closest('.cart-item-card');
                    itemCard.fadeOut(400, function () {
                        // After fading out, reload the page to update summary and cart state
                        location.reload();
                    });
                } else {
                    alert(response.message || "There was an error removing the item. Please try again.");
                }
            })
            .fail(function () {
                alert("A server error occurred. Please try again.");
            });
    }
});