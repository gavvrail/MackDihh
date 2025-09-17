// SignalR Chat Functionality
let chatConnection = null;
let currentSessionId = null;

// Initialize SignalR connection
function initializeChat() {
    if (chatConnection) {
        return;
    }

    // Check if we're actually on a page that needs SignalR chat
    const isOnChatPage = document.getElementById('chatMessages') && !document.getElementById('support-chat-icon');
    if (!isOnChatPage) {
        console.log('Not on a chat page, skipping SignalR initialization');
        return;
    }

    chatConnection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .withAutomaticReconnect()
        .build();

    // Connection event handlers
    chatConnection.start().then(function () {
        console.log("SignalR Chat connected");
        updateConnectionStatus(true);
    }).catch(function (err) {
        console.error("SignalR Chat connection error: ", err);
        updateConnectionStatus(false);
        
        // Only show error messages if we're actually on a chat page that needs SignalR
        const isOnChatPage = document.getElementById('chatMessages') && !document.getElementById('support-chat-icon');
        
        if (isOnChatPage && typeof showNotification === 'function') {
            if (err.status === 401) {
                showNotification('Please log in to use chat', 'error', 5000);
            } else if (err.status === 403) {
                showNotification('You do not have permission to use chat', 'error', 5000);
            } else {
                showNotification('Chat service is temporarily unavailable', 'error', 5000);
            }
        } else {
            // If not on a chat page, just log the error silently
            console.log('SignalR connection failed but not on a chat page, ignoring error');
        }
        
        // Only retry if we're on a chat page that needs SignalR
        if (isOnChatPage) {
            setTimeout(function() {
                if (chatConnection.state === signalR.HubConnectionState.Disconnected) {
                    initializeChat();
                }
            }, 5000);
        }
    });

    // Message received handler
    chatConnection.on("ReceiveMessage", function (message) {
        displayMessage(message);
    });

    // Error handler
    chatConnection.on("Error", function (error) {
        console.error("Chat error:", error);
        if (typeof showNotification === 'function') {
            showNotification(error, 'error', 5000);
        }
    });

    // Typing indicator handler
    chatConnection.on("TypingIndicator", function (data) {
        showTypingIndicator(data);
    });

    // User connection handlers
    chatConnection.on("UserConnected", function (user) {
        console.log("User connected:", user.userName);
    });

    chatConnection.on("UserDisconnected", function (user) {
        console.log("User disconnected:", user.userName);
    });

    // Connection state handlers
    chatConnection.onreconnecting(function () {
        updateConnectionStatus(false);
    });

    chatConnection.onreconnected(function () {
        updateConnectionStatus(true);
    });

    chatConnection.onclose(function () {
        updateConnectionStatus(false);
    });
}

// Join chat session
function joinChatSession(sessionId) {
    if (chatConnection && chatConnection.state === signalR.HubConnectionState.Connected) {
        currentSessionId = sessionId;
        chatConnection.invoke("JoinGroup", `session_${sessionId}`);
    }
}

// Leave chat session
function leaveChatSession() {
    if (chatConnection && currentSessionId) {
        chatConnection.invoke("LeaveGroup", `session_${currentSessionId}`);
        currentSessionId = null;
    }
}

// Send message
function sendChatMessage(message, senderName) {
    if (chatConnection && currentSessionId && message.trim()) {
        // Sanitize inputs before sending
        const sanitizedMessage = sanitizeInput(message);
        const sanitizedSenderName = sanitizeInput(senderName);
        
        if (sanitizedMessage.length > 0) {
            chatConnection.invoke("SendMessage", currentSessionId, sanitizedMessage, sanitizedSenderName);
        }
    }
}

// Send typing indicator
function sendTypingIndicator(senderName, isTyping) {
    if (chatConnection && currentSessionId) {
        const sanitizedSenderName = sanitizeInput(senderName);
        chatConnection.invoke("SendTypingIndicator", currentSessionId, sanitizedSenderName, isTyping);
    }
}

