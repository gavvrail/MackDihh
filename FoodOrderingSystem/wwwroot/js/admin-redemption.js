// Admin Redemption History Management JavaScript

// Function to copy text to clipboard with toast notification
function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(function() {
        // Show success message
        const toast = document.createElement('div');
        toast.className = 'alert alert-success position-fixed';
        toast.style.top = '20px';
        toast.style.right = '20px';
        toast.style.zIndex = '9999';
        toast.innerHTML = '<i class="fas fa-check me-2"></i>Code copied to clipboard!';
        document.body.appendChild(toast);
        
        setTimeout(() => {
            toast.remove();
        }, 2000);
    }).catch(function(err) {
        console.error('Could not copy text: ', err);
        alert('Could not copy to clipboard');
    });
}

// Function to mark redemption as used
function markAsUsed(redemptionId) {
    if (confirm('Mark this redemption as used?')) {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        
        fetch('/Admin/MarkRedemptionAsUsed', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ id: redemptionId })
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                location.reload();
            } else {
                alert('Error: ' + data.message);
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('An error occurred');
        });
    }
}
