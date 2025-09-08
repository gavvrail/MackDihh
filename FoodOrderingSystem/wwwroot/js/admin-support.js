class AdminSupport {
    constructor() {
        this.messageInput = document.getElementById('messageInput');
        this.sendButton = document.getElementById('sendButton');
        this.chatMessages = document.getElementById('chatMessages');
        this.resolveButton = document.getElementById('resolveButton');
        this.closeButton = document.getElementById('closeButton');
        
        this.bindEvents();
        this.scrollToBottom();
        this.startPolling();
    }

    bindEvents() {
        this.sendButton.addEventListener('click', () => this.sendMessage());
        this.messageInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                this.sendMessage();
            }
        });

        if (this.resolveButton) {
            this.resolveButton.addEventListener('click', () => this.resolveSession());
        }

        if (this.closeButton) {
            this.closeButton.addEventListener('click', () => this.closeSession());
        }
    }

    async sendMessage() {
        const message = this.messageInput.value.trim();
        if (!message) return;

        // Disable input and button
        this.messageInput.disabled = true;
        this.sendButton.disabled = true;

        try {
            const response = await fetch('/AdminSupport/SendReply', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                },
                body: `sessionId=${encodeURIComponent(sessionId)}&message=${encodeURIComponent(message)}`
            });

            const result = await response.json();

            if (result.success) {
                this.addMessage(result.message);
                this.messageInput.value = '';
            } else {
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

    async resolveSession() {
        if (!confirm('Are you sure you want to mark this session as resolved?')) {
            return;
        }

        try {
            const response = await fetch('/AdminSupport/ResolveSession', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                },
                body: `sessionId=${encodeURIComponent(sessionId)}`
            });

            const result = await response.json();

            if (result.success) {
                this.showSuccess('Session marked as resolved');
                setTimeout(() => {
                    window.location.href = '/AdminSupport/Index';
                }, 1500);
            } else {
                this.showError(result.error || 'Failed to resolve session');
            }
        } catch (error) {
            console.error('Error resolving session:', error);
            this.showError('Failed to resolve session. Please try again.');
        }
    }

    async closeSession() {
        if (!confirm('Are you sure you want to close this session?')) {
            return;
        }

        try {
            const response = await fetch('/AdminSupport/CloseSession', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                },
                body: `sessionId=${encodeURIComponent(sessionId)}`
            });

            const result = await response.json();

            if (result.success) {
                this.showSuccess('Session closed');
                setTimeout(() => {
                    window.location.href = '/AdminSupport/Index';
                }, 1500);
            } else {
                this.showError(result.error || 'Failed to close session');
            }
        } catch (error) {
            console.error('Error closing session:', error);
            this.showError('Failed to close session. Please try again.');
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

    startPolling() {
        // Poll for new messages every 5 seconds
        setInterval(() => this.checkForNewMessages(), 5000);
    }

    async checkForNewMessages() {
        try {
            const response = await fetch(`/AdminSupport/GetNewMessages?sessionId=${encodeURIComponent(sessionId)}`);
            const result = await response.json();

            if (result.messages && result.messages.length > 0) {
                result.messages.forEach(message => {
                    this.addMessage(message);
                });
            }
        } catch (error) {
            console.error('Error checking for new messages:', error);
        }
    }

    showError(message) {
        this.showNotification(message, 'danger');
    }

    showSuccess(message) {
        this.showNotification(message, 'success');
    }

    showNotification(message, type) {
        // Create a temporary notification
        const notificationDiv = document.createElement('div');
        notificationDiv.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
        notificationDiv.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
        notificationDiv.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        
        document.body.appendChild(notificationDiv);
        
        // Auto-remove after 5 seconds
        setTimeout(() => {
            if (notificationDiv.parentNode) {
                notificationDiv.remove();
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
    new AdminSupport();
});
