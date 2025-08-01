$(function () {
    // Use event delegation to handle clicks on buttons that might be added dynamically
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

    function updateQuantity(cartItemId, quantity) {
        var token = $('input[name="__RequestVerificationToken"]').val();
        $.post('/Cart/UpdateQuantity', { cartItemId: cartItemId, quantity: quantity, __RequestVerificationToken: token })
            .done(function (response) {
                if (response.success) {
                    // Update cart count
                    updateCartCount(response.newCount);
                    
                    // Update subtotal
                    updateSubtotal(response.subtotal);
                    
                    // Update the specific item's total
                    var itemCard = $('[data-itemid="' + cartItemId + '"]').closest('.cart-item-card');
                    var totalElement = itemCard.find('.card-text.fw-bold');
                    var pricePerItem = parseFloat(itemCard.find('.card-text small').text().replace('RM', ''));
                    var newTotal = (pricePerItem * quantity).toFixed(2);
                    totalElement.text('RM' + newTotal);
                    
                    // Update quantity input
                    itemCard.find('.quantity-input').val(quantity);
                }
            });
    }

    function removeItem(cartItemId) {
        var token = $('input[name="__RequestVerificationToken"]').val();
        $.post('/Cart/RemoveItem', { cartItemId: cartItemId, __RequestVerificationToken: token })
            .done(function (response) {
                if (response.success) {
                    // Update cart count
                    updateCartCount(response.newCount);
                    
                    // Update subtotal
                    updateSubtotal(response.subtotal);
                    
                    // Remove the item card with animation
                    var itemCard = $('[data-itemid="' + cartItemId + '"]').closest('.cart-item-card');
                    itemCard.fadeOut(300, function() {
                        itemCard.remove();
                        
                        // Check if cart is empty
                        if ($('.cart-item-card').length === 0) {
                            location.reload(); // Reload to show empty cart message
                        }
                    });
                }
            });
    }

    function updateCartCount(newCount) {
        var cartBadge = $('#cart-count');
        if (cartBadge.length) {
            if (newCount > 0) {
                cartBadge.text(newCount).show();
            } else {
                cartBadge.text('0').hide();
            }
        }
    }

    function updateSubtotal(subtotal) {
        $('.summary-card .d-flex.justify-content-between span:last-child').each(function() {
            if ($(this).prev().text().trim() === 'Subtotal') {
                $(this).text(subtotal);
            }
        });
        
        // Update total as well
        $('.summary-card .d-flex.justify-content-between.fw-bold.h5 span:last-child').text(subtotal);
    }
});
