// Password Toggle Functionality
document.addEventListener('DOMContentLoaded', function() {
    // Function to create password toggle button
    function createPasswordToggle(passwordField) {
        // Create toggle button
        const toggleButton = document.createElement('button');
        toggleButton.type = 'button';
        toggleButton.className = 'btn btn-outline-secondary password-toggle-btn';
        toggleButton.innerHTML = '<i class="fas fa-eye"></i>';
        toggleButton.style.cssText = `
            position: absolute;
            right: 10px;
            top: 50%;
            transform: translateY(-50%);
            z-index: 10;
            border: none;
            background: transparent;
            color: #6c757d;
            padding: 5px;
            cursor: pointer;
        `;

        // Make password field container relative for absolute positioning
        const passwordContainer = passwordField.parentElement;
        passwordContainer.style.position = 'relative';

        // Add padding to password field to make room for toggle button
        passwordField.style.paddingRight = '40px';

        // Insert toggle button after password field
        passwordField.parentNode.insertBefore(toggleButton, passwordField.nextSibling);

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