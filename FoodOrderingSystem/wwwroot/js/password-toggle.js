// Password Toggle Functionality
document.addEventListener('DOMContentLoaded', function() {
    // Function to create password toggle button
    function createPasswordToggle(passwordField) {
        // Create toggle button
        const toggleButton = document.createElement('button');
        toggleButton.type = 'button';
        toggleButton.className = 'password-toggle-btn';
        toggleButton.innerHTML = '<i class="fas fa-eye"></i>';

        // Make password field container relative for absolute positioning
        const passwordContainer = passwordField.parentElement;
        passwordContainer.style.position = 'relative';

        // Add padding to password field to make room for toggle button
        passwordField.style.paddingRight = '45px';

        // Insert toggle button directly after the input field, before any validation messages
        passwordField.insertAdjacentElement('afterend', toggleButton);

        // Ensure the button stays positioned correctly within the input field area only
        const repositionButton = () => {
            // Get the input field's position and dimensions
            const inputRect = passwordField.getBoundingClientRect();
            const containerRect = passwordContainer.getBoundingClientRect();
            
            // Calculate the position relative to the input field only
            const relativeTop = inputRect.top - containerRect.top;
            const relativeRight = containerRect.right - inputRect.right;
            
            // Position the button within the input field bounds
            toggleButton.style.position = 'absolute';
            toggleButton.style.right = '12px';
            toggleButton.style.top = '50%';
            toggleButton.style.transform = 'translateY(-50%)';
            toggleButton.style.zIndex = '30';
            toggleButton.style.pointerEvents = 'auto';
            
            // Ensure it stays within the input field height (not affected by requirements box)
            toggleButton.style.maxHeight = '24px';
            toggleButton.style.height = '24px';
        };

        // Reposition on various events
        repositionButton();
        passwordField.addEventListener('focus', repositionButton);
        passwordField.addEventListener('blur', repositionButton);
        passwordField.addEventListener('input', repositionButton);

        // Use MutationObserver to handle dynamic DOM changes
        const observer = new MutationObserver(() => {
            repositionButton();
        });

        // Observe changes to the parent container
        observer.observe(passwordContainer, {
            childList: true,
            subtree: true,
            attributes: true,
            attributeFilter: ['class', 'style']
        });

        // Additional observer specifically for password requirements visibility
        const requirementsObserver = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                if (mutation.type === 'attributes' && mutation.attributeName === 'style') {
                    const target = mutation.target;
                    if (target.id === 'passwordRequirements') {
                        // When requirements box visibility changes, reposition the button
                        setTimeout(() => {
                            repositionButton();
                        }, 10);
                    }
                }
            });
        });

        // Observe the password requirements div specifically
        const requirementsDiv = document.getElementById('passwordRequirements');
        if (requirementsDiv) {
            requirementsObserver.observe(requirementsDiv, {
                attributes: true,
                attributeFilter: ['style']
            });
        }

        // Toggle password visibility
        toggleButton.addEventListener('click', function() {
            if (passwordField.type === 'password') {
                passwordField.type = 'text';
                toggleButton.innerHTML = '<i class="fas fa-eye-slash"></i>';
                toggleButton.style.color = '#dc3545';
            } else {
                passwordField.type = 'password';
                toggleButton.innerHTML = '<i class="fas fa-eye"></i>';
                toggleButton.style.color = '#6c757d';
            }
        });

        // Hide password on form submission
        const form = passwordField.closest('form');
        if (form) {
            form.addEventListener('submit', function() {
                passwordField.type = 'password';
                toggleButton.innerHTML = '<i class="fas fa-eye"></i>';
                toggleButton.style.color = '#6c757d';
            });
        }
    }

    // Find all password fields and add toggle buttons
    const passwordFields = document.querySelectorAll('input[type="password"]');
    passwordFields.forEach(createPasswordToggle);
}); 