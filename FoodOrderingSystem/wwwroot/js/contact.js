// Contact page JavaScript
document.addEventListener('DOMContentLoaded', function() {
    // Update map iframe with dynamic location
    const mapIframe = document.querySelector('.google-map');
    if (mapIframe && typeof MAP_CONFIG !== 'undefined') {
        // Update the map source with the configured location
        mapIframe.src = generateMapUrl();
        
        // Update the directions link
        const directionsLink = document.querySelector('a[href*="maps.google.com"]');
        if (directionsLink) {
            directionsLink.href = generateDirectionsUrl();
        }
    }
    
    // Handle map loading
    const mapLoading = document.getElementById('mapLoading');
    
    if (mapIframe && mapLoading) {
        mapIframe.addEventListener('load', function() {
            mapLoading.style.display = 'none';
        });
        
        // Fallback: hide loading after 5 seconds if map doesn't load
        setTimeout(function() {
            if (mapLoading.style.display !== 'none') {
                mapLoading.style.display = 'none';
            }
        }, 5000);
    }
    
    // Name field validation - prevent digits
    const nameInputs = document.querySelectorAll('.name-input');
    nameInputs.forEach(input => {
        // Prevent typing digits and special characters
        input.addEventListener('keypress', function(e) {
            const char = String.fromCharCode(e.which);
            const regex = /^[A-Za-z\s]$/;
            
            if (!regex.test(char)) {
                e.preventDefault();
                
                // Show visual feedback
                this.classList.add('is-invalid');
                setTimeout(() => {
                    this.classList.remove('is-invalid');
                }, 1000);
                
                return false;
            }
        });
        
        // Validate on input (for paste operations)
        input.addEventListener('input', function() {
            const value = this.value;
            const regex = /^[A-Za-z\s]*$/;
            
            if (!regex.test(value)) {
                // Remove invalid characters
                this.value = value.replace(/[^A-Za-z\s]/g, '');
                this.classList.add('is-invalid');
                
                setTimeout(() => {
                    this.classList.remove('is-invalid');
                }, 1000);
            } else if (value.length > 0) {
                this.classList.remove('is-invalid');
                this.classList.add('is-valid');
            }
        });
        
        // Clear validation on focus
        input.addEventListener('focus', function() {
            this.classList.remove('is-invalid');
        });
    });

    // Handle form submission
    const contactForm = document.querySelector('form');
    if (contactForm) {
        contactForm.addEventListener('submit', function(e) {
            e.preventDefault();
            
            // Validate name fields before submission
            let isValid = true;
            nameInputs.forEach(input => {
                const value = input.value.trim();
                const regex = /^[A-Za-z\s]+$/;
                
                if (!regex.test(value)) {
                    input.classList.add('is-invalid');
                    isValid = false;
                } else {
                    input.classList.remove('is-invalid');
                    input.classList.add('is-valid');
                }
            });
            
            if (!isValid) {
                alert('Please enter valid names (letters and spaces only).');
                return false;
            }
            
            // Get form data
            const formData = new FormData(contactForm);
            const data = Object.fromEntries(formData);
            
            // Show success message (you can implement actual form submission here)
            alert('Thank you for your message! We will get back to you soon.');
            contactForm.reset();
            
            // Clear validation classes
            nameInputs.forEach(input => {
                input.classList.remove('is-valid', 'is-invalid');
            });
        });
    }
}); 