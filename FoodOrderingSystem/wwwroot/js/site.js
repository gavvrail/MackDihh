// Use the modern, preferred syntax for DOM ready
$(function () {
    // Cart form handling is now done in menu.js
    // This prevents duplicate event handlers

    function updateCartBadge(count) {
        var badge = $('.cart-badge');
        var container = $('.cart-icon-container');

        if (count > 0) {
            if (badge.length === 0) {
                // If the badge doesn't exist, create it
                container.append('<span class="cart-badge">' + count + '</span>');
            } else {
                // If it exists, just update the text
                badge.text(count);
            }
        } else {
            // If the count is 0, remove the badge
            badge.remove();
        }
    }

    // Handle logout functionality
    $(document).on('click', '#logoutLink', function(e) {
        e.preventDefault();
        
        // Create a hidden form and submit it
        var form = $('<form>', {
            'method': 'POST',
            'action': '/Identity/Account/Logout?returnUrl=' + encodeURIComponent(window.location.origin)
        });
        
        // Add anti-forgery token
        var token = $('input[name="__RequestVerificationToken"]').val();
        if (token) {
            form.append($('<input>', {
                'type': 'hidden',
                'name': '__RequestVerificationToken',
                'value': token
            }));
        }
        
        // Append form to body and submit
        $('body').append(form);
        form.submit();
    });

    // Notification system is now handled by notifications.js
});
