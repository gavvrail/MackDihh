// Support Chat Functionality
let welcomeMessageSent = false;
let isChatOpen = false;
let notificationCheckInterval = null;

// Make functions globally accessible
window.toggleSupportChat = function() {
    try {
        const popup = document.getElementById('support-chat-popup');
        const icon = document.getElementById('support-chat-icon');
        
        if (!popup || !icon) {
            console.error('Support chat elements not found');
            return;
        }
        
        if (isChatOpen) {
            // Close chat
            popup.style.display = 'none';
            icon.innerHTML = '<i class="fas fa-headset"></i>';
            isChatOpen = false;
        } else {
            // Open chat
            popup.style.display = 'flex';
            icon.innerHTML = '<i class="fas fa-times"></i>';
            isChatOpen = true;
            
            // Load existing messages
            loadWidgetMessages();
            
            // Only focus on input if user is authenticated
            const messageInput = document.getElementById('support-message-input');
            if (messageInput) {
                setTimeout(() => {
                    messageInput.focus();
                }, 150);
            }
        }
    } catch (error) {
        console.error('Error toggling support chat:', error);
    }
}

window.sendSupportMessage = async function() {
    try {
        const input = document.getElementById('support-message-input');
        if (!input) {
            console.error('Support message input not found');
            return;
        }
        
        const message = input.value.trim();
        
        if (message === '') return;
        
        // Disable input while sending
        input.disabled = true;
        
        // Add user message to chat immediately
        addMessageToChat(message, true);
        
        // Clear input
        input.value = '';
        
        // Send message to server
        try {
            // Get anti-forgery token
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || 
                         document.querySelector('meta[name="__RequestVerificationToken"]')?.content;
            
            const response = await fetch('/CustomerSupport/SendMessage', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                },
                body: `message=${encodeURIComponent(message)}${token ? `&__RequestVerificationToken=${encodeURIComponent(token)}` : ''}`
            });

            const result = await response.json();
            console.log('Widget send message response:', result);

            if (result.success) {
                console.log('Message sent successfully via widget');
                // Message already added to chat, no need to add again
                
                // Check for auto-response
                checkAutoResponse(message);
            } else {
                console.error('Failed to send message via widget:', result.error);
                // Show error message
                addMessageToChat('Sorry, there was an error sending your message. Please try again.', false);
            }
        } catch (error) {
            console.error('Error sending message via widget:', error);
            addMessageToChat('Sorry, there was an error sending your message. Please try again.', false);
        } finally {
            // Re-enable input
            input.disabled = false;
            input.focus();
        }
        
    } catch (error) {
        console.error('Error sending support message:', error);
    }
}

window.loadWidgetMessages = async function() {
    try {
        const messagesContainer = document.getElementById('support-chat-messages');
        if (!messagesContainer) {
            console.error('Support chat messages container not found');
            return;
        }
        
        // Clear existing messages except the welcome message
        const welcomeMessage = messagesContainer.querySelector('.support-message:first-child');
        messagesContainer.innerHTML = '';
        if (welcomeMessage) {
            messagesContainer.appendChild(welcomeMessage);
        }
        
        // Load messages from server
        const response = await fetch('/CustomerSupport/GetMessages');
        const data = await response.json();
        console.log('Widget load messages response:', data);
        
        if (data.messages && data.messages.length > 0) {
            // Add each message
            data.messages.forEach(message => {
                addMessageToChat(message.message, message.isFromCustomer, message.senderName, message.timestamp);
            });
        }
    } catch (error) {
        console.error('Error loading widget messages:', error);
    }
}

window.addMessageToChat = function(message, isUser, senderName = null, timestamp = null) {
    try {
        const messagesContainer = document.getElementById('support-chat-messages');
        if (!messagesContainer) {
            console.error('Support chat messages container not found');
            return;
        }
        
        const messageDiv = document.createElement('div');
        messageDiv.className = `support-message ${isUser ? 'user-message' : ''}`;
        
        const icon = isUser ? 'fas fa-user' : 'fas fa-robot';
        const sender = senderName || (isUser ? 'You' : 'MackDihh Support');
        const timeStr = timestamp ? ` <small class="text-muted">(${timestamp})</small>` : '';
        
        messageDiv.innerHTML = `
            <div class="support-message-content">
                <i class="${icon} me-2"></i>
                <strong>${sender}:</strong> ${message}${timeStr}
            </div>
        `;
        
        messagesContainer.appendChild(messageDiv);
        
        // Scroll to bottom with a small delay to ensure the message is rendered
        setTimeout(() => {
            messagesContainer.scrollTop = messagesContainer.scrollHeight;
        }, 50);
    } catch (error) {
        console.error('Error adding message to chat:', error);
    }
}