// Display received message
function displayMessage(message) {
    const messagesContainer = document.getElementById('chatMessages');
    if (!messagesContainer) return;

    const messageElement = document.createElement('div');
    messageElement.className = `message ${message.isFromCustomer ? 'customer-message' : 'admin-message'}`;
    
    const timestamp = new Date(message.timestamp).toLocaleTimeString();
    messageElement.innerHTML = `
        <div class="message-header">
            <strong>${message.senderName}</strong>
            <small class="text-muted">${timestamp}</small>
        </div>
        <div class="message-content">${escapeHtml(message.message)}</div>
    `;

    messagesContainer.appendChild(messageElement);
    messagesContainer.scrollTop = messagesContainer.scrollHeight;
}

// Show typing indicator
function showTypingIndicator(data) {
    const typingIndicator = document.getElementById('typingIndicator');
    if (!typingIndicator) return;

    if (data.isTyping) {
        typingIndicator.textContent = `${data.senderName} is typing...`;
        typingIndicator.style.display = 'block';
    } else {
        typingIndicator.style.display = 'none';
    }
}

// Update connection status
function updateConnectionStatus(isConnected) {
    const statusElement = document.getElementById('connectionStatus');
    if (statusElement) {
        statusElement.textContent = isConnected ? 'Connected' : 'Disconnected';
        statusElement.className = isConnected ? 'text-success' : 'text-danger';
    }
}

// Escape HTML to prevent XSS - Enhanced version
function escapeHtml(text) {
    if (typeof text !== 'string') {
        return '';
    }
    
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;',
        '/': '&#x2F;',
        '`': '&#x60;',
        '=': '&#x3D;'
    };
    
    return text.replace(/[&<>"'`=\/]/g, function(m) { 
        return map[m] || m; 
    });
}

// Sanitize user input more thoroughly
function sanitizeInput(input) {
    if (typeof input !== 'string') {
        return '';
    }
    
    // Remove any potential script tags and their content
    let sanitized = input.replace(/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi, '');
    
    // Remove any potential javascript: protocols
    sanitized = sanitized.replace(/javascript:/gi, '');
    
    // Remove any potential data: protocols that could be dangerous
    sanitized = sanitized.replace(/data:(?!image\/[png|jpg|jpeg|gif|webp])/gi, '');
    
    // Trim whitespace and limit length
    sanitized = sanitized.trim().substring(0, 1000);
    
    return sanitized;
}

// Initialize chat when page loads
document.addEventListener('DOMContentLoaded', function() {
    // Only initialize SignalR chat if we're on a specific chat page that needs it
    // Check for specific chat elements that require SignalR (not the support widget)
    const chatMessages = document.getElementById('chatMessages');
    const supportChatIcon = document.getElementById('support-chat-icon');
    
    // Only initialize if we have chatMessages but not the support chat icon
    // This means we're on a dedicated chat page, not the main site with support widget
    if (chatMessages && !supportChatIcon) {
        console.log('Initializing SignalR chat for dedicated chat page');
        initializeChat();
    } else {
        console.log('Skipping SignalR chat initialization - not on a chat page');
    }
});

// Handle chat form submission
function handleChatFormSubmit(event) {
    event.preventDefault();
    
    const messageInput = document.getElementById('chatMessageInput');
    const senderName = document.getElementById('senderName')?.value || 'User';
    
    if (messageInput && messageInput.value.trim()) {
        sendChatMessage(messageInput.value.trim(), senderName);
        messageInput.value = '';
    }
}

// Handle typing indicator
let typingTimer;
function handleTypingIndicator() {
    const senderName = document.getElementById('senderName')?.value || 'User';
    
    // Send typing indicator
    sendTypingIndicator(senderName, true);
    
    // Clear existing timer
    clearTimeout(typingTimer);
    
    // Set timer to stop typing indicator after 1 second
    typingTimer = setTimeout(function() {
        sendTypingIndicator(senderName, false);
    }, 1000);
}
