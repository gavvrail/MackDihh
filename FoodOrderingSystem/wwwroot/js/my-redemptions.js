// My Redemptions Page JavaScript

// Function to copy redemption code with animated toast
function copyCode(code) {
    navigator.clipboard.writeText(code).then(function() {
        // Show success message
        const toast = document.createElement('div');
        toast.className = 'alert alert-success position-fixed';
        toast.style.top = '20px';
        toast.style.right = '20px';
        toast.style.zIndex = '9999';
        toast.style.opacity = '0.95';
        toast.innerHTML = '<i class="fas fa-check me-2"></i>Code copied to clipboard!';
        document.body.appendChild(toast);
        
        // Animate in
        setTimeout(() => {
            toast.style.transition = 'all 0.3s ease';
            toast.style.transform = 'translateX(0)';
        }, 100);
        
        // Remove after delay
        setTimeout(() => {
            toast.style.transform = 'translateX(100%)';
            setTimeout(() => toast.remove(), 300);
        }, 2000);
    }).catch(function(err) {
        console.error('Could not copy text: ', err);
        alert('Could not copy to clipboard. Please copy the code manually.');
    });
}

// Auto-dismiss alerts on page load
$(document).ready(function() {
    setTimeout(function() {
        $('.alert').fadeOut('slow');
    }, 5000);
});