// Initialize support chat when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    try {
        // Initialize chat state
        const popup = document.getElementById('support-chat-popup');
        if (popup) {
            popup.style.display = 'none';
            isChatOpen = false;
        }
        
        // Add event listeners for better reliability
        const icon = document.getElementById('support-chat-icon');
        if (icon) {
            // Remove onclick attribute and add proper event listener
            icon.removeAttribute('onclick');
            icon.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                toggleSupportChat();
            });
        }
        
        // Handle Enter key in input
        const input = document.getElementById('support-message-input');
        if (input) {
            input.addEventListener('keypress', function(e) {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    sendSupportMessage();
                }
            });
        }
        
        // Add click event listener to close button
        const closeButton = document.querySelector('#support-chat-popup .btn-close');
        if (closeButton) {
            closeButton.removeAttribute('onclick');
            closeButton.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                toggleSupportChat();
            });
        }
        
        // Add click event listener to send button
        const sendButton = document.querySelector('#support-chat-popup button[onclick="sendSupportMessage()"]');
        if (sendButton) {
            sendButton.removeAttribute('onclick');
            sendButton.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                sendSupportMessage();
            });
        }
        
        // Start notification checking for admin users
        startNotificationChecking();
        
        // Start polling for new messages for regular users
        startWidgetMessagePolling();
        
    } catch (error) {
        console.error('Error initializing support chat:', error);
    }
});

// Close chat when clicking outside (with improved logic)
document.addEventListener('click', function(e) {
    try {
        const popup = document.getElementById('support-chat-popup');
        const icon = document.getElementById('support-chat-icon');
        
        if (popup && isChatOpen && 
            !popup.contains(e.target) && 
            !icon.contains(e.target)) {
            toggleSupportChat();
        }
    } catch (error) {
        console.error('Error handling outside click:', error);
    }
});

// Admin notification functionality
window.updateAdminNotifications = function() {
    try {
        // Check if user is admin
        const isAdmin = document.querySelector('.body-wrapper[data-user-role="Admin"]') !== null;
        
        if (!isAdmin) return;
        
        // Fetch unread message count
        fetch('/AdminSupport/GetUnreadCount')
            .then(response => response.json())
            .then(data => {
                const notificationBadge = document.getElementById('admin-chat-notification');
                if (notificationBadge) {
                    if (data.count > 0) {
                        notificationBadge.textContent = data.count;
                        notificationBadge.style.display = 'flex';
                    } else {
                        notificationBadge.style.display = 'none';
                    }
                }
                
                // Update admin panel stats
                updateAdminPanelStats(data);
            })
            .catch(error => {
                console.error('Error fetching notification count:', error);
            });
    } catch (error) {
        console.error('Error updating admin notifications:', error);
    }
};

// Update admin panel statistics
window.updateAdminPanelStats = function(data) {
    try {
        const unreadCountElement = document.getElementById('admin-unread-count');
        const totalSessionsElement = document.getElementById('admin-total-sessions');
        
        if (unreadCountElement) {
            unreadCountElement.textContent = data.count || 0;
            
            // Add animation for new unread messages
            if (data.count > 0) {
                unreadCountElement.style.color = '#dc3545';
                unreadCountElement.style.transform = 'scale(1.1)';
                setTimeout(() => {
                    unreadCountElement.style.transform = 'scale(1)';
                }, 200);
            } else {
                unreadCountElement.style.color = '#007bff';
            }
        }
        
        if (totalSessionsElement && data.totalSessions !== undefined) {
            totalSessionsElement.textContent = data.totalSessions;
        }
    } catch (error) {
        console.error('Error updating admin panel stats:', error);
    }
};

// Refresh admin stats manually
window.refreshAdminStats = function() {
    try {
        const refreshButton = document.querySelector('button[onclick="refreshAdminStats()"]');
        if (refreshButton) {
            refreshButton.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Refreshing...';
            refreshButton.disabled = true;
        }
        
        updateAdminNotifications();
        
        setTimeout(() => {
            if (refreshButton) {
                refreshButton.innerHTML = '<i class="fas fa-sync-alt me-2"></i>Refresh';
                refreshButton.disabled = false;
            }
        }, 1000);
    } catch (error) {
        console.error('Error refreshing admin stats:', error);
    }
};

