// Password strength indicator JavaScript
document.addEventListener('DOMContentLoaded', function() {
    const passwordInput = document.getElementById('newPassword');
    const strengthFill = document.getElementById('passwordStrengthFill');
    const strengthText = document.getElementById('passwordStrengthText');
    
    if (passwordInput && strengthFill && strengthText) {
        passwordInput.addEventListener('input', function() {
            const password = this.value;
            const strength = calculatePasswordStrength(password);
            updatePasswordStrengthIndicator(strength);
            updatePasswordRequirements(password);
        });
    }
    
    function calculatePasswordStrength(password) {
        let score = 0;
        let feedback = [];
        
        // Length check
        if (password.length >= 6) {
            score += 20;
        } else {
            feedback.push('At least 6 characters');
        }
        
        // Uppercase check
        if (new RegExp("[A-Z]").test(password)) {
            score += 20;
        } else {
            feedback.push('One uppercase letter');
        }
        
        // Lowercase check
        if (new RegExp("[a-z]").test(password)) {
            score += 20;
        } else {
            feedback.push('One lowercase letter');
        }
        
        // Digit check
        if (new RegExp("\\d").test(password)) {
            score += 20;
        } else {
            feedback.push('One number');
        }
        
        // Special character check
        var specialCharPattern = new RegExp("[!@$%^&*()_+\\-=\\[\\]{};':\"\\\\|,.<>\\/?]");
        if (specialCharPattern.test(password)) {
            score += 20;
        } else {
            feedback.push('One special character');
        }
        
        // Bonus points for longer passwords
        if (password.length >= 8) score += 10;
        if (password.length >= 12) score += 10;
        
        return {
            score: Math.min(score, 100),
            feedback: feedback
        };
    }
    
    function updatePasswordStrengthIndicator(strength) {
        // Remove all existing classes
        strengthFill.className = 'password-strength-fill';
        
        // Add appropriate class based on score
        if (strength.score === 0) {
            strengthFill.className = 'password-strength-fill';
            strengthText.textContent = 'Enter a password';
            strengthText.className = 'password-strength-text text-muted';
        } else if (strength.score < 40) {
            strengthFill.className = 'password-strength-fill very-weak';
            strengthText.textContent = 'Very Weak';
            strengthText.className = 'password-strength-text text-danger';
        } else if (strength.score < 60) {
            strengthFill.className = 'password-strength-fill weak';
            strengthText.textContent = 'Weak';
            strengthText.className = 'password-strength-text text-warning';
        } else if (strength.score < 80) {
            strengthFill.className = 'password-strength-fill fair';
            strengthText.textContent = 'Fair';
            strengthText.className = 'password-strength-text text-warning';
        } else if (strength.score < 100) {
            strengthFill.className = 'password-strength-fill good';
            strengthText.textContent = 'Good';
            strengthText.className = 'password-strength-text text-info';
        } else {
            strengthFill.className = 'password-strength-fill strong';
            strengthText.textContent = 'Strong';
            strengthText.className = 'password-strength-text text-success';
        }
    }
    
    function updatePasswordRequirements(password) {
        const requirements = {
            'req-length': password.length >= 6,
            'req-uppercase': new RegExp("[A-Z]").test(password),
            'req-lowercase': new RegExp("[a-z]").test(password),
            'req-digit': new RegExp("\\d").test(password),
            'req-special': new RegExp("[!@$%^&*()_+\\-=\\[\\]{};':\"\\\\|,.<>\\/?]").test(password)
        };
        
        Object.keys(requirements).forEach(reqId => {
            const element = document.getElementById(reqId);
            if (element) {
                const icon = element.querySelector('i');
                if (requirements[reqId]) {
                    element.classList.add('valid');
                    icon.className = 'fas fa-check text-success';
                } else {
                    element.classList.remove('valid');
                    icon.className = 'fas fa-times text-danger';
                }
            }
        });
    }
});
