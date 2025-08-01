// Simple Notification System - Single Instance
(function() {
    let currentToast = null;
    let notificationContainer = null;
    let notification = null;
    let messageElement = null;
    let autoHideTimer = null;

    function initializeNotification() {
        if (!notificationContainer) {
            notificationContainer = document.getElementById('notification-container');
            notification = document.getElementById('notification');
            messageElement = document.getElementById('notification-message');
        }
    }

    function showNotification(message, type = 'success', duration = 3000) {
        // Initialize if needed
        initializeNotification();
        
        // Clear any existing timer
        if (autoHideTimer) {
            clearTimeout(autoHideTimer);
            autoHideTimer = null;
        }
        
        // Hide any existing toast
        if (currentToast) {
            currentToast.hide();
            currentToast = null;
        }
        
        // Set message
        messageElement.textContent = message;
        
        // Set notification type and styling
        notification.className = `toast align-items-center text-white border-0`;
        
        switch (type) {
            case 'success':
                notification.classList.add('bg-success');
                break;
            case 'error':
                notification.classList.add('bg-danger');
                break;
            case 'warning':
                notification.classList.add('bg-warning');
                break;
            case 'info':
                notification.classList.add('bg-info');
                break;
            default:
                notification.classList.add('bg-success');
        }

        // Show notification
        notificationContainer.style.display = 'block';
        
        // Create and show new toast (disable Bootstrap's auto-hide)
        currentToast = new bootstrap.Toast(notification, {
            autohide: false,
            delay: 0
        });
        
        currentToast.show();

        // Set our own auto-hide timer
        autoHideTimer = setTimeout(() => {
            hideNotification();
        }, duration);
    }

    function hideNotification() {
        // Clear timer
        if (autoHideTimer) {
            clearTimeout(autoHideTimer);
            autoHideTimer = null;
        }
        
        // Hide toast
        if (currentToast) {
            currentToast.hide();
            currentToast = null;
        }
        
        // Hide container
        if (notificationContainer) {
            notificationContainer.style.display = 'none';
        }
    }

    // Only define once
    if (!window.showNotification) {
        window.showNotification = showNotification;
    }
    
    // Expose hide function for manual closing
    if (!window.hideNotification) {
        window.hideNotification = hideNotification;
    }
})(); 