// Start notification checking for admin users
window.startNotificationChecking = function() {
    try {
        // Check if user is admin
        const isAdmin = document.querySelector('.body-wrapper[data-user-role="Admin"]') !== null;
        
        if (!isAdmin) return;
        
        // Update notifications immediately
        updateAdminNotifications();
        
        // Check for new notifications every 30 seconds
        notificationCheckInterval = setInterval(updateAdminNotifications, 30000);
    } catch (error) {
        console.error('Error starting notification checking:', error);
    }
};

// Stop notification checking
window.stopNotificationChecking = function() {
    try {
        if (notificationCheckInterval) {
            clearInterval(notificationCheckInterval);
            notificationCheckInterval = null;
        }
    } catch (error) {
        console.error('Error stopping notification checking:', error);
    }
};

// Widget message polling for regular users
let widgetMessagePollingInterval = null;

window.startWidgetMessagePolling = function() {
    try {
        // Check if user is authenticated and not admin
        const isAdmin = document.querySelector('.body-wrapper[data-user-role="Admin"]') !== null;
        const isAuthenticated = document.querySelector('.body-wrapper[data-user-role]') !== null;
        
        if (isAdmin || !isAuthenticated) return;
        
        // Poll for new messages every 10 seconds
        widgetMessagePollingInterval = setInterval(() => {
            if (isChatOpen) {
                loadWidgetMessages();
            }
        }, 10000);
    } catch (error) {
        console.error('Error starting widget message polling:', error);
    }
};

window.stopWidgetMessagePolling = function() {
    try {
        if (widgetMessagePollingInterval) {
            clearInterval(widgetMessagePollingInterval);
            widgetMessagePollingInterval = null;
        }
    } catch (error) {
        console.error('Error stopping widget message polling:', error);
    }
};

// Auto-response functionality
window.checkAutoResponse = async function(message) {
    try {
        const response = await fetch(`/CustomerSupport/GetAutoResponse?message=${encodeURIComponent(message)}`);
        const data = await response.json();
        
        if (data.response) {
            // Show auto-response after a short delay
            setTimeout(() => {
                addMessageToChat(data.response, false, 'MackDihh Support');
            }, 1000);
        }
    } catch (error) {
        console.error('Error checking auto-response:', error);
    }
};


// Open ticket form
window.openTicketForm = function() {
    const modal = new bootstrap.Modal(document.getElementById('supportTicketModal'));
    modal.show();
};

// Handle ticket form submission
document.addEventListener('DOMContentLoaded', function() {
    const ticketForm = document.getElementById('supportTicketForm');
    if (ticketForm) {
        ticketForm.addEventListener('submit', function(e) {
            e.preventDefault();
            
            const subject = document.getElementById('supportTicketSubject').value;
            const category = document.getElementById('supportTicketCategory').value;
            const description = document.getElementById('supportTicketDescription').value;
            
            // Create a formatted message for the ticket
            const ticketMessage = `[TICKET] Subject: ${subject}\nCategory: ${category}\n\nDescription:\n${description}`;
            
            // Send the ticket as a message
            const input = document.getElementById('support-message-input');
            if (input) {
                input.value = ticketMessage;
                sendSupportMessage();
            }
            
            // Close modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('supportTicketModal'));
            modal.hide();
            
            // Reset form
            ticketForm.reset();
        });
    }
});

// Clear chat history on logout (user side only)
window.clearSupportChatHistory = function() {
    try {
        const messagesContainer = document.getElementById('support-chat-messages');
        if (messagesContainer) {
            // Keep only the welcome message
            const welcomeMessage = messagesContainer.querySelector('.support-message:first-child');
            messagesContainer.innerHTML = '';
            if (welcomeMessage) {
                messagesContainer.appendChild(welcomeMessage);
            }
        }
        
        // Clear any stored chat data
        localStorage.removeItem('supportChatHistory');
        sessionStorage.removeItem('supportChatHistory');
        
        console.log('Support chat history cleared');
    } catch (error) {
        console.error('Error clearing support chat history:', error);
    }
};

// Listen for logout events
document.addEventListener('DOMContentLoaded', function() {
    // Check if there's a logout link and add event listener
    const logoutLinks = document.querySelectorAll('a[href*="Logout"], a[href*="logout"]');
    logoutLinks.forEach(link => {
        link.addEventListener('click', function() {
            // Clear chat history when logout is clicked
            setTimeout(() => {
                clearSupportChatHistory();
            }, 100);
        });
    });
    
    // Also listen for form submissions that might be logout
    const logoutForms = document.querySelectorAll('form[action*="Logout"], form[action*="logout"]');
    logoutForms.forEach(form => {
        form.addEventListener('submit', function() {
            setTimeout(() => {
                clearSupportChatHistory();
            }, 100);
        });
    });
});
