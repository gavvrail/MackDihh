document.addEventListener('DOMContentLoaded', function () {

    // --- VIEW MORE FUNCTIONALITY FOR DESCRIPTIONS ---
    const viewMoreButtons = document.querySelectorAll('.view-more-btn');
    
    viewMoreButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            
            const descriptionText = this.previousElementSibling;
            const fullText = descriptionText.dataset.fullText;
            const isExpanded = descriptionText.dataset.expanded === 'true';
            
            if (isExpanded) {
                // Collapse the text
                const truncatedText = fullText.length > 80 ? fullText.substring(0, 80) + '...' : fullText;
                descriptionText.textContent = truncatedText;
                descriptionText.dataset.expanded = 'false';
                this.textContent = 'View More';
            } else {
                // Expand the text
                descriptionText.textContent = fullText;
                descriptionText.dataset.expanded = 'true';
                this.textContent = 'View Less';
            }
        });
    });

    // --- LIVE SEARCH FUNCTIONALITY ---
    const searchInput = document.getElementById('menuSearch');
    const menuItems = document.querySelectorAll('.menu-item-card');
    const categorySections = document.querySelectorAll('.category-section');
    const categoryLinks = document.querySelectorAll('.category-nav .nav-link');

    searchInput.addEventListener('input', function (e) {
        const searchTerm = e.target.value.toLowerCase().trim();

        // Track which categories have visible items
        const visibleCategories = new Set();

        menuItems.forEach(function (item) {
            const itemName = item.dataset.name;
            const categorySection = item.closest('.category-section');
            
            if (itemName.includes(searchTerm)) {
                item.style.display = 'block';
                if (categorySection) {
                    visibleCategories.add(categorySection.id);
                }
            } else {
                item.style.display = 'none';
            }
        });

        // Show/hide category sections based on visible items
        categorySections.forEach(function (section) {
            const visibleItems = section.querySelectorAll('.menu-item-card[style="display: block"], .menu-item-card:not([style*="display: none"])');
            if (visibleItems.length > 0) {
                section.style.display = 'block';
            } else {
                section.style.display = 'none';
            }
        });

        // Show/hide category navigation links
        categoryLinks.forEach(function (link) {
            const targetId = link.getAttribute('href');
            const targetSection = document.querySelector(targetId);
            if (targetSection && targetSection.style.display !== 'none') {
                link.style.display = 'block';
            } else {
                link.style.display = 'none';
            }
        });
    });

    // --- SCROLLSPY & SMOOTH SCROLL FOR CATEGORY SIDEBAR ---
    const mainContent = document.querySelector('.menu-content'); // Assuming menu content is in a scrollable container

    // Smooth scroll to section on link click
    categoryLinks.forEach(link => {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            const targetId = this.getAttribute('href');
            const targetSection = document.querySelector(targetId);
            if (targetSection) {
                targetSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
        });
    });

    // Highlight active link on scroll
    function onScroll() {
        let currentSectionId = '';

        categorySections.forEach(section => {
            const sectionTop = section.offsetTop - 150; // Offset for better accuracy
            if (window.scrollY >= sectionTop) {
                currentSectionId = section.getAttribute('id');
            }
        });

        categoryLinks.forEach(link => {
            link.classList.remove('active');
            if (link.getAttribute('href') === `#${currentSectionId}`) {
                link.classList.add('active');
            }
        });
    }

    window.addEventListener('scroll', onScroll);
    onScroll(); // Initial check

    // --- CART COUNT UPDATE FUNCTION ---
    function updateCartCount(newCount) {
        const cartBadge = document.getElementById('cart-count');
        if (cartBadge) {
            if (newCount > 0) {
                cartBadge.textContent = newCount;
                cartBadge.style.display = 'inline';
            } else {
                cartBadge.textContent = '0';
                cartBadge.style.display = 'none';
            }
        } else {
            // Fallback: try to find cart badge by class
            const cartBadgeByClass = document.querySelector('.cart-badge');
            if (cartBadgeByClass) {
                if (newCount > 0) {
                    cartBadgeByClass.textContent = newCount;
                    cartBadgeByClass.style.display = 'inline';
                } else {
                    cartBadgeByClass.textContent = '0';
                    cartBadgeByClass.style.display = 'none';
                }
            }
        }
    }

    // --- ADD TO CART FUNCTIONALITY ---
    const addToCartForms = document.querySelectorAll('.add-to-cart-form');
    
    addToCartForms.forEach(form => {
        // Remove any existing event listeners
        const newForm = form.cloneNode(true);
        form.parentNode.replaceChild(newForm, form);
        
        newForm.addEventListener('submit', function(e) {
            e.preventDefault();
            
            const button = this.querySelector('.btn-add-to-cart');
            
            // Prevent multiple submissions
            if (button.disabled) {
                return;
            }
            
            const formData = new FormData(this);
            const originalText = button.innerHTML;
            
            // Show loading state
            button.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
            button.disabled = true;
            
            fetch(this.action, {
                method: 'POST',
                body: formData,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            })
            .then(response => {
                if (response.redirected) {
                    // Redirect to login page if not authenticated
                    window.location.href = response.url;
                    return;
                }
                return response.json();
            })
            .then(data => {
                if (data && data.success) {
                    // Show success notification (only once)
                    if (typeof showNotification === 'function') {
                        showNotification(data.message, 'success', 3000);
                    }
                    
                    // Update cart count
                    updateCartCount(data.newCount);
                    
                    // Reset button
                    button.innerHTML = originalText;
                    button.disabled = false;
                } else {
                    // Show error notification (only once)
                    if (typeof showNotification === 'function') {
                        showNotification(data.message || 'Failed to add item to cart', 'error', 3000);
                    }
                    
                    // Reset button
                    button.innerHTML = originalText;
                    button.disabled = false;
                }
            })
            .catch(error => {
                console.error('Error:', error);
                if (typeof showNotification === 'function') {
                    showNotification('An error occurred while adding item to cart', 'error', 3000);
                }
                
                // Reset button
                button.innerHTML = originalText;
                button.disabled = false;
            });
        });
    });
});
