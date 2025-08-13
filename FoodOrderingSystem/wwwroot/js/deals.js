// Deals Page JavaScript
$(document).ready(function() {
    // Copy promo code functionality
    $('.copy-btn').on('click', function() {
        const $btn = $(this);
        const code = $btn.data('code');
        
        navigator.clipboard.writeText(code).then(function() {
            // Show success message on button
            const originalText = $btn.html();
            $btn.html('<i class="fas fa-check"></i>');
            $btn.removeClass('btn-outline-secondary').addClass('btn-success');
            
            setTimeout(function() {
                $btn.html(originalText);
                $btn.removeClass('btn-success').addClass('btn-outline-secondary');
            }, 2000);
        }).catch(function(err) {
            console.error('Could not copy text: ', err);
            alert('Could not copy promo code to clipboard');
        });
    });
    
    // Auto-dismiss alerts after 5 seconds
    setTimeout(function() {
        $('.alert').fadeOut('slow');
    }, 5000);
});
