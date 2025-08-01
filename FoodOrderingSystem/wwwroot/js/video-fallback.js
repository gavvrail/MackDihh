// Video Fallback for Localhost Development
document.addEventListener('DOMContentLoaded', function() {
    const videoWrapper = document.querySelector('.video-wrapper');
    const iframe = videoWrapper.querySelector('iframe');
    const placeholder = videoWrapper.querySelector('.video-placeholder');
    
    if (iframe && placeholder) {
        // Check if we're on localhost
        const isLocalhost = window.location.hostname === 'localhost' || 
                           window.location.hostname === '127.0.0.1' ||
                           window.location.hostname.includes('localhost');
        
        // Function to show placeholder
        function showPlaceholder() {
            iframe.style.display = 'none';
            placeholder.style.display = 'flex';
        }
        
        // Function to show iframe
        function showIframe() {
            iframe.style.display = 'block';
            placeholder.style.display = 'none';
        }
        
        // Try to load the iframe
        iframe.onload = function() {
            // If iframe loads successfully, show it
            showIframe();
        };
        
        iframe.onerror = function() {
            // If iframe fails to load, show placeholder
            showPlaceholder();
        };
        
        // For localhost, you can manually trigger the placeholder
        if (isLocalhost) {
            // Uncomment the next line to force placeholder on localhost
            // showPlaceholder();
        }
        
        // Add a button to toggle between iframe and placeholder for testing
        const toggleButton = document.createElement('button');
        toggleButton.innerHTML = '<i class="fas fa-exchange-alt"></i> Toggle Video';
        toggleButton.className = 'btn btn-outline-secondary btn-sm mt-2';
        toggleButton.style.cssText = 'position: absolute; top: 10px; right: 10px; z-index: 10;';
        toggleButton.onclick = function() {
            if (iframe.style.display === 'none') {
                showIframe();
            } else {
                showPlaceholder();
            }
        };
        
        // Only show toggle button on localhost
        if (isLocalhost) {
            videoWrapper.style.position = 'relative';
            videoWrapper.appendChild(toggleButton);
        }
    }
}); 