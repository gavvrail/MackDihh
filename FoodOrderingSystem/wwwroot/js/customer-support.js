class CustomerSupport {
    constructor() {
        this.messageInput = document.getElementById('messageInput');
        this.sendButton = document.getElementById('sendButton');
        this.chatMessages = document.getElementById('chatMessages');
        
        this.bindEvents();
        this.loadMessages();
        this.scrollToBottom();
        
        // Start polling for new messages every 5 seconds
        this.startPolling();
        
        // Make sendMessage method accessible globally for auto-response integration
        window.CustomerSupport = this;
    }

    bindEvents() {
        this.sendButton.addEventListener('click', () => this.sendMessage());
        this.messageInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                this.sendMessage();
            }
        });
    }

    async loadMessages() {
        try {
            const response = await fetch('/CustomerSupport/GetMessages');
            const data = await response.json();
            console.log('Load messages response:', data);
            
            if (data.messages && data.messages.length > 0) {
                // Clear existing messages
                this.chatMessages.innerHTML = '';
                
                // Add each message
                data.messages.forEach(message => {
                    this.addMessage(message);
                });
                console.log(`Loaded ${data.messages.length} messages`);
            } else {
                console.log('No messages found');
            }
        } catch (error) {
            console.error('Error loading messages:', error);
        }
    }

    startPolling() {
        // Poll for new messages every 5 seconds
        setInterval(() => {
            this.loadMessages();
        }, 5000);
    }

    async sendMessage() {
        const message = this.messageInput.value.trim();
        if (!message) return;

        // Disable input and button
        this.messageInput.disabled = true;
        this.sendButton.disabled = true;

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
            console.log('Send message response:', result);

            if (result.success) {
                this.addMessage(result.message);
                this.messageInput.value = '';
                console.log('Message sent successfully');
            } else {
                console.error('Failed to send message:', result.error);
                this.showError(result.error || 'Failed to send message');
            }
        } catch (error) {
            console.error('Error sending message:', error);
            this.showError('Failed to send message. Please try again.');
        } finally {
            // Re-enable input and button
            this.messageInput.disabled = false;
            this.sendButton.disabled = false;
            this.messageInput.focus();
        }
    }

    addMessage(messageData) {
        const messageDiv = document.createElement('div');
        messageDiv.className = `message ${messageData.isFromCustomer ? 'customer' : 'admin'}`;
        
        messageDiv.innerHTML = `
            <div class="message-content">
                <div class="message-header">
                    <strong>${messageData.senderName}</strong>
                    <small class="text-muted">${messageData.timestamp}</small>
                </div>
                <div class="message-text">${this.escapeHtml(messageData.message)}</div>
            </div>
        `;

        this.chatMessages.appendChild(messageDiv);
        this.scrollToBottom();
    }

    scrollToBottom() {
        this.chatMessages.scrollTop = this.chatMessages.scrollHeight;
    }

    showError(message) {
        // Create a temporary error message
        const errorDiv = document.createElement('div');
        errorDiv.className = 'alert alert-danger alert-dismissible fade show';
        errorDiv.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        
        this.chatMessages.parentNode.insertBefore(errorDiv, this.chatMessages);
        
        // Auto-remove after 5 seconds
        setTimeout(() => {
            if (errorDiv.parentNode) {
                errorDiv.remove();
            }
        }, 5000);
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    new CustomerSupport();
});
