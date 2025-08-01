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
    
    // Handle form submission
    const contactForm = document.querySelector('form');
    if (contactForm) {
        contactForm.addEventListener('submit', function(e) {
            e.preventDefault();
            
            // Get form data
            const formData = new FormData(contactForm);
            const data = Object.fromEntries(formData);
            
            // Show success message (you can implement actual form submission here)
            alert('Thank you for your message! We will get back to you soon.');
            contactForm.reset();
        });
    }
}); 