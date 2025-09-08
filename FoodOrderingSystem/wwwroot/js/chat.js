// SignalR Chat Functionality
let chatConnection = null;
let currentSessionId = null;

// Initialize SignalR connection
function initializeChat() {
    if (chatConnection) {
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
    });

    // Message received handler
    chatConnection.on("ReceiveMessage", function (message) {
        displayMessage(message);
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
        chatConnection.invoke("SendMessage", currentSessionId, message, senderName);
    }
}

// Send typing indicator
function sendTypingIndicator(senderName, isTyping) {
    if (chatConnection && currentSessionId) {
        chatConnection.invoke("SendTypingIndicator", currentSessionId, senderName, isTyping);
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

// Escape HTML to prevent XSS
function escapeHtml(text) {
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, function(m) { return map[m]; });
}

// Initialize chat when page loads
document.addEventListener('DOMContentLoaded', function() {
    // Only initialize if we're on a chat page
    if (document.getElementById('chatMessages') || document.getElementById('support-chat-icon')) {
        initializeChat();
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
