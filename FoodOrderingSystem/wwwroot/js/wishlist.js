// Wishlist functionality with popup notifications
document.addEventListener('DOMContentLoaded', function() {
    // Initialize wishlist functionality
    initializeWishlist();
});

function initializeWishlist() {
    // Handle add to wishlist buttons
    const addToWishlistButtons = document.querySelectorAll('.btn-wishlist');
    addToWishlistButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            const form = this.closest('form');
            if (form) {
                addToWishlist(form);
            }
        });
    });

    // Handle remove from wishlist buttons
    const removeFromWishlistButtons = document.querySelectorAll('.remove-wishlist-btn');
    removeFromWishlistButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            const form = this.closest('form');
            if (form) {
                removeFromWishlist(form);
            }
        });
    });
}

function addToWishlist(form) {
    const formData = new FormData(form);
    const button = form.querySelector('.btn-wishlist');
    const originalContent = button.innerHTML;
    const isInWishlist = button.classList.contains('btn-danger');
    
    // Show loading state
    button.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
    button.disabled = true;

    // If already in wishlist, remove it
    if (isInWishlist) {
        removeFromWishlistByMenuItemId(formData.get('menuItemId'), button);
        return;
    }

    fetch(form.action, {
        method: 'POST',
        body: formData,
        headers: {
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        }
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showPopup(data.message, 'success');
            // Update button to show it's in wishlist
            button.innerHTML = '<i class="fas fa-heart"></i>';
            button.classList.remove('btn-outline-danger');
            button.classList.add('btn-danger');
            button.title = 'Remove from Wishlist';
        } else {
            showPopup(data.message, 'error');
            // Restore original button state
            button.innerHTML = originalContent;
            button.disabled = false;
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showPopup('An error occurred. Please try again.', 'error');
        // Restore original button state
        button.innerHTML = originalContent;
        button.disabled = false;
    });
}

function removeFromWishlistByMenuItemId(menuItemId, button) {
    const originalContent = button.innerHTML;
    
    // Show loading state
    button.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
    button.disabled = true;

    // First, we need to get the wishlist item ID
    fetch('/WishList/GetWishListItemId', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        },
        body: `menuItemId=${menuItemId}`
    })
    .then(response => response.json())
    .then(data => {
        if (data.success && data.wishListItemId) {
            // Now remove the item
            const removeFormData = new FormData();
            removeFormData.append('wishListItemId', data.wishListItemId);
            
            return fetch('/WishList/RemoveFromWishList', {
                method: 'POST',
                body: removeFormData,
                headers: {
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                }
            });
        } else {
            throw new Error('Could not find wishlist item');
        }
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showPopup(data.message, 'success');
            // Update button to show it's not in wishlist
            button.innerHTML = '<i class="far fa-heart"></i>';
            button.classList.remove('btn-danger');
            button.classList.add('btn-outline-danger');
            button.title = 'Add to Wishlist';
        } else {
            showPopup(data.message, 'error');
            // Restore original button state
            button.innerHTML = originalContent;
            button.disabled = false;
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showPopup('An error occurred. Please try again.', 'error');
        // Restore original button state
        button.innerHTML = originalContent;
        button.disabled = false;
    });
}

function removeFromWishlist(form) {
    const formData = new FormData(form);
    const button = form.querySelector('.remove-wishlist-btn');
    const originalContent = button.innerHTML;
    const wishlistItem = form.closest('.wishlist-item-card');
    
    // Show loading state
    button.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
    button.disabled = true;

    fetch(form.action, {
        method: 'POST',
        body: formData,
        headers: {
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        }
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showPopup(data.message, 'success');
            // Remove the item from the UI with animation
            if (wishlistItem) {
                wishlistItem.style.transition = 'opacity 0.3s ease';
                wishlistItem.style.opacity = '0';
                setTimeout(() => {
                    wishlistItem.remove();
                    // Update wishlist count
                    updateWishlistCount();
                }, 300);
            }
        } else {
            showPopup(data.message, 'error');
            // Restore original button state
            button.innerHTML = originalContent;
            button.disabled = false;
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showPopup('An error occurred. Please try again.', 'error');
        // Restore original button state
        button.innerHTML = originalContent;
        button.disabled = false;
    });
}

function showPopup(message, type = 'info') {
    // Remove any existing popups
    const existingPopups = document.querySelectorAll('.wishlist-popup');
    existingPopups.forEach(popup => popup.remove());

    // Create popup element
    const popup = document.createElement('div');
    popup.className = `wishlist-popup wishlist-popup-${type}`;
    popup.innerHTML = `
        <div class="popup-content">
            <div class="popup-icon">
                ${type === 'success' ? '<i class="fas fa-check-circle"></i>' : 
                  type === 'error' ? '<i class="fas fa-exclamation-circle"></i>' : 
                  '<i class="fas fa-info-circle"></i>'}
            </div>
            <div class="popup-message">${message}</div>
            <button class="popup-close" onclick="this.parentElement.parentElement.remove()">
                <i class="fas fa-times"></i>
            </button>
        </div>
    `;

    // Add to page
    document.body.appendChild(popup);

    // Show popup with animation
    setTimeout(() => {
        popup.classList.add('show');
    }, 10);

    // Auto-hide after 4 seconds
    setTimeout(() => {
        if (popup.parentElement) {
            popup.classList.remove('show');
            setTimeout(() => {
                if (popup.parentElement) {
                    popup.remove();
                }
            }, 300);
        }
    }, 4000);
}

function updateWishlistCount() {
    // Update the wishlist count in the header/navbar if it exists
    const countElement = document.querySelector('.wishlist-count');
    if (countElement) {
        fetch('/WishList/GetWishListCount')
            .then(response => response.json())
            .then(data => {
                countElement.textContent = data.count;
            })
            .catch(error => {
                console.error('Error updating wishlist count:', error);
            });
    }
}
